using MemoryPack;

namespace GameServer.Protocol;

/// <summary>클라이언트 → 서버: 로그인 요청.</summary>
[MemoryPackable]
public partial record LoginRequest(string Name);

/// <summary>서버 → 클라이언트: 로그인 응답(배정된 플레이어/존/스폰 좌표).</summary>
[MemoryPackable]
public partial record LoginResponse(long PlayerId, bool Success, string ZoneId, float X, float Y);

/// <summary>클라이언트 → 서버: 이동 요청(목표 좌표).</summary>
[MemoryPackable]
public partial record MoveRequest(float X, float Y);

/// <summary>서버 → 클라이언트: 같은 존의 엔티티가 이동했음을 알리는 브로드캐스트.</summary>
[MemoryPackable]
public partial record EntityMoved(long PlayerId, float X, float Y);

/// <summary>서버 → 클라이언트: 오류 통지.</summary>
[MemoryPackable]
public partial record ErrorPacket(string Message);
