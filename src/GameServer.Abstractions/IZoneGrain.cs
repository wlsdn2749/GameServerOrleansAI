namespace GameServer.Abstractions;

/// <summary>존(구역) 액터. grain 문자열 키 = 존 ID. 구역 내 플레이어를 추적하고 이벤트를 스트림에 발행한다.</summary>
public interface IZoneGrain : IGrainWithStringKey
{
    /// <summary>플레이어를 구역에 입장시키고 입장 이벤트를 발행한다.</summary>
    Task Enter(PlayerSnapshot snapshot);

    /// <summary>플레이어를 구역에서 퇴장시키고 퇴장 이벤트를 발행한다.</summary>
    Task Leave(long playerId);

    /// <summary>이동을 구역 스트림에 브로드캐스트한다.</summary>
    Task NotifyMove(long playerId, float x, float y);

    /// <summary>현재 구역에 있는 플레이어 스냅샷 목록을 반환한다.</summary>
    Task<IReadOnlyList<PlayerSnapshot>> GetPlayers();
}
