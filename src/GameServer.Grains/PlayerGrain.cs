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
        string oldZoneId = state.State.ZoneId;
        string newZoneId = WorldGrid.ZoneIdOf(x, y);

        state.State.X = x;
        state.State.Y = y;
        state.State.ZoneId = newZoneId;
        await state.WriteStateAsync();

        // 셀 경계를 넘었으면 이전 존에서 퇴장하고 새 존에 입장한다.
        if (newZoneId != oldZoneId)
        {
            await GrainFactory.GetGrain<IZoneGrain>(oldZoneId).Leave(id);
            await GrainFactory.GetGrain<IZoneGrain>(newZoneId).Enter(Snapshot(id));
        }

        await GrainFactory.GetGrain<IZoneGrain>(newZoneId).NotifyMove(id, x, y);
    }

    public Task<PlayerSnapshot> GetSnapshot()
        => Task.FromResult(Snapshot(this.GetPrimaryKeyLong()));

    private PlayerSnapshot Snapshot(long id)
        => new(id, state.State.Name, state.State.ZoneId, state.State.X, state.State.Y);
}
