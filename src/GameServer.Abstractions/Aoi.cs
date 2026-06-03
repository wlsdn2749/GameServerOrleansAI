namespace GameServer.Abstractions;

/// <summary>관심영역(AOI) 구독 집합 차이 계산 — I/O와 분리된 순수 함수로 단위 테스트 가능하게 둔다.</summary>
public static class Aoi
{
    /// <summary>
    /// 현재 구독 집합 <paramref name="current"/>에서 목표 집합 <paramref name="desired"/>로 가기 위해
    /// 새로 구독할 존과 해제할 존을 계산한다.
    /// </summary>
    public static (IReadOnlyList<string> ToSubscribe, IReadOnlyList<string> ToUnsubscribe) Diff(
        IEnumerable<string> current, IEnumerable<string> desired)
    {
        var currentSet = current as HashSet<string> ?? [.. current];
        var desiredSet = desired as HashSet<string> ?? [.. desired];

        var toSubscribe = desiredSet.Where(z => !currentSet.Contains(z)).ToList();
        var toUnsubscribe = currentSet.Where(z => !desiredSet.Contains(z)).ToList();
        return (toSubscribe, toUnsubscribe);
    }
}
