using LogAnalyzer.LogAnalyzerCore.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogAnalyzer.LogAnalyzerCore.RabbitMQ
{
    public class RabbitMQConsumer : IMessageConsumer
    {
        private IConnection? _connection;
        private IChannel? _channel;

        public async Task ConsumeAsync(Func<string, Task> onMessageReceived)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: "log_queue",
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

            await _channel.BasicConsumeAsync(queue: "log_queue", autoAck: false, consumer: consumer);
        }
    }
}
