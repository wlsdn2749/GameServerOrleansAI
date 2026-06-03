using System.Net.Sockets;
using System.Runtime.InteropServices;
using GameServer.Protocol;

// 사용법: dotnet run --project tools/GameServer.TestClient -- <name> [host] [port] [centerX] [centerY]
// centerX/centerY를 주면 그 좌표 근방(±8)에서만 움직인다 → AOI(관심영역) 격리 시연용.
string name = args.Length > 0 ? args[0] : $"player-{Random.Shared.Next(1000, 9999)}";
string host = args.Length > 1 ? args[1] : "127.0.0.1";
int port = args.Length > 2 ? int.Parse(args[2]) : 9000;
float centerX = args.Length > 3 ? float.Parse(args[3]) : 0f;
float centerY = args.Length > 4 ? float.Parse(args[4]) : 0f;

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
    float x = centerX + rng.Next(-8, 9);
    float y = centerY + rng.Next(-8, 9);
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

        case Opcode.EntityEntered:
            var entered = MessageCodec.Decode<EntityEntered>(payload);
            if (entered.PlayerId != myId)
                Console.WriteLine($"[{name}] <- EntityEntered #{entered.PlayerId} '{entered.Name}' at ({entered.X},{entered.Y})");
            break;

        case Opcode.EntityLeft:
            var left = MessageCodec.Decode<EntityLeft>(payload);
            if (left.PlayerId != myId) // 자기 자신의 셀 이동에 따른 self-leave는 표시하지 않음
                Console.WriteLine($"[{name}] <- EntityLeft #{left.PlayerId}");
            break;

        case Opcode.Error:
            var err = MessageCodec.Decode<ErrorPacket>(payload);
            Console.WriteLine($"[{name}] <- Error: {err.Message}");
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
