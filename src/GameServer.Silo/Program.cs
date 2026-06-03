using GameServer.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddMemoryGrainStorage("playerStore");
    silo.AddMemoryGrainStorage("PubSubStore");
    silo.AddMemoryStreams(GameStreams.ProviderName);
});

builder.Logging.AddSimpleConsole(o => o.SingleLine = true);

var host = builder.Build();

Console.WriteLine("GameServer Silo starting (localhost clustering, in-memory storage/streams)...");
await host.RunAsync();
