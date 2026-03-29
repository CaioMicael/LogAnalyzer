using LogAnalyzer.Domain.DTO;

namespace LogAnalyzer.Domain.Interfaces
{
    public interface ILogParser
    {
        bool TryParseLog(string message, out LogResponseTime logResponseTime);
    }
}
