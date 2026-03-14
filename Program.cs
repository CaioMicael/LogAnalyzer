using LogAnalyzer.LogAnalyzerCore.Interfaces;
using LogAnalyzer.LogAnalyzerCore.RabbitMQ;
using LogAnalyzer.LogAnalyzerCore.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
builder.Services.AddHostedService<MessageConsumerWorker>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
