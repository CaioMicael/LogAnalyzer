# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet restore
dotnet build
dotnet test
```

Run the Worker (requires RabbitMQ):
```bash
cd LogAnalyzer.Worker
RABBITMQ_HOST=localhost RABBITMQ_QUEUE=log_queue dotnet run
```

Run the API:
```bash
cd LogAnalyzer.API
dotnet run
# Listens on http://localhost:5293 / https://localhost:7049
```

Code formatting check (used in CI):
```bash
dotnet format --verify-no-changes
```

## Architecture

Five-project layered solution targeting .NET 9:

```
Domain ← Infrastructure ← Worker
  ↑
Application (empty, reserved for business logic)
API (currently independent)
```

**LogAnalyzer.Domain** — core abstractions and logic:
- `ILogParser` interface: strategy pattern for pluggable log parsers
- `LogParserResponseTime`: regex parser that extracts `OriginIP`, `RequestURL`, and `ResponseTime` from HTTP access log lines
- `LogResponseTime`: immutable record DTO

**LogAnalyzer.Infrastructure** — external integrations:
- `IMessageConsumer` interface abstracts the message broker
- `RabbitMQConsumer`: async consumer with manual acknowledgment, reads `RABBITMQ_HOST` / `RABBITMQ_QUEUE` from env vars

**LogAnalyzer.Worker** — long-running process:
- `MessageConsumerWorker` (`BackgroundService`): consumes messages from the queue, delegates parsing to `ILogParser`
- `Program.cs` wires up DI: `IMessageConsumer → RabbitMQConsumer`, `ILogParser → LogParserResponseTime`

**LogAnalyzer.API** — ASP.NET Core Web API (minimal APIs setup, currently has only the default weather scaffold)

## Code Style

- Comments must be written in **Portuguese**.

## Key Design Decisions

- `IMessageConsumer` abstraction exists specifically to allow swapping RabbitMQ for Kafka or Azure Service Bus without touching the Worker (see `docs/message_architecture.md`).
- `ILogParser` is a strategy pattern — new log formats should be new implementations of this interface, not modifications to existing parsers.
- Microsoft.ML 5.0.0 is already referenced in Domain for planned anomaly detection features.
