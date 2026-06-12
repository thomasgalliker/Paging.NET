using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Applies filter values from <see cref="PagingInfo.Filter"/> to a query using typed expression trees.
    /// Value semantics:
    /// <list type="bullet">
    /// <item><description>String without leading operator: case-insensitive 'contains' search.</description></item>
    /// <item><description>String with leading operator (e.g. <c>"&gt;=5000"</c>): comparison.</description></item>
    /// <item><description>Numeric, bool or DateTime value: equality.</description></item>
    /// <item><description>Dictionary of operator/value pairs (e.g. <c>{"&gt;": "2012-01-01"}</c>): range comparisons.</description></item>
    /// <item><description>Collection of values: IN-filter (any value matches).</description></item>
    /// </list>
    /// Invalid filter values are leniently skipped; a warning is traced.
    /// </summary>
    internal static class FilterExpressionBuilder
    {
        internal static IQueryable<TEntity> ApplyFilter<TEntity>(IQueryable<TEntity> queryable, string propertyPath, object? filterValue)
        {
            if (filterValue == null)
            {
                // Ignore null values
                return queryable;
            }

            var propertyLambda = SortExpressionBuilder.CreatePropertyPathLambda<TEntity>(propertyPath);
            if (propertyLambda == null)
            {
                Trace.WriteLine($"Paging.ApplyFilter: Property path '{propertyPath}' cannot be resolved on type '{typeof(TEntity).Name}'.");
                return queryable;
            }

            if (filterValue is string stringValue)
            {
                if (string.IsNullOrEmpty(stringValue))
                {
                    // Ignore null/empty values
                    Trace.WriteLine($"Paging.ApplyFilter: Filter value for property '{propertyPath}' is null or empty.");
                    return queryable;
                }

                if (stringValue[0].IsOperator())
                {
                    // If the given string value contains a comparison operator,
                    // we apply it together with the rest of the expression
                    var (operatorToken, remainder) = SplitOperator(stringValue);
                    if (!TryMapOperator(operatorToken, out var comparisonType))
                    {
                        Trace.WriteLine($"Paging.ApplyFilter: Comparison operator '{operatorToken}' for property '{propertyPath}' is not supported.");
                        return queryable;
                    }

                    return TryApplyComparison(queryable, propertyLambda, propertyPath, comparisonType, remainder);
                }

                // No comparison operator means: property contains value
                return TryApplyContains(queryable, propertyLambda, propertyPath, stringValue);
            }

            if (filterValue.IsNumericType() || filterValue is bool || filterValue is DateTime)
            {
                return TryApplyComparison(queryable, propertyLambda, propertyPath, ExpressionType.Equal, filterValue);
            }

            if (filterValue is IDictionary<string, object?> ranges)
            {
                foreach (var range in ranges)
                {
                    if (string.IsNullOrEmpty(range.Key))
                    {
                        // Ignore null/empty operator
                        Trace.WriteLine($"Paging.ApplyFilter: Filter range operator for property '{propertyPath}' is null or empty.");
                        continue;
                    }

                    if (!range.Key[0].IsOperator())
                    {
                        throw new NotSupportedException($"Filter range operator '{range.Key[0]}' is currently not supported. Affected property: {propertyPath}.");
                    }

                    if (!TryMapOperator(range.Key.Trim(), out var comparisonType))
                    {
                        Trace.WriteLine($"Paging.ApplyFilter: Filter range operator '{range.Key}' for property '{propertyPath}' is not supported.");
                        continue;
                    }

                    queryable = TryApplyComparison(queryable, propertyLambda, propertyPath, comparisonType, range.Value);
                }

                return queryable;
            }

            if (filterValue is IEnumerable enumerable)
            {
                return TryApplyInFilter(queryable, propertyLambda, propertyPath, enumerable);
            }

            throw new NotSupportedException($"Filter values of type '{filterValue.GetType().Name}' are currently not supported. Affected property: {propertyPath}.");
        }

        private static IQueryable<TEntity> TryApplyComparison<TEntity>(
            IQueryable<TEntity> queryable,
            LambdaExpression propertyLambda,
            string propertyPath,
            ExpressionType comparisonType,
            object? filterValue)
        {
            try
            {
                if (filterValue == null)
                {
                    throw new InvalidOperationException("Comparison value must not be null.");
                }

                var propertyType = propertyLambda.ReturnType;
                var convertedValue = ConvertValue(filterValue, propertyType);
                var constant = Expression.Constant(convertedValue, propertyType);
                var body = Expression.MakeBinary(comparisonType, propertyLambda.Body, constant);
                var predicate = Expression.Lambda<Func<TEntity, bool>>(body, propertyLambda.Parameters[0]);

                Trace.WriteLine($"Paging.ApplyFilter: {propertyPath} {comparisonType} {convertedValue}");
                return queryable.Where(predicate);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.ApplyFilter: Comparison '{propertyPath} {comparisonType} {filterValue}' failed with exception: {ex}");
                return queryable;
            }
        }

        private static IQueryable<TEntity> TryApplyContains<TEntity>(
            IQueryable<TEntity> queryable,
            LambdaExpression propertyLambda,
            string propertyPath,
            string filterValue)
        {
            try
            {
                var searchValue = filterValue.Replace("\"", string.Empty).ToLowerInvariant();

                var body = propertyLambda.Body;
                if (body.Type != typeof(string))
                {
                    // Non-string properties are converted to their string representation
                    var toStringMethod = body.Type.GetMethod(nameof(ToString), Type.EmptyTypes)
                        ?? throw new InvalidOperationException($"Type '{body.Type.Name}' does not have a parameterless ToString method.");
                    body = Expression.Call(body, toStringMethod);
                }

                var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

                body = Expression.Call(body, toLowerMethod);
                body = Expression.Call(body, containsMethod, Expression.Constant(searchValue));
                var predicate = Expression.Lambda<Func<TEntity, bool>>(body, propertyLambda.Parameters[0]);

                Trace.WriteLine($"Paging.ApplyFilter: {propertyPath} contains \"{searchValue}\"");
                return queryable.Where(predicate);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.ApplyFilter: Contains filter '{propertyPath}' ~ '{filterValue}' failed with exception: {ex}");
                return queryable;
            }
        }

        private static IQueryable<TEntity> TryApplyInFilter<TEntity>(
            IQueryable<TEntity> queryable,
            LambdaExpression propertyLambda,
            string propertyPath,
            IEnumerable filterValues)
        {
            try
            {
                var enumerator = filterValues.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    Trace.WriteLine($"Paging.ApplyFilter: Filter collection for property '{propertyPath}' is empty.");
                    return queryable;
                }

                if (enumerator.Current == null)
                {
                    Trace.WriteLine($"Paging.ApplyFilter: Filter collection for property '{propertyPath}' starts with a null value.");
                    return queryable;
                }

                var elementType = enumerator.Current.GetType();
                var propertyType = propertyLambda.ReturnType;

                Type listElementType;
                if (elementType == propertyType || Nullable.GetUnderlyingType(propertyType) == elementType)
                {
                    // Boxed values of the underlying type cast cleanly to the nullable property type
                    listElementType = propertyType;
                }
                else
                {
                    Trace.WriteLine($"Paging.ApplyFilter: Filter collection of type '{elementType.Name}' " +
                                    $"cannot be applied to property '{propertyPath}' of type '{propertyType.Name}'.");
                    return queryable;
                }

                var castList = filterValues.Cast(listElementType);
                var constant = Expression.Constant(castList, typeof(IEnumerable<>).MakeGenericType(listElementType));
                var body = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { listElementType }, constant, propertyLambda.Body);
                var predicate = Expression.Lambda<Func<TEntity, bool>>(body, propertyLambda.Parameters[0]);

                Trace.WriteLine($"Paging.ApplyFilter: {propertyPath} in [collection of {elementType.Name}]");
                return queryable.Where(predicate);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.ApplyFilter: IN filter for property '{propertyPath}' failed with exception: {ex}");
                return queryable;
            }
        }

        private static (string OperatorToken, string Remainder) SplitOperator(string value)
        {
            if (value.Length >= 2 && (value[1] == '='))
            {
                return (value.Substring(0, 2), value.Substring(2));
            }

            return (value.Substring(0, 1), value.Substring(1));
        }

        private static bool TryMapOperator(string operatorToken, out ExpressionType comparisonType)
        {
            switch (operatorToken)
            {
                case ">":
                    comparisonType = ExpressionType.GreaterThan;
                    return true;
                case ">=":
                    comparisonType = ExpressionType.GreaterThanOrEqual;
                    return true;
                case "<":
                    comparisonType = ExpressionType.LessThan;
                    return true;
                case "<=":
                    comparisonType = ExpressionType.LessThanOrEqual;
                    return true;
                case "=":
                case "==":
                    comparisonType = ExpressionType.Equal;
                    return true;
                default:
                    comparisonType = default;
                    return false;
            }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType.IsInstanceOfType(value))
            {
                return value;
            }

            if (value is string stringValue)
            {
                stringValue = stringValue.Trim();

                if (underlyingType == typeof(DateTime))
                {
                    return DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                }

                if (underlyingType == typeof(DateTimeOffset))
                {
                    return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                }

                if (underlyingType.IsEnum)
                {
                    return Enum.Parse(underlyingType, stringValue, ignoreCase: true);
                }

                if (underlyingType == typeof(Guid))
                {
                    return Guid.Parse(stringValue);
                }

                return Convert.ChangeType(stringValue, underlyingType, CultureInfo.InvariantCulture);
            }

            if (underlyingType == typeof(DateTimeOffset) && value is DateTime dateTimeValue)
            {
                return new DateTimeOffset(dateTimeValue);
            }

            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }

        internal static bool IsOperator(this char op)
        {
            return op == '>' || op == '<' || op == '=';
        }

        internal static bool IsNumericType(this object value)
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
    }
}
