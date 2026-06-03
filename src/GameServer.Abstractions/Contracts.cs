namespace GameServer.Abstractions;

/// <summary>플레이어의 현재 상태 스냅샷.</summary>
[GenerateSerializer]
public record PlayerSnapshot(
    [property: Id(0)] long PlayerId,
    [property: Id(1)] string Name,
    [property: Id(2)] string ZoneId,
    [property: Id(3)] float X,
    [property: Id(4)] float Y);

/// <summary>로그인 결과: 배정된 존과 스폰 좌표.</summary>
[GenerateSerializer]
public record LoginResult(
    [property: Id(0)] long PlayerId,
    [property: Id(1)] string ZoneId,
    [property: Id(2)] float X,
    [property: Id(3)] float Y);

/// <summary>존 스트림에 흐르는 이벤트의 다형 베이스. 구독자는 패턴 매칭으로 분기한다.</summary>
[GenerateSerializer]
public abstract record ZoneEvent;

/// <summary>존 스트림 이벤트: 엔티티 이동.</summary>
[GenerateSerializer]
public record EntityMovedEvent(
    [property: Id(0)] long PlayerId,
    [property: Id(1)] float X,
    [property: Id(2)] float Y) : ZoneEvent;

/// <summary>존 스트림 이벤트: 엔티티 입장.</summary>
[GenerateSerializer]
public record EntityEnteredEvent(
    [property: Id(0)] PlayerSnapshot Player) : ZoneEvent;

/// <summary>존 스트림 이벤트: 엔티티 퇴장.</summary>
[GenerateSerializer]
public record EntityLeftEvent(
    [property: Id(0)] long PlayerId) : ZoneEvent;

/// <summary>존 단위 브로드캐스트에 쓰는 스트림 식별 상수.</summary>
public static class GameStreams
{
    /// <summary>Orleans 스트림 프로바이더 이름. 사일로/게이트웨이/테스트에서 동일하게 등록한다.</summary>
    public const string ProviderName = "game";

    /// <summary>존 이벤트 스트림 네임스페이스.</summary>
    public const string ZoneNamespace = "zone";
}

/// <summary>월드 상수.</summary>
public static class WorldConstants
{
    /// <summary>PR #1 단계의 단일 존 식별자(AOI 격자 분할은 추후 PR).</summary>
    public const string DefaultZoneId = "zone-0";
}
