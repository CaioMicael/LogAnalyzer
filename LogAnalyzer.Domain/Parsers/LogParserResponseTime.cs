using LogAnalyzer.Domain.DTO;
using LogAnalyzer.Domain.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogAnalyzer.Domain.Parsers
{
    public class LogParserResponseTime : ILogParser
    {
        private static readonly Regex LogRegex = new(
            "^(?<ip>\\S+)\\s+\\S+\\s+\\S+\\s+\\[[^\\]]+\\]\\s+\"[A-Z]+\\s+(?<url>\\S+)\\s+[^\"]+\"\\s+\\d+\\s+\\d+\\s+\"[^\"]*\"\\s+\"[^\"]*\"\\s+(?<responseTime>\\d+(?:\\.\\d+)?)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public bool TryParseLog(string message, out LogResponseTime logResponseTime)
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
