using GameServer.Abstractions;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.TestingHost;

namespace GameServer.Tests;

[Collection(ClusterCollection.Name)]
public class ZoneGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;

    [Fact]
    public async Task Enter_then_GetPlayers_contains_player()
    {
        var zone = _cluster.GrainFactory.GetGrain<IZoneGrain>("zone-enter");
        await zone.Enter(new PlayerSnapshot(5, "n", "zone-enter", 0f, 0f));

        var players = await zone.GetPlayers();

        Assert.Contains(players, p => p.PlayerId == 5);
    }

    [Fact]
    public async Task Leave_removes_player()
    {
        var zone = _cluster.GrainFactory.GetGrain<IZoneGrain>("zone-leave");
        await zone.Enter(new PlayerSnapshot(6, "n", "zone-leave", 0f, 0f));

        await zone.Leave(6);
        var players = await zone.GetPlayers();

        Assert.DoesNotContain(players, p => p.PlayerId == 6);
    }

    [Fact]
    public async Task NotifyMove_publishes_EntityMovedEvent_to_zone_stream()
    {
        const string zoneId = "zone-stream";
        var received = new List<ZoneEvent>();
        var movedSeen = new TaskCompletionSource();

        var provider = _cluster.Client.GetStreamProvider(GameStreams.ProviderName);
        var stream = provider.GetStream<ZoneEvent>(
            StreamId.Create(GameStreams.ZoneNamespace, zoneId));

        var handle = await stream.SubscribeAsync((evt, _) =>
        {
            received.Add(evt);
            if (evt is EntityMovedEvent)
                movedSeen.TrySetResult();
            return Task.CompletedTask;
        });

        var zone = _cluster.GrainFactory.GetGrain<IZoneGrain>(zoneId);
        await zone.Enter(new PlayerSnapshot(7, "n", zoneId, 0f, 0f));
        await zone.NotifyMove(7, 9.5f, -4f);

        await movedSeen.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await handle.UnsubscribeAsync();

        var moved = received.OfType<EntityMovedEvent>().Single();
        Assert.Equal(7, moved.PlayerId);
        Assert.Equal(9.5f, moved.X);
        Assert.Equal(-4f, moved.Y);
    }
}
