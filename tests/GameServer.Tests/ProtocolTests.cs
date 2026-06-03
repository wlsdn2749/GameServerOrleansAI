using GameServer.Protocol;

namespace GameServer.Tests;

public class ProtocolTests
{
    [Fact]
    public void Encode_then_TryReadFrame_round_trips_opcode_and_payload()
    {
        var original = new LoginRequest("hero");
        byte[] frame = MessageCodec.Encode(Opcode.LoginReq, original);

        bool ok = PacketCodec.TryReadFrame(frame, out var opcode, out var payload, out int consumed);

        Assert.True(ok);
        Assert.Equal(Opcode.LoginReq, opcode);
        Assert.Equal(frame.Length, consumed);

        var decoded = MessageCodec.Decode<LoginRequest>(payload);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void TryReadFrame_returns_false_when_buffer_incomplete()
    {
        byte[] frame = MessageCodec.Encode(Opcode.MoveReq, new MoveRequest(1.5f, 2.5f));
        // Only give a partial buffer (header not fully present).
        var partial = frame.AsSpan(0, 3).ToArray();

        bool ok = PacketCodec.TryReadFrame(partial, out _, out _, out int consumed);

        Assert.False(ok);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void TryReadFrame_reads_first_of_two_concatenated_frames()
    {
        byte[] f1 = MessageCodec.Encode(Opcode.MoveReq, new MoveRequest(1f, 2f));
        byte[] f2 = MessageCodec.Encode(Opcode.EntityMoved, new EntityMoved(42, 3f, 4f));
        byte[] combined = [.. f1, .. f2];

        bool ok = PacketCodec.TryReadFrame(combined, out var opcode, out var payload, out int consumed);

        Assert.True(ok);
        Assert.Equal(Opcode.MoveReq, opcode);
        Assert.Equal(f1.Length, consumed);
        Assert.Equal(new MoveRequest(1f, 2f), MessageCodec.Decode<MoveRequest>(payload));
    }

    [Fact]
    public void EntityMoved_round_trips_through_full_codec()
    {
        var moved = new EntityMoved(7, 10.25f, -3.5f);
        byte[] frame = MessageCodec.Encode(Opcode.EntityMoved, moved);

        PacketCodec.TryReadFrame(frame, out var opcode, out var payload, out _);

        Assert.Equal(Opcode.EntityMoved, opcode);
        Assert.Equal(moved, MessageCodec.Decode<EntityMoved>(payload));
    }
}
