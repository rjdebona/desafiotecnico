using LancamentoService.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LancamentoService.Infrastructure
{
    public class FluxoDeCaixaRepository
    {
        private readonly LancamentoDbContext _context;
        private readonly IEventPublisher? _publisher;

        public FluxoDeCaixaRepository(LancamentoDbContext context)
        {
            _context = context;
        }

        public FluxoDeCaixaRepository(LancamentoDbContext context, IEventPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public FluxoDeCaixa? GetById(Guid id) => _context.FluxosDeCaixa.Include(f => f.Lancamentos).FirstOrDefault(f => f.Id == id);

        public void Add(FluxoDeCaixa fluxo)
        {
            FluxoDeCaixa _fluxo = new FluxoDeCaixa(fluxo.Id == Guid.Empty ? Guid.NewGuid() : fluxo.Id, fluxo.Nome, fluxo.Lancamentos);
            _context.FluxosDeCaixa.Add(_fluxo);
            _context.SaveChanges();
        }

        public void Update(FluxoDeCaixa fluxo)
        {
            var tracked = _context.ChangeTracker.Entries<FluxoDeCaixa>().FirstOrDefault(e => e.Entity.Id == fluxo.Id && e.State != Microsoft.EntityFrameworkCore.EntityState.Detached);
            if (tracked != null)
            {
                foreach (var lanc in fluxo.Lancamentos ?? Enumerable.Empty<Lancamento>())
                {
                    var exists = _context.Lancamentos.AsNoTracking().Any(l => l.Id == lanc.Id);
                    var entry = _context.Entry(lanc);
                    if (exists)
                    {
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }
                    else
                    {
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    }
                }

                _context.SaveChanges();
            }
            else
            {
                _context.FluxosDeCaixa.Update(fluxo);
                _context.SaveChanges();
            }

            try
            {
                if (_publisher != null)
                {
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Type = "LancamentoCreated",
                        Data = new {
                            Id = fluxo.Id,
                            Nome = fluxo.Nome,
                            Lancamentos = (fluxo.Lancamentos ?? Enumerable.Empty<Lancamento>()).Select(l => new { l.Id, l.Valor, l.Tipo, l.Data, l.Descricao })
                        }
                    });
                    _ = _publisher.PublishAsync("lancamento.created", payload);
                }
            }
            catch { }
        }

        public void Delete(Guid id)
        {
            var fluxo = _context.FluxosDeCaixa.Find(id);
            if (fluxo == null) return;
            _context.FluxosDeCaixa.Remove(fluxo);

                var tracked = _context.ChangeTracker.Entries<FluxoDeCaixa>().FirstOrDefault(e => e.Entity.Id == fluxo.Id && e.State != Microsoft.EntityFrameworkCore.EntityState.Detached);
                if (tracked != null)
                {
                    _context.SaveChanges();
                }
                else
                {
                    _context.FluxosDeCaixa.Update(fluxo);
                    _context.SaveChanges();
                }
        }

        public void UpdateLancamento(Guid fluxoId, Guid lancamentoId, Lancamento lancamento)
        {
            var existing = _context.Lancamentos.Find(lancamentoId);
            if (existing == null || existing.FluxoDeCaixaId != fluxoId) return;

            Lancamento _lancamento = new Lancamento(lancamento.Id, lancamento.Tipo, lancamento.Valor, lancamento.Data, lancamento.Descricao, fluxoId);
            _context.Lancamentos.Update(_lancamento);
            _context.SaveChanges();
        }

        public void DeleteLancamento(Guid fluxoId, Guid lancamentoId)
        {
            var lancamento = _context.Lancamentos.Find(lancamentoId);
            if (lancamento == null || lancamento.FluxoDeCaixaId != fluxoId) return;
            _context.Lancamentos.Remove(lancamento);
            _context.SaveChanges();
        }

        public IEnumerable<FluxoDeCaixa> GetAll() => _context.FluxosDeCaixa.Include(f => f.Lancamentos).ToList();
    }
}
