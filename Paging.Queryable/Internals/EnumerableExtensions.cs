using System.Collections;

namespace Paging.Queryable.Internals
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerable Cast(this IEnumerable enumerable, Type elementType)
        {
            var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast));
            var genericCastMethod = castMethod!.MakeGenericMethod(elementType);
            var castList = genericCastMethod.Invoke(null, new object[] { enumerable });

            return (IEnumerable)castList!;
        }
    }
}
