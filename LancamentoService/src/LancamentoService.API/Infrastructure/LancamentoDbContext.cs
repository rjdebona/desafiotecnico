using LancamentoService.Domain;
using Microsoft.EntityFrameworkCore;

namespace LancamentoService.Infrastructure
{
    public class LancamentoDbContext : DbContext
    {
        public LancamentoDbContext(DbContextOptions<LancamentoDbContext> options) : base(options) { }

        public DbSet<Lancamento> Lancamentos { get; set; }
        public DbSet<FluxoDeCaixa> FluxosDeCaixa { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FluxoDeCaixa>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.HasMany(e => e.Lancamentos)
                      .WithOne(l => l.FluxoDeCaixa)
                      .HasForeignKey(l => l.FluxoDeCaixaId);
            });

            modelBuilder.Entity<Lancamento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Tipo).IsRequired();
                entity.Property(e => e.Valor).IsRequired();
                entity.Property(e => e.Data).IsRequired();
                entity.Property(e => e.Descricao).HasMaxLength(255);
                entity.Property(e => e.FluxoDeCaixaId).IsRequired();
                entity.HasIndex(e => e.Data);
                entity.HasIndex(e => new { e.FluxoDeCaixaId, e.Data });
                entity.HasIndex(e => e.Tipo);
            });
        }
    }
}
