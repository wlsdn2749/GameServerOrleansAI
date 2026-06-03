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
        state.State.ZoneId = WorldConstants.DefaultZoneId;
        state.State.X = 0f;
        state.State.Y = 0f;
        state.State.LoggedIn = true;
        await state.WriteStateAsync();

        var zone = GrainFactory.GetGrain<IZoneGrain>(state.State.ZoneId);
        await zone.Enter(Snapshot(id));

        return new LoginResult(id, state.State.ZoneId, state.State.X, state.State.Y);
    }

    public async Task Move(float x, float y)
    {
        state.State.X = x;
        state.State.Y = y;
        await state.WriteStateAsync();

        var zone = GrainFactory.GetGrain<IZoneGrain>(state.State.ZoneId);
        await zone.NotifyMove(this.GetPrimaryKeyLong(), x, y);
    }

    public Task<PlayerSnapshot> GetSnapshot()
        => Task.FromResult(Snapshot(this.GetPrimaryKeyLong()));

    private PlayerSnapshot Snapshot(long id)
        => new(id, state.State.Name, state.State.ZoneId, state.State.X, state.State.Y);
}
