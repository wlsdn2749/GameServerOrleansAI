using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Gateway;

/// <summary>TCP 연결을 수락하고 연결마다 <see cref="GatewaySession"/>을 구동하는 백그라운드 서비스.</summary>
public sealed class TcpGatewayService(
    IClusterClient client,
    ILogger<TcpGatewayService> logger,
    ILoggerFactory loggerFactory) : BackgroundService
{
    public const int Port = 9000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, Port);
        listener.Start();
        logger.LogInformation("TCP gateway listening on {EndPoint}", listener.LocalEndpoint);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = RunSessionAsync(tcp, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task RunSessionAsync(TcpClient tcp, CancellationToken stoppingToken)
    {
        var session = new GatewaySession(tcp, client, loggerFactory.CreateLogger<GatewaySession>());
        try
        {
            await session.RunAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Session terminated unexpectedly");
        }
    }
}
