using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
#if NETSTANDARD1_3
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
#else
using System.Linq.Dynamic;
#endif

namespace Paging.Queryable
{
    public static class QueryableExtensions
    {
        public static IQueryable<TEntity> ApplyFilter<TEntity>(this IQueryable<TEntity> queryable, IDictionary<string, object> filters)
        {
            foreach (var filter in filters)
            {
                if (filter.Value == null)
                {
                    // Ignore null values
                    continue;
                }

                if (filter.Value is string stringValue)
                {
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        // Ignore null/empty values
                        Trace.WriteLine($"Filter value for key '{filter.Key}' is null or empty.", "Warning");
                        continue;
                    }

                    var op = stringValue[0];
                    if (op.IsOperator())
                    {
                        // If the given string value contains a comparison operator, we apply it
                        queryable = queryable.TryWhere($"{filter.Key} {filter.Value}");
                    }
                    else
                    {
                        // No comparison operator means: key.Contains(value)
                        queryable = queryable.TryWhere($"{filter.Key}.ToString().ToLower().Contains(\"{stringValue.Replace("\"", "").ToLower()}\")");
                    }
                }
                else if (filter.Value.IsNumericType() || filter.Value is bool)
                {
                    queryable = queryable.TryWhere($"{filter.Key} == {filter.Value}");
                }
                else if (filter.Value is DateTime dateTimeValue)
                {
                    queryable = queryable.TryWhere($"{filter.Key} == @0", dateTimeValue);
                }
                else if (filter.Value is IDictionary<string, object> ranges)
                {
                    foreach (var range in ranges)
                    {
                        if (string.IsNullOrEmpty(range.Key))
                        {
                            // Ignore null/empty operator
                            Trace.WriteLine($"Filter value for key '{filter.Key}' is null or empty.", "Warning");
                            continue;
                        }

                        var op = range.Key[0];
                        if (op.IsOperator())
                        {
                            if (range.Value is string && DateTime.TryParse($"{range.Value}", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedDateTimeValue))
                            {
                                queryable = queryable.TryWhere($"{filter.Key} {range.Key} @0", parsedDateTimeValue);
                            }
                            else
                            {
                                queryable = queryable.TryWhere($"{filter.Key} {range.Key} @0", range.Value);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"Filter range operator '{op}' is currently not supported. Affected property: {filter.Key}.");
                        }
                    }
                }
                else if (filter.Value is IEnumerable enumerable)
                {
                    var enumerator = enumerable.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        var elementType = enumerator.Current.GetType();
                        var castList = enumerable.Cast(elementType);
#if NET45
                        const string prefix = "outerIt.";
#else
                        const string prefix = "";
#endif
                        queryable = queryable.TryWhere($"@0.Contains({prefix}{filter.Key})", new object[] { castList });
                    }
                    else
                    {
                        Trace.WriteLine($"Filter collection for key '{filter.Key}' is empty.", "Warning");
                    }
                }
                else
                {
                    throw new NotSupportedException($"Filter values of type '{filter.Value.GetType().Name}' are currently not supported. Affected property: {filter.Key}.");
                }
            }

            return queryable;
        }

        private static bool IsOperator(this char op)
        {
            return (op == '>' || op == '<' || op == '=');
        }

        private static bool IsNumericType(this object value)
        {
            return
                value is short ||
                value is ushort ||
                value is int ||
                value is uint ||
                value is long ||
                value is ulong ||
                value is float ||
                value is double ||
                value is decimal;
        }

        /// <summary>
        /// TryWhere is a Where filter which catches <see cref="ParseException"/>.
        /// We use it in situations where we want to omit wrong filter values, e.g. invalid comparisons like DateTime > new object()
        /// </summary>
        internal static IQueryable<TSource> TryWhere<TSource>(this IQueryable<TSource> source, string predicate, params object[] args)
        {
            try
            {
                Trace.WriteLine($"Paging.TryWhere with Predicate: \"{predicate}\". Args.Count: {args.Length}. {(args.Length > 0 ? $"Args: {string.Join(", ", args)}." : "")}");
                return source.Where(predicate, args);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.TryWhere with Predicate: \"{predicate}\" failed. {ex.Message} {Environment.NewLine}" +
                                $"{ex.StackTrace}", ex.GetType().Name);
            }

            return source;
        }

        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> queryable, IReadOnlyDictionary<string, SortOrder> sorting, bool reverse)
        {
            if (reverse)
            {
                sorting = sorting
                    .Select(s => new { s.Key, Value = s.Value == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc })
                    .ToDictionary(s => s.Key, s => s.Value);
            }

            var sortBy = sorting.ToSortByString();
            Trace.WriteLine($"Paging.SortBy \"{sortBy}\"{(reverse ? " (Reversed)" : "")}");

            try
            {
                queryable = queryable.OrderBy(sortBy);
            }
            catch (Exception ex)
            {
                queryable = queryable.OrderByDefault();

                Trace.WriteLine($"Paging.SortBy \"{sortBy}\"{(reverse ? " (Reversed)" : "")} failed. {ex.Message} {Environment.NewLine}" +
                                $"{ex.StackTrace}", ex.GetType().Name);
            }

            return queryable;
        }

        // Need to OrderBy before Skip/Take.
        public static IQueryable<TEntity> OrderByDefault<TEntity>(this IQueryable<TEntity> queryable)
        {
            //try
            //{
            //    queryable = queryable.OrderBy("Id");
            //}
            //catch (Exception ex)
            //{
            //    Trace.WriteLine($"Paging.OrderByDefault (using Id) failed. {ex.Message} {Environment.NewLine}" +
            //                    $"{ex.StackTrace}", ex.GetType().Name);
            //}

            try
            {
                Trace.WriteLine("Paging.OrderByDefault");
                return queryable.OrderBy("x => false");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.OrderByDefault (using \"x => false\") failed. {ex.Message} {Environment.NewLine}" +
                                $"{ex.StackTrace}", ex.GetType().Name);
            }

            return queryable;
        }
    }
}
