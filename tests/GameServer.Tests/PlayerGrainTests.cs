using GameServer.Abstractions;
using Orleans.TestingHost;

namespace GameServer.Tests;

[Collection(ClusterCollection.Name)]
public class PlayerGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;

    [Fact]
    public async Task Login_assigns_grain_key_as_player_id_and_default_zone()
    {
        const long id = 1001;
        var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(id);

        var result = await player.Login("hero");

        Assert.Equal(id, result.PlayerId);
        Assert.Equal(WorldConstants.DefaultZoneId, result.ZoneId);
    }

    [Fact]
    public async Task GetSnapshot_reflects_login_name()
    {
        var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(1002);

        await player.Login("archer");
        var snapshot = await player.GetSnapshot();

        Assert.Equal(1002, snapshot.PlayerId);
        Assert.Equal("archer", snapshot.Name);
        Assert.Equal(WorldConstants.DefaultZoneId, snapshot.ZoneId);
    }

    [Fact]
    public async Task Move_updates_position()
    {
        var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(1003);
        await player.Login("mage");

        await player.Move(12.5f, -7.25f);
        var snapshot = await player.GetSnapshot();

        Assert.Equal(12.5f, snapshot.X);
        Assert.Equal(-7.25f, snapshot.Y);
    }
}
