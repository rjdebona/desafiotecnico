using System.Text.Json;
using ConsolidacaoService.Domain;

namespace ConsolidacaoService.Infrastructure;

public interface ISaldoHandler
{
    Task HandleLancamentoCreatedAsync(JsonElement data);
}

public class SaldoDiarioHandler : ISaldoHandler
{
    private readonly ConsolidacaoDbContext _db;
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache;
    private readonly Microsoft.Extensions.Logging.ILogger<SaldoDiarioHandler>? _logger;

    public SaldoDiarioHandler(ConsolidacaoDbContext db, Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, Microsoft.Extensions.Logging.ILogger<SaldoDiarioHandler>? logger)
    {
        _db = db; _cache = cache; _logger = logger;
    }

    public async Task HandleLancamentoCreatedAsync(JsonElement data)
    {

        // Espera apenas payload de fluxo completo: { Id, Nome, Lancamentos: [] }
        if (!data.TryGetProperty("Lancamentos", out var lancsElem) || lancsElem.ValueKind != JsonValueKind.Array)
        {
            _logger?.LogWarning("Payload sem Lancamentos array ignorado");
            return;
        }

        var diasAlterados = new HashSet<DateTime>();
        foreach (var lElem in lancsElem.EnumerateArray())
        {
            try
            {
                var id = lElem.GetProperty("Id").GetGuid();
                if (await _db.Lancamentos.FindAsync(id) != null) continue; // idempotente
                var valor = lElem.GetProperty("Valor").GetDecimal();
                var tipoElem = lElem.GetProperty("Tipo");
                var dataTime = lElem.GetProperty("Data").GetDateTime();
                if (dataTime.Kind == DateTimeKind.Unspecified)
                    dataTime = DateTime.SpecifyKind(dataTime, DateTimeKind.Utc);
                else
                    dataTime = dataTime.ToUniversalTime();
                var day = dataTime.Date;
                diasAlterados.Add(day);

                TipoLancamento tipoEnum;
                if (tipoElem.ValueKind == JsonValueKind.Number)
                {
                    var tipoInt = tipoElem.GetInt32();
                    tipoEnum = Enum.IsDefined(typeof(TipoLancamento), tipoInt) ? (TipoLancamento)tipoInt : TipoLancamento.Debito;
                }
                else
                {
                    var tipoStr = tipoElem.GetString() ?? "Debito";
                    tipoEnum = Enum.TryParse<TipoLancamento>(tipoStr, true, out var parsed) ? parsed : TipoLancamento.Debito;
                }

                var lanc = new Lancamento { Id = id, Tipo = tipoEnum, Valor = valor, Data = dataTime, Descricao = lElem.GetProperty("Descricao").GetString() + "Handler" };
                _db.Lancamentos.Add(lanc);

                var saldo = await _db.SaldosDiarios.FindAsync(day);
                if (saldo == null)
                {
                    saldo = new SaldoDiario { Data = day, SaldoTotal = 0 };
                    _db.SaldosDiarios.Add(saldo);
                }
                saldo.SaldoTotal += (lanc.Tipo == TipoLancamento.Credito ? lanc.Valor : -lanc.Valor);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Falha processando item de fluxo");
            }
        }
        await _db.SaveChangesAsync();

        foreach (var day in diasAlterados)
        {
            var saldo = await _db.SaldosDiarios.FindAsync(day);
            if (saldo == null) continue;
            var cacheKey = $"saldo:{saldo.Data:yyyy-MM-dd}";
            var cacheVal = JsonSerializer.Serialize(new { Date = saldo.Data, Saldo = saldo.SaldoTotal });
            var bytes = System.Text.Encoding.UTF8.GetBytes(cacheVal);
            var options = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(1) };
            try { _cache.Set(cacheKey, bytes, options); _logger?.LogInformation("Cache set {Key} = {Val}", cacheKey, cacheVal); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache write fail {Key}", cacheKey); }
        }
    }
}
