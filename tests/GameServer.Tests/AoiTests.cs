using GameServer.Abstractions;

namespace GameServer.Tests;

public class AoiTests
{
    [Fact]
    public void Diff_first_subscription_subscribes_all_nine()
    {
        var desired = WorldGrid.Neighbors(0, 0);

        var (toSubscribe, toUnsubscribe) = Aoi.Diff([], desired);

        Assert.Equal(9, toSubscribe.Count);
        Assert.Empty(toUnsubscribe);
    }

    [Fact]
    public void Diff_same_cell_produces_no_churn()
    {
        var current = WorldGrid.Neighbors(0, 0);
        var desired = WorldGrid.Neighbors(0, 0);

        var (toSubscribe, toUnsubscribe) = Aoi.Diff(current, desired);

        Assert.Empty(toSubscribe);
        Assert.Empty(toUnsubscribe);
    }

    [Fact]
    public void Diff_one_step_move_swaps_three_zones()
    {
        var current = WorldGrid.Neighbors(0, 0);
        var desired = WorldGrid.Neighbors(1, 0); // 한 칸 이동: 3개 빠지고 3개 새로

        var (toSubscribe, toUnsubscribe) = Aoi.Diff(current, desired);

        Assert.Equal(3, toSubscribe.Count);
        Assert.Equal(3, toUnsubscribe.Count);
    }

    [Fact]
    public void Diff_diagonal_move_swaps_five_zones()
    {
        var current = WorldGrid.Neighbors(0, 0);
        var desired = WorldGrid.Neighbors(1, 1); // 대각 이동: 5개 빠지고 5개 새로

        var (toSubscribe, toUnsubscribe) = Aoi.Diff(current, desired);

        Assert.Equal(5, toSubscribe.Count);
        Assert.Equal(5, toUnsubscribe.Count);
    }
}
