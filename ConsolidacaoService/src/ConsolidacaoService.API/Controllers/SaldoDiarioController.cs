using Microsoft.AspNetCore.Mvc;
using ConsolidacaoService.Infrastructure;
using ConsolidacaoService.Domain;
using System.Text.Json;

namespace ConsolidacaoService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class SaldoDiarioController : ControllerBase
{
    private readonly ConsolidacaoDbContext _db;
    private readonly ISaldoDiarioService _service;
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache;
    private readonly ILogger<SaldoDiarioController> _logger;

    public SaldoDiarioController(ConsolidacaoDbContext db, ISaldoDiarioService service, 
        Microsoft.Extensions.Caching.Distributed.IDistributedCache cache,
        ILogger<SaldoDiarioController> logger)
    {
        _db = db; _service = service; _cache = cache; _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime data)
    {
        var reqDay = data.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(data, DateTimeKind.Utc) : data.ToUniversalTime();
        var start = reqDay.Date;
        var cacheKey = $"saldo:{start:yyyy-MM-dd}";

        try 
        {
            // 1. Tentar ler do cache primeiro
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null) 
            {
                var cachedJson = System.Text.Encoding.UTF8.GetString(cached);
                var cachedData = JsonSerializer.Deserialize<JsonElement>(cachedJson);
                _logger.LogInformation("Cache hit para {Key}", cacheKey);
                
                // Ainda precisamos buscar os lançamentos para retornar completo
                var end = start.AddDays(1);
                var lancs = _db.Lancamentos.Where(l => l.Data >= start && l.Data < end).ToList();
                
                return Ok(new { 
                    data = cachedData.GetProperty("Date").GetDateTime(),
                    saldoTotal = cachedData.GetProperty("Saldo").GetDecimal(),
                    lancamentos = lancs,
                    fromCache = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao ler cache {Key}, fallback para DB", cacheKey);
        }

        // 2. Se não encontrar no cache, buscar no banco
        _logger.LogInformation("Cache miss para {Key}, consultando DB", cacheKey);
        var end2 = start.AddDays(1);
        var lancs2 = _db.Lancamentos.Where(l => l.Data >= start && l.Data < end2).ToList();
        var saldo = _service.CalcularSaldo(lancs2, start);
        
        return Ok(new { 
            data = saldo.Data, 
            saldoTotal = saldo.SaldoTotal, 
            lancamentos = lancs2,
            fromCache = false
        });
    }
}
