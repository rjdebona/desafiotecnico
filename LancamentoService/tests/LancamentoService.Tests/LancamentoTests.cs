using System;
using LancamentoService.Domain;
using Xunit;

namespace LancamentoService.Tests
{
    public class LancamentoTests
    {
        [Fact]
        public void CriarLancamento_Valido_DeveSucceed()
        {
            var fluxoId = Guid.NewGuid();
            var id = Guid.NewGuid();
            var l = new Lancamento(id, TipoLancamento.Credito, 150.50m, DateTime.UtcNow, "Pagamento", fluxoId);
            Assert.Equal(id, l.Id);
            Assert.Equal(TipoLancamento.Credito, l.Tipo);
            Assert.Equal(150.50m, l.Valor);
            Assert.Equal("Pagamento", l.Descricao);
            Assert.Equal(fluxoId, l.FluxoDeCaixaId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void CriarLancamento_ComValorInvalido_DeveFalhar(decimal valor)
        {
            var fluxoId = Guid.NewGuid();
            Assert.Throws<ArgumentOutOfRangeException>(() => new Lancamento(Guid.NewGuid(), TipoLancamento.Debito, valor, DateTime.UtcNow, "Desc", fluxoId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CriarLancamento_ComDescricaoInvalida_DeveFalhar(string descricao)
        {
            var fluxoId = Guid.NewGuid();
            Assert.Throws<ArgumentException>(() => new Lancamento(Guid.NewGuid(), TipoLancamento.Debito, 10m, DateTime.UtcNow, descricao, fluxoId));
        }

        [Fact]
        public void CriarLancamento_ComFluxoIdVazio_DeveFalhar()
        {
            Assert.Throws<ArgumentException>(() => new Lancamento(Guid.NewGuid(), TipoLancamento.Credito, 10m, DateTime.UtcNow, "Desc", Guid.Empty));
        }
    }
}
