using System.Globalization;
using System.Text.RegularExpressions;
using LogAnalyzer.Domain.DTO;
using LogAnalyzer.LogAnalyzerCore.Interfaces;

namespace LogAnalyzer.Domain.Workers
{
    public class MessageConsumerWorker : BackgroundService
    {
        private static readonly Regex LogRegex = new(
            "^(?<ip>\\S+)\\s+\\S+\\s+\\S+\\s+\\[[^\\]]+\\]\\s+\"[A-Z]+\\s+(?<url>\\S+)\\s+[^\"]+\"\\s+\\d+\\s+\\d+\\s+\"[^\"]*\"\\s+\"[^\"]*\"\\s+(?<responseTime>\\d+(?:\\.\\d+)?)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly IMessageConsumer _consumer;
        private readonly List<LogResponseTime> _parsedLogs = [];
        private readonly object _parsedLogsLock = new();

        public MessageConsumerWorker(IMessageConsumer consumer)
        {
            _consumer = consumer;
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
            });
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