namespace GameServer.Abstractions;

/// <summary>
/// 월드를 고정 크기 정사각형 셀의 격자로 나누는 순수 함수 모음.
/// 사일로(존 배정)와 게이트웨이(관심영역 구독)가 동일한 규칙을 공유하도록 한곳에 둔다.
/// </summary>
public static class WorldGrid
{
    /// <summary>셀 한 변의 월드 단위 길이.</summary>
    public const float CellSize = 32f;

    /// <summary>월드 좌표가 속한 셀 좌표(내림 분할, 음수 포함).</summary>
    public static (int CellX, int CellY) CellOf(float x, float y)
        => ((int)MathF.Floor(x / CellSize), (int)MathF.Floor(y / CellSize));

    /// <summary>셀 좌표의 존 식별자.</summary>
    public static string ZoneId(int cellX, int cellY) => $"zone_{cellX}_{cellY}";

    /// <summary>월드 좌표가 속한 셀의 존 식별자.</summary>
    public static string ZoneIdOf(float x, float y)
    {
        var (cx, cy) = CellOf(x, y);
        return ZoneId(cx, cy);
    }

    /// <summary>해당 셀과 8방향 이웃을 포함한 3x3 블록의 존 식별자 목록(관심영역).</summary>
    public static IReadOnlyList<string> Neighbors(int cellX, int cellY)
    {
        var result = new List<string>(9);
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                result.Add(ZoneId(cellX + dx, cellY + dy));
        return result;
    }

    /// <summary>월드 좌표를 중심으로 한 3x3 관심영역 존 식별자 목록.</summary>
    public static IReadOnlyList<string> NeighborsOf(float x, float y)
    {
        var (cx, cy) = CellOf(x, y);
        return Neighbors(cx, cy);
    }
}
