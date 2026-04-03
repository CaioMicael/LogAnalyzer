using LogAnalyzer.Domain.Parsers;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogAnalyzer.Tests.Parsers
{
    public class LogParserResponseTimeTests
    {
        private readonly LogParserResponseTime _parser =
            new(NullLogger<LogParserResponseTime>.Instance);

        [Fact]
        public void TryParseLog_LinhaValida_RetornaTrue()
        {
            var linha = "233.223.117.90 - - [27/Dec/2037:12:00:00 +0530] \"DELETE /usr/admin HTTP/1.0\" 502 4963 \"-\" \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4380.0 Safari/537.36 Edg/89.0.759.0\" 4";

            var resultado = _parser.TryParseLog(linha, out var log);

            Assert.True(resultado);
            Assert.Equal("233.223.117.90", log.OriginIP);
            Assert.Equal("/usr/admin", log.RequestURL);
            Assert.Equal(4, log.ResponseTime);
        }

        [Fact]
        public void TryParseLog_LinhaTruncadaSemTempoDeResposta_RetornaFalse()
        {
            var linha = "162.253.4.179 - - [27/Dec/2037:12:00:00 +0530] \"GET /usr/admin/developer HTTP/1.0\" 200 5041 \"http://www.parker-miller.org/tag/list/list/privacy/\" \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like G";

            var resultado = _parser.TryParseLog(linha, out _);

            Assert.False(resultado);
        }
    }
}
