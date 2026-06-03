using System.Net.Sockets;
using System.Runtime.InteropServices;
using GameServer.Abstractions;
using GameServer.Protocol;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace GameServer.Gateway;

/// <summary>
/// 단일 TCP 연결의 수명을 관리한다. 프레임을 디코드해 grain을 호출하고,
/// 소속 존 스트림을 구독해 브로드캐스트를 소켓으로 다시 밀어준다.
/// </summary>
public sealed class GatewaySession
{
    private static long _idCounter = DateTime.UtcNow.Ticks;

    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly IClusterClient _client;
    private readonly ILogger<GatewaySession> _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly List<byte> _inbound = [];

    private long _playerId;
    private string _zoneId = "";
    private StreamSubscriptionHandle<ZoneEvent>? _subscription;

    public GatewaySession(TcpClient tcp, IClusterClient client, ILogger<GatewaySession> logger)
    {
        _tcp = tcp;
        _stream = tcp.GetStream();
        _client = client;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                int read = await _stream.ReadAsync(buffer, ct);
                if (read == 0)
                    break; // peer closed

                _inbound.AddRange(buffer.AsSpan(0, read));
                await DrainFramesAsync(ct);
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException)
        {
            // client disconnected — normal
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private async Task DrainFramesAsync(CancellationToken ct)
    {
        while (true)
        {
            Opcode opcode;
            byte[] payload;
            int consumed;
            {
                var span = CollectionsMarshal.AsSpan(_inbound);
                if (!PacketCodec.TryReadFrame(span, out opcode, out payload, out consumed))
                    break;
            }
            _inbound.RemoveRange(0, consumed);
            await DispatchAsync(opcode, payload, ct);
        }
    }

    private async Task DispatchAsync(Opcode opcode, byte[] payload, CancellationToken ct)
    {
        switch (opcode)
        {
            case Opcode.LoginReq:
                await HandleLoginAsync(MessageCodec.Decode<LoginRequest>(payload));
                break;

            case Opcode.MoveReq:
                if (_playerId != 0)
                {
                    var move = MessageCodec.Decode<MoveRequest>(payload);
                    await _client.GetGrain<IPlayerGrain>(_playerId).Move(move.X, move.Y);
                }
                break;

            default:
                _logger.LogWarning("Unhandled opcode {Opcode}", opcode);
                break;
        }
    }

    private async Task HandleLoginAsync(LoginRequest request)
    {
        _playerId = Interlocked.Increment(ref _idCounter);
        var player = _client.GetGrain<IPlayerGrain>(_playerId);
        var result = await player.Login(request.Name);
        _zoneId = result.ZoneId;

        var provider = _client.GetStreamProvider(GameStreams.ProviderName);
        var stream = provider.GetStream<ZoneEvent>(
            StreamId.Create(GameStreams.ZoneNamespace, _zoneId));
        _subscription = await stream.SubscribeAsync(OnZoneEventAsync);

        _logger.LogInformation("Player {PlayerId} '{Name}' logged in to {Zone}",
            _playerId, request.Name, _zoneId);

        await SendAsync(Opcode.LoginResp,
            new LoginResponse(result.PlayerId, true, result.ZoneId, result.X, result.Y));
    }

    private async Task OnZoneEventAsync(ZoneEvent evt, StreamSequenceToken? token)
    {
        if (evt is EntityMovedEvent moved)
            await SendAsync(Opcode.EntityMoved, new EntityMoved(moved.PlayerId, moved.X, moved.Y));
    }

    private async Task SendAsync<T>(Opcode opcode, T packet)
    {
        byte[] frame = MessageCodec.Encode(opcode, packet);
        await _sendLock.WaitAsync();
        try
        {
            await _stream.WriteAsync(frame);
            await _stream.FlushAsync();
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            // peer gone; read loop will observe and clean up
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task CleanupAsync()
    {
        if (_subscription is not null)
        {
            try { await _subscription.UnsubscribeAsync(); }
            catch { /* best effort */ }
        }

        if (_playerId != 0 && _zoneId.Length > 0)
        {
            try { await _client.GetGrain<IZoneGrain>(_zoneId).Leave(_playerId); }
            catch { /* best effort */ }
        }

        _tcp.Dispose();
        _logger.LogInformation("Session for player {PlayerId} closed", _playerId);
    }
}
