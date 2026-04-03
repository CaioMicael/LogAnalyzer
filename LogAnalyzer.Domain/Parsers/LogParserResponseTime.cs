using LogAnalyzer.Domain.DTO;
using LogAnalyzer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LogAnalyzer.Domain.Parsers
{
    // Realiza o parse do Apache Combined Log Format com campo extra de tempo de resposta:
    // {ip} - - [{timestamp}] "{method} {url} {protocol}" {status} {bytes} "{referer}" "{user-agent}" {responseTime}
    public class LogParserResponseTime : ILogParser
    {
        private readonly ILogger<LogParserResponseTime> _logger;

        public LogParserResponseTime(ILogger<LogParserResponseTime> logger)
        {
            _logger = logger;
        }

        public bool TryParseLog(string message, out LogResponseTime logResponseTime)
        {
            logResponseTime = new LogResponseTime();

            if (string.IsNullOrWhiteSpace(message))
                return false;

            var sw = Stopwatch.StartNew();
            var line = message.AsSpan().TrimEnd();
            _logger.LogDebug("[Parser] TrimEnd: {µs}µs", sw.Elapsed.TotalMicroseconds); sw.Restart();

            // 1. IP — tudo antes do primeiro espaço
            var firstSpace = line.IndexOf(' ');
            if (firstSpace < 0) return false;
            var ip = line[..firstSpace].ToString();
            _logger.LogDebug("[Parser] Etapa 1 - IP: {µs}µs", sw.Elapsed.TotalMicroseconds); sw.Restart();

            // 2. Avança até o bloco de timestamp [...] e passa por ele
            var timestampOpen = line.IndexOf('[');
            if (timestampOpen < 0) return false;
            var timestampClose = line.IndexOf(']');
            if (timestampClose < timestampOpen) return false;

            var afterTimestamp = line[(timestampClose + 1)..];
            _logger.LogDebug("[Parser] Etapa 2 - Timestamp: {µs}µs", sw.Elapsed.TotalMicroseconds); sw.Restart();

            // 3. Campo de requisição — primeiro bloco entre aspas: "METHOD URL PROTOCOL"
            var requestOpen = afterTimestamp.IndexOf('"');
            if (requestOpen < 0) return false;
            var requestClose = afterTimestamp[(requestOpen + 1)..].IndexOf('"');
            if (requestClose < 0) return false;
            requestClose += requestOpen + 1;

            // Extrai a URL do campo de requisição (segundo token: METHOD [URL] PROTOCOL)
            var request = afterTimestamp[(requestOpen + 1)..requestClose];
            var urlStart = request.IndexOf(' ');
            if (urlStart < 0) return false;
            var urlEnd = request[(urlStart + 1)..].IndexOf(' ');
            var url = urlEnd < 0
                ? request[(urlStart + 1)..].ToString()
                : request[(urlStart + 1)..(urlStart + 1 + urlEnd)].ToString();

            var afterRequest = afterTimestamp[(requestClose + 1)..];
            _logger.LogDebug("[Parser] Etapa 3 - Request/URL: {µs}µs", sw.Elapsed.TotalMicroseconds); sw.Restart();

            // 4. Pula status code e bytes; localiza o referer entre aspas
            var refererOpen = afterRequest.IndexOf('"');
            if (refererOpen < 0) return false;
            var refererClose = afterRequest[(refererOpen + 1)..].IndexOf('"');
            if (refererClose < 0) return false;
            refererClose += refererOpen + 1;

            var afterReferer = afterRequest[(refererClose + 1)..];
            _logger.LogDebug("[Parser] Etapa 4 - Referer: {µs}µs", sw.Elapsed.TotalMicroseconds); sw.Restart();

            // 5. User-agent — próximo bloco entre aspas
            var uaOpen = afterReferer.IndexOf('"');
            if (uaOpen < 0) return false;
            var uaClose = afterReferer[(uaOpen + 1)..].IndexOf('"');
            if (uaClose < 0) return false;
            uaClose += uaOpen + 1;
            _logger.LogDebug("[Parser] Etapa 5 - User-Agent: {µs}µs", sw.Elapsed.TotalMicroseconds); sw.Restart();

            // 6. Tempo de resposta — texto restante após o user-agent (valor inteiro em ms)
            var responseTimeRaw = afterReferer[(uaClose + 1)..].Trim();
            if (!int.TryParse(responseTimeRaw, out var responseTime))
                return false;
            _logger.LogDebug("[Parser] Etapa 6 - ResponseTime: {µs}µs", sw.Elapsed.TotalMicroseconds);

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
