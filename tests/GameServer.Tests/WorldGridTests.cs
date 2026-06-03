using GameServer.Abstractions;

namespace GameServer.Tests;

public class WorldGridTests
{
    [Theory]
    [InlineData(0f, 0f, 0, 0)]
    [InlineData(1f, 1f, 0, 0)]
    [InlineData(WorldGrid.CellSize - 0.01f, 0f, 0, 0)]
    [InlineData(WorldGrid.CellSize, 0f, 1, 0)]
    [InlineData(-0.01f, 0f, -1, 0)]
    [InlineData(-WorldGrid.CellSize, 0f, -1, 0)]
    [InlineData(-WorldGrid.CellSize - 0.01f, 0f, -2, 0)]
    public void CellOf_floors_world_coordinate_to_cell(float x, float y, int expectedCx, int expectedCy)
    {
        var (cx, cy) = WorldGrid.CellOf(x, y);

        Assert.Equal(expectedCx, cx);
        Assert.Equal(expectedCy, cy);
    }

    [Fact]
    public void ZoneId_is_stable_for_same_cell()
    {
        Assert.Equal(WorldGrid.ZoneId(2, -3), WorldGrid.ZoneId(2, -3));
        Assert.NotEqual(WorldGrid.ZoneId(2, -3), WorldGrid.ZoneId(2, 3));
    }

    [Fact]
    public void ZoneIdOf_matches_ZoneId_of_its_cell()
    {
        var (cx, cy) = WorldGrid.CellOf(70f, -10f);

        Assert.Equal(WorldGrid.ZoneId(cx, cy), WorldGrid.ZoneIdOf(70f, -10f));
    }

    [Fact]
    public void Neighbors_returns_3x3_block_including_center()
    {
        var neighbors = WorldGrid.Neighbors(0, 0);

        Assert.Equal(9, neighbors.Count);
        Assert.Contains(WorldGrid.ZoneId(0, 0), neighbors);
        Assert.Contains(WorldGrid.ZoneId(-1, -1), neighbors);
        Assert.Contains(WorldGrid.ZoneId(1, 1), neighbors);
        Assert.Equal(9, neighbors.Distinct().Count());
    }

    [Fact]
    public void Spawn_zone_is_the_default_zone()
    {
        Assert.Equal(WorldConstants.DefaultZoneId, WorldGrid.ZoneIdOf(0f, 0f));
    }
}
