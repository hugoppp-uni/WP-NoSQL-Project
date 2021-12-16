namespace TwitterTest;

public static class Extensions
{
    //https://stackoverflow.com/a/10629938/12867415
    public static IEnumerable<IEnumerable<T>> GetKCombinations<T>(this IEnumerable<T> list, int length) where T : IComparable
    {
        if (length == 1) return list.Select(t => new T[] { t });

        return GetKCombinations(list, length - 1)
            .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                (t1, t2) => t1.Concat(new T[] { t2 }));
    }

}
