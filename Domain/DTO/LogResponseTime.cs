namespace LogAnalyzer.Domain.DTO
{
    public record LogResponseTime
    {
        public string? OriginIP { get; set; }
        public string RequestURL { get; set; } = string.Empty;
        public float ResponseTime { get; set; } = float.MinValue;
    }
}
