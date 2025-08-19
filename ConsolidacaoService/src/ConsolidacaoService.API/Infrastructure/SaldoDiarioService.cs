using ConsolidacaoService.Domain;

namespace ConsolidacaoService.Infrastructure;

public interface ISaldoDiarioService
{
    SaldoDiario CalcularSaldo(IEnumerable<Lancamento> lancamentos, DateTime data);
}

public class SaldoDiarioService : ISaldoDiarioService
{
    public SaldoDiario CalcularSaldo(IEnumerable<Lancamento> lancamentos, DateTime data)
    {
        var doDia = lancamentos.Where(l => l.Data.Date == data.Date);
        decimal saldo = doDia.Sum(l => l.Tipo == TipoLancamento.Credito ? l.Valor : -l.Valor);
        return new SaldoDiario { Data = data.Date, SaldoTotal = saldo };
    }
}
