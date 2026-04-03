using LogAnalyzer.Core.Interfaces;
using LogAnalyzer.Core.RabbitMQ;
using LogAnalyzer.Domain.Interfaces;
using LogAnalyzer.Domain.Parsers;
using LogAnalyzer.Infrastructure.MongoDB;
using LogAnalyzer.Workers.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
builder.Services.AddSingleton<ILogParser, LogParserResponseTime>();
builder.Services.AddHostedService<MessageConsumerWorker>();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

using var host = builder.Build();

await host.RunAsync();
