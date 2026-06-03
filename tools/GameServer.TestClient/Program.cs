using System.Net.Sockets;
using System.Runtime.InteropServices;
using GameServer.Protocol;

// 사용법: dotnet run --project tools/GameServer.TestClient -- <name> [host] [port]
string name = args.Length > 0 ? args[0] : $"player-{Random.Shared.Next(1000, 9999)}";
string host = args.Length > 1 ? args[1] : "127.0.0.1";
int port = args.Length > 2 ? int.Parse(args[2]) : 9000;

using var tcp = new TcpClient();
await tcp.ConnectAsync(host, port);
var stream = tcp.GetStream();
Console.WriteLine($"[{name}] connected to {host}:{port}");

long myId = 0;

// 수신 루프: 서버에서 오는 프레임을 디코드해 출력한다.
var reader = Task.Run(async () =>
{
    var inbound = new List<byte>();
    var buffer = new byte[4096];
    while (true)
    {
        int read;
        try { read = await stream.ReadAsync(buffer); }
        catch { break; }
        if (read == 0) break;
        inbound.AddRange(buffer.AsSpan(0, read));

        while (true)
        {
            Opcode opcode;
            byte[] payload;
            int consumed;
            {
                var span = CollectionsMarshal.AsSpan(inbound);
                if (!PacketCodec.TryReadFrame(span, out opcode, out payload, out consumed))
                    break;
            }
            inbound.RemoveRange(0, consumed);
            HandlePacket(opcode, payload);
        }
    }
});

// 로그인
await SendAsync(Opcode.LoginReq, new LoginRequest(name));

// 2초마다 임의 좌표로 이동 요청
var rng = new Random();
while (tcp.Connected)
{
    await Task.Delay(2000);
    float x = rng.Next(0, 100);
    float y = rng.Next(0, 100);
    Console.WriteLine($"[{name}] -> Move({x}, {y})");
    await SendAsync(Opcode.MoveReq, new MoveRequest(x, y));
}

await reader;
return;

void HandlePacket(Opcode opcode, byte[] payload)
{
    switch (opcode)
    {
        case Opcode.LoginResp:
            var resp = MessageCodec.Decode<LoginResponse>(payload);
            myId = resp.PlayerId;
            Console.WriteLine($"[{name}] <- LoginResp id={resp.PlayerId} zone={resp.ZoneId} spawn=({resp.X},{resp.Y})");
            break;

        case Opcode.EntityMoved:
            var moved = MessageCodec.Decode<EntityMoved>(payload);
            string who = moved.PlayerId == myId ? "me" : $"#{moved.PlayerId}";
            Console.WriteLine($"[{name}] <- EntityMoved {who} -> ({moved.X},{moved.Y})");
            break;

        default:
            Console.WriteLine($"[{name}] <- {opcode} ({payload.Length} bytes)");
            break;
    }
}

async Task SendAsync<T>(Opcode opcode, T packet)
{
    byte[] frame = MessageCodec.Encode(opcode, packet);
    await stream.WriteAsync(frame);
    await stream.FlushAsync();
}
