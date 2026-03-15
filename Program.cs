using LogAnalyzer.Domain.Workers;
using LogAnalyzer.LogAnalyzerCore.Interfaces;
using LogAnalyzer.LogAnalyzerCore.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
builder.Services.AddHostedService<MessageConsumerWorker>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
