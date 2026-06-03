using MemoryPack;

namespace GameServer.Protocol;

/// <summary>패킷 record를 MemoryPack으로 직렬화한 뒤 프레이밍까지 묶어주는 헬퍼.</summary>
public static class MessageCodec
{
    /// <summary>패킷을 직렬화하고 <paramref name="opcode"/>로 프레이밍한 바이트 배열을 만든다.</summary>
    public static byte[] Encode<T>(Opcode opcode, T value)
    {
        byte[] payload = MemoryPackSerializer.Serialize(value);
        return PacketCodec.Encode(opcode, payload);
    }

    /// <summary>프레임 payload 바이트를 패킷 record로 역직렬화한다.</summary>
    public static T Decode<T>(ReadOnlySpan<byte> payload)
        => MemoryPackSerializer.Deserialize<T>(payload)
           ?? throw new InvalidDataException($"Failed to decode payload as {typeof(T).Name}.");
}
