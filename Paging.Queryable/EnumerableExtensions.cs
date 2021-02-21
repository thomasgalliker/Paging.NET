using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Paging.Queryable
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerable Cast(this IEnumerable enumerable, Type elementType)
        {
            var castMethod = typeof(Enumerable).GetMethod("Cast");
            var genericCastMethod = castMethod.MakeGenericMethod(elementType);
            var castList = genericCastMethod.Invoke(null, new object[] { enumerable });

            return (IEnumerable)castList;
        }
    }
}
