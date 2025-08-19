using RabbitMQ.Client;
using System.Text;

namespace LancamentoService.Infrastructure
{
    public class RabbitMqEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchange = "lancamentos";

        public RabbitMqEventPublisher(string hostname = "localhost")
        {
            var maxAttempts = 12;
            var baseDelayMs = 300;
            Exception? last = null;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var factory = new ConnectionFactory() { HostName = hostname, AutomaticRecoveryEnabled = true, NetworkRecoveryInterval = TimeSpan.FromSeconds(5) };
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Fanout, durable: true);
                    Console.WriteLine($"[Lancamento] Conectado RabbitMQ tentativa {attempt}.");
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (attempt == maxAttempts) break;
                    var delay = baseDelayMs * attempt;
                    Console.WriteLine($"[Lancamento] Falha conexão RabbitMQ tentativa {attempt}: {ex.Message}. Retry em {delay}ms...");
                    Thread.Sleep(delay);
                }
            }
            throw new InvalidOperationException($"Não foi possível conectar RabbitMQ após {maxAttempts} tentativas", last);
        }

        public Task PublishAsync(string routingKey, string payload)
        {
            var body = Encoding.UTF8.GetBytes(payload);
            var props = _channel.CreateBasicProperties();
            props.DeliveryMode = 2;
            _channel.BasicPublish(exchange: _exchange, routingKey: routingKey ?? string.Empty, basicProperties: props, body: body);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
        }
    }
}
