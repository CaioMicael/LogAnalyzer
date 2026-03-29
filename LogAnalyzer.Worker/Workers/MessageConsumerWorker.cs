using LogAnalyzer.Core.Interfaces;
using LogAnalyzer.Domain.DTO;
using LogAnalyzer.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogAnalyzer.Workers.Workers
{
    public class MessageConsumerWorker : BackgroundService
    {
        private readonly ILogger<MessageConsumerWorker> _logger;
        private readonly IMessageConsumer _consumer;
        private readonly ILogParser _logParser;
        private readonly List<LogResponseTime> _parsedLogs = [];
        private readonly object _parsedLogsLock = new();

        public MessageConsumerWorker(
            IMessageConsumer consumer,
            ILogParser logParser,
            ILogger<MessageConsumerWorker> logger)
        {
            _consumer = consumer;
            _logParser = logParser;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _consumer.ConsumeAsync(async message =>
            {
                if (_logParser.TryParseLog(message, out var logResponseTime))
                {
                    lock (_parsedLogsLock)
                    {
                        _parsedLogs.Add(logResponseTime);
                    }
                }

                await Task.CompletedTask;
            }, stoppingToken);
        }
    }
}
