using GameServer.Abstractions;
using Orleans.Runtime;

namespace GameServer.Grains;

/// <summary>플레이어 grain의 영속 상태.</summary>
[GenerateSerializer]
public sealed class PlayerState
{
    [Id(0)] public string Name { get; set; } = "";
    [Id(1)] public string ZoneId { get; set; } = "";
    [Id(2)] public float X { get; set; }
    [Id(3)] public float Y { get; set; }
    [Id(4)] public bool LoggedIn { get; set; }
}

/// <summary>플레이어 액터: 로그인/이동 상태를 보유하고 소속 존 grain과 연동한다.</summary>
public sealed class PlayerGrain(
    [PersistentState("player", "playerStore")] IPersistentState<PlayerState> state)
    : Grain, IPlayerGrain
{
    public async Task<LoginResult> Login(string name)
    {
        long id = this.GetPrimaryKeyLong();
        state.State.Name = name;
        state.State.X = WorldConstants.SpawnX;
        state.State.Y = WorldConstants.SpawnY;
        state.State.ZoneId = WorldGrid.ZoneIdOf(state.State.X, state.State.Y);
        state.State.LoggedIn = true;
        await state.WriteStateAsync();

        var zone = GrainFactory.GetGrain<IZoneGrain>(state.State.ZoneId);
        await zone.Enter(Snapshot(id));

        return new LoginResult(id, state.State.ZoneId, state.State.X, state.State.Y);
    }

    public async Task Move(float x, float y)
    {
        long id = this.GetPrimaryKeyLong();

        // 방어적 검증: 비정상/범위 밖 좌표는 월드 경계로 클램프해 grain을 절대 죽이지 않는다.
        if (!WorldGrid.IsValidPosition(x, y))
            (x, y) = WorldGrid.Clamp(x, y);

        string oldZoneId = state.State.ZoneId;
        string newZoneId = WorldGrid.ZoneIdOf(x, y);

        // 셀 경계를 넘었으면: 새 존에 '먼저' 입장(겹침은 안전), 그 다음 이전 존에서 퇴장.
        // 어느 쪽이 실패해도 "어느 존에도 없는" 공백 상태가 생기지 않게 한다.
        if (newZoneId != oldZoneId)
        {
            await GrainFactory.GetGrain<IZoneGrain>(newZoneId)
                .Enter(new PlayerSnapshot(id, state.State.Name, newZoneId, x, y));
            await GrainFactory.GetGrain<IZoneGrain>(oldZoneId).Leave(id);
        }

        // 전환이 성공한 뒤에 상태를 영속화한다(쓰기-후-전환이 아니라 전환-후-쓰기).
        state.State.X = x;
        state.State.Y = y;
        state.State.ZoneId = newZoneId;
        await state.WriteStateAsync();

        await GrainFactory.GetGrain<IZoneGrain>(newZoneId).NotifyMove(id, x, y);
    }

    public Task<PlayerSnapshot> GetSnapshot()
        => Task.FromResult(Snapshot(this.GetPrimaryKeyLong()));

    public async Task Logout()
    {
        if (!state.State.LoggedIn)
            return;

        await GrainFactory.GetGrain<IZoneGrain>(state.State.ZoneId)
            .Leave(this.GetPrimaryKeyLong());
        state.State.LoggedIn = false;
        await state.WriteStateAsync();
    }

    private PlayerSnapshot Snapshot(long id)
        => new(id, state.State.Name, state.State.ZoneId, state.State.X, state.State.Y);
}
