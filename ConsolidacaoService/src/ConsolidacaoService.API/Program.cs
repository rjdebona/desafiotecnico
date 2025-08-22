using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using FluxoDeCaixaAuth;
using ConsolidacaoService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var redisHost = builder.Configuration["REDIS_HOST"] ?? "";
if (string.IsNullOrWhiteSpace(redisHost) || redisHost == "memory")
    builder.Services.AddDistributedMemoryCache();
else
    builder.Services.AddStackExchangeRedisCache(o => { o.Configuration = redisHost; o.InstanceName = "consolidacao_"; });

var pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres";
var pgDb = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "fluxo_db";
var pgUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "fluxo";
var pgPw = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "fluxo_pw";
var pgPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var configuredCs = builder.Configuration.GetConnectionString("DefaultConnection");
var pgCs = string.IsNullOrWhiteSpace(configuredCs)
    ? $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPw};Include Error Detail=true"
    : configuredCs;
builder.Services.AddDbContext<ConsolidacaoDbContext>(o => o.UseNpgsql(pgCs));
Console.WriteLine($"[Consolidacao] Usando Postgres em {pgHost}:{pgPort} DB={pgDb}");

builder.Services.AddFluxoDeCaixaAuth(builder.Configuration);
builder.Services.AddScoped<ISaldoDiarioService, SaldoDiarioService>();
builder.Services.AddScoped<ISaldoHandler, SaldoDiarioHandler>();

var app = builder.Build();
var maxDbAttempts = int.TryParse(Environment.GetEnvironmentVariable("DB_MAX_ATTEMPTS"), out var mda) ? mda : 12;
var dbDelayMs = int.TryParse(Environment.GetEnvironmentVariable("DB_RETRY_DELAY_MS"), out var dms) ? dms : 1500;
for (var attempt = 1; attempt <= maxDbAttempts; attempt++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsolidacaoDbContext>();
        db.Database.Migrate();
        Console.WriteLine($"[Consolidacao] Migração concluída (tentativa {attempt}).");
        break;
    }
    catch (Exception ex)
    {
        if (attempt == maxDbAttempts) throw;
        Console.WriteLine($"[Consolidacao] Migração falhou tentativa {attempt}: {ex.Message}. Aguardando {dbDelayMs}ms...");
        Thread.Sleep(dbDelayMs);
    }
}

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseAuthentication();
app.UseAuthorization();
// UI estática (tela de saldo diário)
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

var rabbitHost = builder.Configuration["RABBIT_HOST"] ?? "localhost";
var maxAttempts = int.TryParse(Environment.GetEnvironmentVariable("RABBIT_MAX_ATTEMPTS"), out var ma) ? ma : 12;
var baseDelayMs = int.TryParse(Environment.GetEnvironmentVariable("RABBIT_BASE_DELAY_MS"), out var bd) ? bd : 500;
IConnection? connection = null;
IModel? channel = null;
QueueDeclareOk? q = null;
for (var attempt = 1; attempt <= maxAttempts; attempt++)
{
    try
    {
        var factory = new ConnectionFactory { HostName = rabbitHost, AutomaticRecoveryEnabled = true, NetworkRecoveryInterval = TimeSpan.FromSeconds(5) };
        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        channel.ExchangeDeclare(exchange: "lancamentos", type: ExchangeType.Fanout, durable: true);
        q = channel.QueueDeclare(queue: "lancamentos_consolidacao", durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(q.QueueName, "lancamentos", "");
        Console.WriteLine($"[Consolidacao] Conectado RabbitMQ em tentativa {attempt}.");
        break;
    }
    catch (Exception ex)
    {
        if (attempt == maxAttempts)
        {
            Console.WriteLine($"[Consolidacao] Falha definitiva ao conectar RabbitMQ após {attempt} tentativas: {ex.Message}");
            throw;
        }
        var delay = baseDelayMs * attempt;
        Console.WriteLine($"[Consolidacao] Tentativa {attempt} falhou: {ex.Message}. Retentando em {delay}ms...");
        Thread.Sleep(delay);
    }
}

var consumer = new EventingBasicConsumer(channel!);
consumer.Received += async (_, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    try
    {
        using var scope = app.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ISaldoHandler>();
        var doc = JsonDocument.Parse(message);
        if (doc.RootElement.TryGetProperty("Type", out var t) && t.GetString() == "LancamentoCreated")
        {
            var data = doc.RootElement.GetProperty("Data");
            await handler.HandleLancamentoCreatedAsync(data);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro processamento mensagem: {ex.Message}");
    }
};
channel!.BasicConsume(q!.QueueName, autoAck: true, consumer: consumer);

app.Lifetime.ApplicationStopping.Register(() => { try { channel?.Close(); } catch { } try { connection?.Close(); } catch { } });

app.Run();

public partial class Program { }
