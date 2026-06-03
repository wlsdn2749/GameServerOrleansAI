using GameServer.Abstractions;
using Orleans.Runtime;
using Orleans.Streams;

namespace GameServer.Grains;

/// <summary>존 액터: 구역 내 플레이어 멤버십을 추적하고 이벤트를 존 스트림에 발행한다.</summary>
public sealed class ZoneGrain : Grain, IZoneGrain
{
    private readonly Dictionary<long, PlayerSnapshot> _players = [];
    private IAsyncStream<ZoneEvent> _stream = null!;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var provider = this.GetStreamProvider(GameStreams.ProviderName);
        var streamId = StreamId.Create(GameStreams.ZoneNamespace, this.GetPrimaryKeyString());
        _stream = provider.GetStream<ZoneEvent>(streamId);
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task Enter(PlayerSnapshot snapshot)
    {
        _players[snapshot.PlayerId] = snapshot;
        await _stream.OnNextAsync(new EntityEnteredEvent(snapshot));
    }

    public async Task Leave(long playerId)
    {
        if (_players.Remove(playerId))
            await _stream.OnNextAsync(new EntityLeftEvent(playerId));
    }

    public async Task NotifyMove(long playerId, float x, float y)
    {
        if (_players.TryGetValue(playerId, out var snapshot))
            _players[playerId] = snapshot with { X = x, Y = y };

        await _stream.OnNextAsync(new EntityMovedEvent(playerId, x, y));
    }

    public Task<IReadOnlyList<PlayerSnapshot>> GetPlayers()
        => Task.FromResult<IReadOnlyList<PlayerSnapshot>>(_players.Values.ToArray());
}
