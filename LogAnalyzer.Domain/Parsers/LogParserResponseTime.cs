using LogAnalyzer.Domain.DTO;
using LogAnalyzer.Domain.Interfaces;
using System.Globalization;

namespace LogAnalyzer.Domain.Parsers
{
    // Parses Apache Combined Log Format with a trailing response-time field:
    // {ip} - - [{timestamp}] "{method} {url} {protocol}" {status} {bytes} "{referer}" "{user-agent}" {responseTime}
    public class LogParserResponseTime : ILogParser
    {
        public bool TryParseLog(string message, out LogResponseTime logResponseTime)
        {
            logResponseTime = new LogResponseTime();

            if (string.IsNullOrWhiteSpace(message))
                return false;

            var line = message.AsSpan().TrimEnd();

            // 1. IP — everything before the first space
            var firstSpace = line.IndexOf(' ');
            if (firstSpace < 0) return false;
            var ip = line[..firstSpace].ToString();

            // 2. Skip to timestamp block [...] and past it
            var timestampOpen = line.IndexOf('[');
            if (timestampOpen < 0) return false;
            var timestampClose = line.IndexOf(']');
            if (timestampClose < timestampOpen) return false;

            var afterTimestamp = line[(timestampClose + 1)..];

            // 3. Request field — first quoted block: "METHOD URL PROTOCOL"
            var requestOpen = afterTimestamp.IndexOf('"');
            if (requestOpen < 0) return false;
            var requestClose = afterTimestamp[(requestOpen + 1)..].IndexOf('"');
            if (requestClose < 0) return false;
            requestClose += requestOpen + 1;

            var request = afterTimestamp[(requestOpen + 1)..requestClose];
            var urlStart = request.IndexOf(' ');
            if (urlStart < 0) return false;
            var urlEnd = request[(urlStart + 1)..].IndexOf(' ');
            var url = urlEnd < 0
                ? request[(urlStart + 1)..].ToString()
                : request[(urlStart + 1)..(urlStart + 1 + urlEnd)].ToString();

            var afterRequest = afterTimestamp[(requestClose + 1)..];

            // 4. Skip status and bytes (two space-separated tokens before the referer quote)
            var refererOpen = afterRequest.IndexOf('"');
            if (refererOpen < 0) return false;
            var refererClose = afterRequest[(refererOpen + 1)..].IndexOf('"');
            if (refererClose < 0) return false;
            refererClose += refererOpen + 1;

            var afterReferer = afterRequest[(refererClose + 1)..];

            // 5. User-agent field — next quoted block
            var uaOpen = afterReferer.IndexOf('"');
            if (uaOpen < 0) return false;
            var uaClose = afterReferer[(uaOpen + 1)..].IndexOf('"');
            if (uaClose < 0) return false;
            uaClose += uaOpen + 1;

            // 6. Response time — remaining text after user-agent
            var responseTimeRaw = afterReferer[(uaClose + 1)..].Trim();
            if (!float.TryParse(responseTimeRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var responseTime))
                return false;

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
