using LogAnalyzer.Core.Interfaces;
using LogAnalyzer.Domain.DTO;
using LogAnalyzer.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
                // Cada mensagem contém um arquivo inteiro; cada linha é um registro de log
                var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                var stopwatchLote = Stopwatch.StartNew();
                var registrosParseados = 0;

                foreach (var line in lines)
                {
                    // Mede o tempo de parse de cada registro individualmente
                    var stopwatchRegistro = Stopwatch.StartNew();
                    var sucesso = _logParser.TryParseLog(line, out var logResponseTime);
                    stopwatchRegistro.Stop();

                    if (sucesso)
                    {
                        lock (_parsedLogsLock)
                        {
                            _parsedLogs.Add(logResponseTime);
                        }

                        registrosParseados++;
                        _logger.LogDebug("Registro parseado em {ElapsedMicrossegundos}µs — IP: {IP} | URL: {URL} | Tempo de resposta: {TempoResposta}ms",
                            stopwatchRegistro.Elapsed.TotalMicroseconds,
                            logResponseTime.OriginIP,
                            logResponseTime.RequestURL,
                            logResponseTime.ResponseTime);
                    }
                }

                stopwatchLote.Stop();

                // Resumo do lote para avaliar performance geral
                _logger.LogInformation(
                    "Lote processado: {Parseados}/{Total} registros em {ElapsedMs}ms (média: {MediaMicrossegundos}µs/registro)",
                    registrosParseados,
                    lines.Length,
                    stopwatchLote.Elapsed.TotalMilliseconds,
                    registrosParseados > 0 ? stopwatchLote.Elapsed.TotalMicroseconds / registrosParseados : 0);

                await Task.CompletedTask;
            }, stoppingToken);
        }
    }
}
