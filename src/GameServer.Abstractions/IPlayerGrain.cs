namespace GameServer.Abstractions;

/// <summary>플레이어 액터. grain 정수 키 = 플레이어 ID.</summary>
public interface IPlayerGrain : IGrainWithIntegerKey
{
    /// <summary>로그인하여 이름을 설정하고 존에 입장한다.</summary>
    Task<LoginResult> Login(string name);

    /// <summary>목표 좌표로 이동하고 소속 존에 브로드캐스트를 요청한다.</summary>
    Task Move(float x, float y);

    /// <summary>현재 스냅샷을 반환한다.</summary>
    Task<PlayerSnapshot> GetSnapshot();

    /// <summary>로그아웃: 현재 소속 존에서 퇴장한다(연결 종료 시 게이트웨이가 호출, 멤버십 단일 진실원).</summary>
    Task Logout();
}
