using GameServer.Abstractions;
using Orleans.TestingHost;

namespace GameServer.Tests;

[Collection(ClusterCollection.Name)]
public class ZoneTransitionTests(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;

    [Fact]
    public async Task Move_across_cell_boundary_transfers_player_to_new_zone()
    {
        var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(2001);
        await player.Login("traveler");

        // (0,0) -> spawn zone; move far enough to land in a different cell.
        const float farX = WorldGrid.CellSize * 3 + 5f;
        const float farY = WorldGrid.CellSize * 3 + 5f;
        await player.Move(farX, farY);

        var snapshot = await player.GetSnapshot();
        string newZoneId = WorldGrid.ZoneIdOf(farX, farY);

        Assert.NotEqual(WorldConstants.DefaultZoneId, newZoneId); // sanity: actually changed cell
        Assert.Equal(newZoneId, snapshot.ZoneId);

        var oldZone = _cluster.GrainFactory.GetGrain<IZoneGrain>(WorldConstants.DefaultZoneId);
        var newZone = _cluster.GrainFactory.GetGrain<IZoneGrain>(newZoneId);
        Assert.DoesNotContain(await oldZone.GetPlayers(), p => p.PlayerId == 2001);
        Assert.Contains(await newZone.GetPlayers(), p => p.PlayerId == 2001);
    }

    [Fact]
    public async Task Move_with_non_finite_coordinates_is_clamped_not_crashed()
    {
        var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(2003);
        await player.Login("griefer");

        // 악의적 좌표: grain이 죽지 않고 유효 존에 남아야 한다.
        await player.Move(float.NaN, float.PositiveInfinity);

        var snapshot = await player.GetSnapshot();
        Assert.True(WorldGrid.IsValidPosition(snapshot.X, snapshot.Y));
        Assert.Equal(WorldGrid.ZoneIdOf(snapshot.X, snapshot.Y), snapshot.ZoneId);
    }

    [Fact]
    public async Task Logout_removes_player_from_current_zone()
    {
        var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(2004);
        await player.Login("leaver");

        await player.Logout();

        var zone = _cluster.GrainFactory.GetGrain<IZoneGrain>(WorldConstants.DefaultZoneId);
        Assert.DoesNotContain(await zone.GetPlayers(), p => p.PlayerId == 2004);
    }

    [Fact]
    public async Task Move_within_same_cell_keeps_zone()
    {
        var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(2002);
        await player.Login("homebody");

        // Stay inside the spawn cell.
        await player.Move(WorldGrid.CellSize / 2f, WorldGrid.CellSize / 2f);

        var snapshot = await player.GetSnapshot();
        Assert.Equal(WorldConstants.DefaultZoneId, snapshot.ZoneId);

        var zone = _cluster.GrainFactory.GetGrain<IZoneGrain>(WorldConstants.DefaultZoneId);
        Assert.Contains(await zone.GetPlayers(), p => p.PlayerId == 2002);
    }
}
