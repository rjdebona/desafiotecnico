using Microsoft.EntityFrameworkCore;
using System;
using FluxoDeCaixaAuth;
using LancamentoService.Infrastructure;
// using LancamentoService.Domain; // seed removido

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFluxoDeCaixaAuth(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration: Postgres only (SQLite removido)
var pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres";
var pgDb = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "fluxo_db";
var pgUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "fluxo";
var pgPw = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "fluxo_pw";
var pgPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var configuredCs = builder.Configuration.GetConnectionString("DefaultConnection");
// Se já veio uma connection string completa (ex: via env ConnectionStrings__DefaultConnection) usamos ela; caso contrário montamos incluindo a porta.
var pgCs = string.IsNullOrWhiteSpace(configuredCs)
    ? $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPw};Include Error Detail=true"
    : configuredCs;
// Aviso defensivo: detectar se veio uma connection string de SQLite por engano
if (pgCs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) || pgCs.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine($"[Lancamento][WARN] Connection string aparenta ser SQLite: '{pgCs}'. Verifique se variáveis POSTGRES_* estão definidas corretamente e remova ConnectionStrings__DefaultConnection.");
}
builder.Services.AddDbContext<LancamentoDbContext>(o => o.UseNpgsql(pgCs));
Console.WriteLine($"[Lancamento] Usando Postgres em {pgHost}:{pgPort} DB={pgDb}");

var rabbitHost = builder.Configuration["RABBIT_HOST"] ?? "localhost";
builder.Services.AddSingleton<IEventPublisher>(sp => new RabbitMqEventPublisher(rabbitHost));
builder.Services.AddScoped<FluxoDeCaixaRepository>();

var app = builder.Build();

var maxDbAttempts = int.TryParse(Environment.GetEnvironmentVariable("DB_MAX_ATTEMPTS"), out var mda) ? mda : 12;
var dbDelayMs = int.TryParse(Environment.GetEnvironmentVariable("DB_RETRY_DELAY_MS"), out var dms) ? dms : 1500;
for (var attempt = 1; attempt <= maxDbAttempts; attempt++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LancamentoDbContext>();
        db.Database.Migrate();
        Console.WriteLine($"[Lancamento] Migração concluída (tentativa {attempt}).");
        break;
    }
    catch (Exception ex)
    {
        if (attempt == maxDbAttempts) throw;
        Console.WriteLine($"[Lancamento] Migração falhou tentativa {attempt}: {ex.Message}. Aguardando {dbDelayMs}ms...");
        Thread.Sleep(dbDelayMs);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();
// UI estática (tela de lançamentos)
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();

public partial class Program {}
