using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Builds a predicate expression for a single <see cref="FilterCondition"/> against a
    /// server-declared property path. The <see cref="FilterOperator"/> determines the comparison:
    /// <list type="bullet">
    /// <item><description>Equal/NotEqual/GreaterThan/GreaterThanOrEqual/LessThan/LessThanOrEqual: binary comparison.</description></item>
    /// <item><description>Contains/StartsWith/EndsWith: case-insensitive string match (non-string properties are converted via ToString).</description></item>
    /// <item><description>In: IN-filter (the property matches any value of the supplied collection).</description></item>
    /// </list>
    /// Values that cannot be converted to the property type are leniently skipped (returns <c>null</c>); a warning is traced.
    /// </summary>
    internal static class FilterExpressionBuilder
    {
        internal static Expression<Func<TEntity, bool>>? BuildPredicate<TEntity>(
            LambdaExpression propertyLambda,
            string propertyPath,
            FilterOperator filterOperator,
            object? filterValue)
        {
            switch (filterOperator)
            {
                case FilterOperator.Contains:
                case FilterOperator.StartsWith:
                case FilterOperator.EndsWith:
                    return BuildStringPredicate<TEntity>(propertyLambda, propertyPath, filterOperator, filterValue);

                case FilterOperator.In:
                    return BuildInPredicate<TEntity>(propertyLambda, propertyPath, filterValue);

                default:
                    return BuildComparisonPredicate<TEntity>(propertyLambda, propertyPath, MapComparison(filterOperator), filterValue);
            }
        }

        private static ExpressionType MapComparison(FilterOperator filterOperator)
        {
            return filterOperator switch
            {
                FilterOperator.Equal => ExpressionType.Equal,
                FilterOperator.NotEqual => ExpressionType.NotEqual,
                FilterOperator.GreaterThan => ExpressionType.GreaterThan,
                FilterOperator.GreaterThanOrEqual => ExpressionType.GreaterThanOrEqual,
                FilterOperator.LessThan => ExpressionType.LessThan,
                FilterOperator.LessThanOrEqual => ExpressionType.LessThanOrEqual,
                _ => ExpressionType.Equal,
            };
        }

        private static Expression<Func<TEntity, bool>>? BuildComparisonPredicate<TEntity>(
            LambdaExpression propertyLambda,
            string propertyPath,
            ExpressionType comparisonType,
            object? filterValue)
        {
            try
            {
                if (filterValue == null)
                {
                    var nullConstant = Expression.Constant(null, propertyLambda.ReturnType);
                    var nullBody = Expression.MakeBinary(comparisonType, propertyLambda.Body, nullConstant);
                    return Expression.Lambda<Func<TEntity, bool>>(nullBody, propertyLambda.Parameters[0]);
                }

                var propertyType = propertyLambda.ReturnType;
                var convertedValue = ConvertValue(filterValue, propertyType);
                var constant = Expression.Constant(convertedValue, propertyType);
                var body = Expression.MakeBinary(comparisonType, propertyLambda.Body, constant);

                Trace.WriteLine($"Paging.ApplyFilter: {propertyPath} {comparisonType} {convertedValue}");
                return Expression.Lambda<Func<TEntity, bool>>(body, propertyLambda.Parameters[0]);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.ApplyFilter: Comparison '{propertyPath} {comparisonType} {filterValue}' failed with exception: {ex}");
                return null;
            }
        }

        private static Expression<Func<TEntity, bool>>? BuildStringPredicate<TEntity>(
            LambdaExpression propertyLambda,
            string propertyPath,
            FilterOperator filterOperator,
            object? filterValue)
        {
            try
            {
                if (filterValue == null)
                {
                    Trace.WriteLine($"Paging.ApplyFilter: String filter value for property '{propertyPath}' is null.");
                    return null;
                }

                var searchValue = Convert.ToString(filterValue, CultureInfo.InvariantCulture)?
                    .Replace("\"", string.Empty)
                    .ToLowerInvariant();

                if (string.IsNullOrEmpty(searchValue))
                {
                    Trace.WriteLine($"Paging.ApplyFilter: String filter value for property '{propertyPath}' is empty.");
                    return null;
                }

                var body = propertyLambda.Body;
                if (body.Type != typeof(string))
                {
                    var toStringMethod = body.Type.GetMethod(nameof(ToString), Type.EmptyTypes)
                        ?? throw new InvalidOperationException($"Type '{body.Type.Name}' does not have a parameterless ToString method.");
                    body = Expression.Call(body, toStringMethod);
                }

                var methodName = filterOperator switch
                {
                    FilterOperator.StartsWith => nameof(string.StartsWith),
                    FilterOperator.EndsWith => nameof(string.EndsWith),
                    _ => nameof(string.Contains),
                };

                var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
                var stringMethod = typeof(string).GetMethod(methodName, new[] { typeof(string) })!;

                body = Expression.Call(body, toLowerMethod);
                body = Expression.Call(body, stringMethod, Expression.Constant(searchValue));

                Trace.WriteLine($"Paging.ApplyFilter: {propertyPath} {filterOperator} \"{searchValue}\"");
                return Expression.Lambda<Func<TEntity, bool>>(body, propertyLambda.Parameters[0]);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.ApplyFilter: {filterOperator} filter '{propertyPath}' ~ '{filterValue}' failed with exception: {ex}");
                return null;
            }
        }

        private static Expression<Func<TEntity, bool>>? BuildInPredicate<TEntity>(
            LambdaExpression propertyLambda,
            string propertyPath,
            object? filterValue)
        {
            try
            {
                if (filterValue is not IEnumerable enumerable || filterValue is string)
                {
                    Trace.WriteLine($"Paging.ApplyFilter: IN filter for property '{propertyPath}' requires a collection of values.");
                    return null;
                }

                var propertyType = propertyLambda.ReturnType;
                var listType = typeof(List<>).MakeGenericType(propertyType);
                var typedList = (IList)Activator.CreateInstance(listType)!;

                foreach (var item in enumerable)
                {
                    typedList.Add(item == null ? null : ConvertValue(item, propertyType));
                }

                if (typedList.Count == 0)
                {
                    Trace.WriteLine($"Paging.ApplyFilter: IN filter collection for property '{propertyPath}' is empty.");
                    return null;
                }

                var constant = Expression.Constant(typedList, typeof(IEnumerable<>).MakeGenericType(propertyType));
                var body = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { propertyType }, constant, propertyLambda.Body);

                Trace.WriteLine($"Paging.ApplyFilter: {propertyPath} in [{typedList.Count} values]");
                return Expression.Lambda<Func<TEntity, bool>>(body, propertyLambda.Parameters[0]);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Paging.ApplyFilter: IN filter for property '{propertyPath}' failed with exception: {ex}");
                return null;
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

            if (underlyingType.IsEnum)
            {
                return Enum.ToObject(underlyingType, value);
            }

            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
    }
}
