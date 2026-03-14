using RabbitMQ.Client;

namespace LogAnalyzer.RabbitMQ
{
    public class RabbitMQConsumer
    {
        public async Task Consume()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "log_queue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);


        }
    }
}
