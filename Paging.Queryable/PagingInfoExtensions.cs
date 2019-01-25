using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using System.Linq.Expressions;
#if NETSTANDARD1_3
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
#else
using System.Linq.Dynamic;
#endif

namespace Paging.Queryable
{
    public static class PagingInfoExtensions
    {
        public static PaginationSet<TEntity> CreatePaginationSet<TEntity>(this PagingInfo pagingInfo, IQueryable<TEntity> queryable, Expression<Func<TEntity, bool>> searchPredicate = null)
        {
            return pagingInfo.CreatePaginationSet(queryable, entities => entities, searchPredicate);
        }

        public static PaginationSet<TDto> CreatePaginationSet<TEntity, TDto>(this PagingInfo pagingInfo, IQueryable<TEntity> queryable, Func<IEnumerable<TEntity>, IEnumerable<TDto>> mapEntitiesToDtos, Expression<Func<TEntity, bool>> searchPredicate = null)
        {
            if (pagingInfo == null)
            {
                var dtos = mapEntitiesToDtos(queryable);
                var paginationSet = new PaginationSet<TDto>(dtos);
                return paginationSet;
            }
            else
            {
                var totalCountUnfiltered = queryable.Count();

                // Free-text search using custom search predicate
                if (pagingInfo.Search != null && searchPredicate != null)
                {
                    queryable = queryable.Where(searchPredicate);
                }

                // Property-based filter
                if (pagingInfo.Filter != null)
                {
                    foreach (var filter in pagingInfo.Filter)
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
                                continue;
                            }

                            var op = stringValue[0];
                            if (op == '>' || op == '<' || op == '=')
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
                                    continue;
                                }

                                var op = range.Key[0];
                                if (op == '>' || op == '<' || op == '=')
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
                        else
                        {
                            throw new NotSupportedException($"Filter values of type '{filter.Value.GetType().Name}' is currently not supported. Affected property: {filter.Key}.");
                        }
                    }
                }

                var totalCount = queryable.Count();

                // Order
                if (!string.IsNullOrEmpty(pagingInfo.SortBy))
                {
                    var sortBy = pagingInfo.SortBy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sortBy.Length == 1)
                    {
                        queryable = queryable.OrderBy(sortBy[0] + (pagingInfo.Reverse ? " descending" : ""));
                    }
                    if (sortBy.Length > 1)
                    {
                        queryable = queryable.OrderBy(pagingInfo.SortBy);
                    }
                }
                else
                {
                    // Need to OrderBy before Skip/Take. Maybe we should throw an exception here?
                    queryable = queryable.OrderBy("Id");
                }

                // Page
                if (pagingInfo.ItemsPerPage > 0)
                {
                    queryable = queryable.Skip((pagingInfo.CurrentPage - 1) * pagingInfo.ItemsPerPage).Take(pagingInfo.ItemsPerPage);
                }

                var dtos = mapEntitiesToDtos(queryable.ToList());
                var paginationSet = new PaginationSet<TDto>(pagingInfo, dtos, totalCount, totalCountUnfiltered);
                return paginationSet;
            }
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
        private static IQueryable<TSource> TryWhere<TSource>(this IQueryable<TSource> source, string predicate, params object[] args)
        {
            try
            {
                Trace.WriteLine($"Paging.TryWhere. Predicate: \"{predicate}\". Args.Count: {args.Length}. {(args.Length > 0 ? $"Args: {string.Join(", ", args)}." : "")}");
                return source.Where(predicate, args);
            }
            catch (ParseException parseException)
            {
                Trace.WriteLine($"{parseException.Message}. Predicate: \"{predicate}\". Args.Count: {args.Length}. {(args.Length > 0 ? $"Args: {string.Join(", ", args)}." : "")}{Environment.NewLine}{parseException.StackTrace}", "ParseException");
            }

            return source;
        }
    }
}
