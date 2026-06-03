using GameServer.Abstractions;
using GameServer.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
    client.AddMemoryStreams(GameStreams.ProviderName);
});

builder.Logging.AddSimpleConsole(o => o.SingleLine = true);
builder.Services.AddHostedService<TcpGatewayService>();

var host = builder.Build();

Console.WriteLine($"GameServer Gateway starting (TCP :{TcpGatewayService.Port}, Orleans client)...");
await host.RunAsync();
