using System;
using System.Collections.Generic;

namespace LancamentoService.Domain
{
    public class FluxoDeCaixa
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public ICollection<Lancamento> Lancamentos { get; private set; } = new List<Lancamento>();
        public FluxoDeCaixa(Guid id, string nome, IEnumerable<Lancamento> lancamentos = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome é obrigatório", nameof(nome));

            Id = id;
            Nome = nome;

            if (lancamentos != null)
            {
                foreach (var lancamento in lancamentos)
                    Lancamentos.Add(lancamento);
            }
        }

        public void AddLancamento(Lancamento lancamento)
        {
            Lancamentos.Add(lancamento);
        }

        protected FluxoDeCaixa() { }

    }
}
