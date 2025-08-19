namespace LancamentoService.Infrastructure
{
    public interface IEventPublisher
    {
        Task PublishAsync(string routingKey, string payload);
    }
}
