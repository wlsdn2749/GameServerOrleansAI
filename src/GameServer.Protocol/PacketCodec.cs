using System.Buffers.Binary;

namespace GameServer.Protocol;

/// <summary>
/// 와이어 프레이밍: <c>[length:int32 LE][opcode:uint16 LE][payload]</c>.
/// length 필드는 opcode(2바이트) + payload 길이의 합이다.
/// </summary>
public static class PacketCodec
{
    public const int HeaderSize = sizeof(int) + sizeof(ushort); // 6

    /// <summary>opcode와 payload를 하나의 프레임 바이트 배열로 인코딩한다.</summary>
    public static byte[] Encode(Opcode opcode, ReadOnlySpan<byte> payload)
    {
        int length = sizeof(ushort) + payload.Length;
        var frame = new byte[sizeof(int) + length];
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0), length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(sizeof(int)), (ushort)opcode);
        payload.CopyTo(frame.AsSpan(HeaderSize));
        return frame;
    }

    /// <summary>
    /// 버퍼 앞에서 완전한 프레임 하나를 읽는다. 프레임이 아직 완성되지 않았으면 false를 반환하고
    /// <paramref name="consumed"/>는 0이다. 성공 시 <paramref name="consumed"/>는 소비한 바이트 수다.
    /// </summary>
    public static bool TryReadFrame(ReadOnlySpan<byte> buffer, out Opcode opcode, out byte[] payload, out int consumed)
    {
        opcode = Opcode.None;
        payload = [];
        consumed = 0;

        if (buffer.Length < sizeof(int))
            return false;

        int length = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        int frameTotal = sizeof(int) + length;
        if (length < sizeof(ushort) || buffer.Length < frameTotal)
            return false;

        opcode = (Opcode)BinaryPrimitives.ReadUInt16LittleEndian(buffer[sizeof(int)..]);
        int payloadLength = length - sizeof(ushort);
        payload = buffer.Slice(HeaderSize, payloadLength).ToArray();
        consumed = frameTotal;
        return true;
    }
}
