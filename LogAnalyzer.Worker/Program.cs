using LogAnalyzer.Core.Interfaces;
using LogAnalyzer.Core.RabbitMQ;
using LogAnalyzer.Domain.Interfaces;
using LogAnalyzer.Domain.Parsers;
using LogAnalyzer.Workers.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
builder.Services.AddSingleton<ILogParser, LogParserResponseTime>();
builder.Services.AddHostedService<MessageConsumerWorker>();

using var host = builder.Build();

await host.RunAsync();
