namespace ConsolidacaoService.Domain;

public enum TipoLancamento { Credito = 0, Debito = 1 }

public class Lancamento
{
    public Guid Id { get; set; }
    public TipoLancamento Tipo { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public string? Descricao { get; set; }
}
