using LogAnalyzer.LogAnalyzerCore.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

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

        public async Task ConsumeAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory { HostName = _hostName };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(options: null, cancellationToken);

            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.Span);
                    await onMessageReceived(message);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                }
                catch
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _queueName, 
                autoAck: false, 
                consumer: consumer, 
                cancellationToken);

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // shutdown esperado
            }

            if (_channel is not null)
            {
                await _channel.CloseAsync(cancellationToken);
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync(cancellationToken);
                await _connection.DisposeAsync();
            }
        }
    }
}
