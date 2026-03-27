using LogAnalyzer.Core.Interfaces;
using LogAnalyzer.Domain.DTO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogAnalyzer.Workers.Workers
{
    public class MessageConsumerWorker : BackgroundService
    {
        private readonly ILogger<MessageConsumerWorker> _logger;
        private static readonly Regex LogRegex = new(
            "^(?<ip>\\S+)\\s+\\S+\\s+\\S+\\s+\\[[^\\]]+\\]\\s+\"[A-Z]+\\s+(?<url>\\S+)\\s+[^\"]+\"\\s+\\d+\\s+\\d+\\s+\"[^\"]*\"\\s+\"[^\"]*\"\\s+(?<responseTime>\\d+(?:\\.\\d+)?)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly IMessageConsumer _consumer;
        private readonly List<LogResponseTime> _parsedLogs = [];
        private readonly object _parsedLogsLock = new();

        public MessageConsumerWorker(IMessageConsumer consumer, ILogger<MessageConsumerWorker> logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _consumer.ConsumeAsync(async message =>
            {
                if (TryParseLogResponseTime(message, out var logResponseTime))
                {
                    lock (_parsedLogsLock)
                    {
                        _parsedLogs.Add(logResponseTime);
                    }
                }

                await Task.CompletedTask;
            }, stoppingToken);
        }

        private static bool TryParseLogResponseTime(string message, out LogResponseTime logResponseTime)
        {
            logResponseTime = new LogResponseTime();

            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var match = LogRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }

            var ip = match.Groups["ip"].Value;
            var url = match.Groups["url"].Value;
            var responseTimeRaw = match.Groups["responseTime"].Value;

            if (!float.TryParse(responseTimeRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var responseTime))
            {
                return false;
            }

            logResponseTime = new LogResponseTime
            {
                OriginIP = ip,
                RequestURL = url,
                ResponseTime = responseTime
            };

            return true;
        }
    }
}