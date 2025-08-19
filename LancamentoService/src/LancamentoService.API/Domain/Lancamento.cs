using System;
using System.Text.Json.Serialization;

namespace LancamentoService.Domain
{
    public enum TipoLancamento
    {
        Credito,
        Debito
    }

    public class Lancamento
    {
        public Guid Id { get; private set; }
        public TipoLancamento Tipo { get; private set; }
        public decimal Valor { get; private set; }
        public DateTime Data { get; private set; }
        public string Descricao { get; private set; } = string.Empty;
        public Guid FluxoDeCaixaId { get; private set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public FluxoDeCaixa? FluxoDeCaixa { get; private set; }

        // EF Core
        private Lancamento() { }

        // Used by System.Text.Json to deserialize incoming API payloads
        public Lancamento(Guid id,TipoLancamento tipo, decimal valor, DateTime data, string descricao, Guid fluxoDeCaixaId)
        {
            if (fluxoDeCaixaId == Guid.Empty)
                throw new ArgumentException("FluxoDeCaixaId inválido", nameof(fluxoDeCaixaId));
            if (valor <= 0)
                throw new ArgumentOutOfRangeException(nameof(valor), "Valor deve ser positivo");
            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descrição inválida", nameof(descricao));
            if(id==Guid.Empty)
                id = Guid.NewGuid();

            Id = id;
            Tipo = tipo;
            Valor = valor;
            Data = data;
            Descricao = descricao.Trim();
            FluxoDeCaixaId = fluxoDeCaixaId;
        }
    }
}
