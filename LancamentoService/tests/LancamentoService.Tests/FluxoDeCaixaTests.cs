using System;
using LancamentoService.Domain;
using Xunit;

namespace LancamentoService.Tests
{
    public class FluxoDeCaixaTests
    {
        [Fact]
        public void DeveAdicionarLancamentoAoFluxoDeCaixa()
        {
            var fluxoId = Guid.NewGuid();
            var fluxo = new FluxoDeCaixa(fluxoId, "Principal");
            var lancamento = new Lancamento(Guid.NewGuid(), TipoLancamento.Credito, 100m, DateTime.Today, "Teste", fluxoId);
            fluxo.AddLancamento(lancamento);
            Assert.Contains(lancamento, fluxo.Lancamentos);
        }
    }
}
