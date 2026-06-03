using GameServer.Abstractions;
using Microsoft.Extensions.Logging;
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
    [PersistentState("player", "playerStore")] IPersistentState<PlayerState> state,
    ILogger<PlayerGrain> logger)
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

        if (newZoneId != oldZoneId)
        {
            // 1) 새 존에 먼저 입장. 실패하면 여기서 중단 → 이전 존 멤버십·상태가 그대로 유지된다(공백 없음).
            await GrainFactory.GetGrain<IZoneGrain>(newZoneId)
                .Enter(new PlayerSnapshot(id, state.State.Name, newZoneId, x, y));

            // 2) 입장이 확정된 즉시 상태를 새 존으로 영속화. 영속 ZoneId는 항상 '실제 소속 존'을 가리킨다
            //    → Logout/다음 전환이 올바른 존을 정리한다(영구 2중 소속 방지).
            state.State.X = x;
            state.State.Y = y;
            state.State.ZoneId = newZoneId;
            await state.WriteStateAsync();

            // 3) 이전 존 퇴장은 베스트에포트. 실패해도 전환은 이미 확정이며, 잔여 멤버십은
            //    존 멤버십 TTL/하트비트로 회수한다(로드맵 후속 과제).
            try
            {
                await GrainFactory.GetGrain<IZoneGrain>(oldZoneId).Leave(id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Leave({OldZone}) failed during transition for player {PlayerId}; left as soft-state residue", oldZoneId, id);
            }
        }
        else
        {
            state.State.X = x;
            state.State.Y = y;
            await state.WriteStateAsync();
        }

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
