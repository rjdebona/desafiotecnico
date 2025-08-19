using Microsoft.EntityFrameworkCore;
using ConsolidacaoService.Domain;

namespace ConsolidacaoService.Infrastructure;

public class ConsolidacaoDbContext : DbContext
{
    public ConsolidacaoDbContext(DbContextOptions<ConsolidacaoDbContext> options) : base(options) {}

    public DbSet<Lancamento> Lancamentos { get; set; } = null!;
    public DbSet<SaldoDiario> SaldosDiarios { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).IsRequired();
            e.Property(x => x.Valor).IsRequired();
            e.Property(x => x.Data).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(255);
        });
        modelBuilder.Entity<SaldoDiario>(e => {
            e.HasKey(x => x.Data);
            e.Property(x => x.SaldoTotal).IsRequired();
        });
    }
}
