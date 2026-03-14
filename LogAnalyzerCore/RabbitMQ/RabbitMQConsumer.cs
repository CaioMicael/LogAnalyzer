using LogAnalyzer.LogAnalyzerCore.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogAnalyzer.LogAnalyzerCore.RabbitMQ
{
    public class RabbitMQConsumer : IMessageConsumer
    {
        private readonly string _hostName;
        private readonly string _queueName;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQConsumer()
        {
            _hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                ?? throw new ArgumentNullException("Variável de ambiente RABBITMQ_HOST não definida");

            _queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE")
                ?? throw new ArgumentNullException("Variável de ambiente RABBITMQ_QUEUE não definida");
        }

        public async Task ConsumeAsync(Func<string, Task> onMessageReceived)
        {
            var factory = new ConnectionFactory { HostName = _hostName };
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                await onMessageReceived(message);
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
        }
    }
}
