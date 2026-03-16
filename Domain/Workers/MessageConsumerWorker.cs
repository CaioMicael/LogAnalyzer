using LogAnalyzer.LogAnalyzerCore.Interfaces;

namespace LogAnalyzer.Domain.Workers
{
    public class MessageConsumerWorker : BackgroundService
    {
        private readonly IMessageConsumer _consumer;

        public MessageConsumerWorker(IMessageConsumer consumer)
        {
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _consumer.ConsumeAsync(async (message) =>
            {
                Thread.Sleep(1);
                Console.WriteLine($"Received message: {message}");  
                await Task.CompletedTask;
            });
        }
    }
}
