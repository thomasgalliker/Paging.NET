using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Builds a predicate expression for a single <see cref="FilterCondition"/> against a
    /// server-declared property path. The <see cref="FilterOperator"/> determines the comparison:
    /// <list type="bullet">
    /// <item><description>Equal/NotEqual: binary comparison (case-insensitive for strings).</description></item>
    /// <item><description>GreaterThan/GreaterThanOrEqual/LessThan/LessThanOrEqual: binary comparison (case-sensitive for strings).</description></item>
    /// <item><description>Contains/StartsWith/EndsWith (and their negated Not* forms): case-insensitive string match (non-string properties are converted via ToString).</description></item>
    /// <item><description>In/NotIn: (negated) IN-filter (the property matches any/none of the supplied collection; case-insensitive for strings).</description></item>
    /// </list>
    /// String comparisons are null-safe: a null property never matches a positive operator and always
    /// matches its negated form (rather than dereferencing <c>ToLower</c>).
    /// Values that cannot be converted to the property type are leniently skipped (returns <c>null</c>); a warning is traced.
    /// </summary>
    internal static class FilterExpressionBuilder
    {
        private static readonly MethodInfo StringToLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        private static readonly MethodInfo StringContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        private static readonly MethodInfo StringStartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
        private static readonly MethodInfo StringEndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
        private static readonly MethodInfo EnumerableContainsMethod = GetEnumerableMethod(nameof(Enumerable.Contains), parameterCount: 2);
        private static readonly MethodInfo EnumerableAnyMethod = GetEnumerableMethod(nameof(Enumerable.Any), parameterCount: 2);

        internal static Expression<Func<TEntity, bool>>? BuildPredicate<TEntity>(
            LambdaExpression propertyLambda,
            in FilterConditionContext context,
            FilterOperator filterOperator,
            object? filterValue)
        {
            var body = BuildBody(propertyLambda, context, filterOperator, filterValue);
            if (body == null)
            {
                return null;
            }

            return Expression.Lambda<Func<TEntity, bool>>(body, propertyLambda.Parameters[0]);
        }

        /// <summary>
        /// Builds a predicate that matches <typeparamref name="TEntity"/> instances whose declared
        /// collection contains at least one element satisfying the condition, i.e.
        /// <c>e =&gt; e.Collection.Any(x =&gt; &lt;leaf comparison&gt;)</c>. Query providers such as EF Core
        /// translate this to a SQL <c>EXISTS</c>.
        /// </summary>
        internal static Expression<Func<TEntity, bool>>? BuildCollectionAnyPredicate<TEntity>(
            LambdaExpression collectionSelector,
            LambdaExpression elementSelector,
            in FilterConditionContext context,
            FilterOperator filterOperator,
            object? filterValue)
        {
            var leafBody = BuildBody(elementSelector, context, filterOperator, filterValue);
            if (leafBody == null)
            {
                return null;
            }

            var elementType = elementSelector.Parameters[0].Type;
            var leafLambda = Expression.Lambda(leafBody, elementSelector.Parameters[0]);

            // Unwrap the implicit upcast the compiler inserts when a List<T>/ICollection<T> navigation
            // is assigned to Expression<Func<TEntity, IEnumerable<TElement>>>, so the query provider
            // sees a plain e.Collection.Any(...) call.
            var collectionBody = SortExpressionBuilder.UnwrapConverts(collectionSelector.Body);

            var anyCall = Expression.Call(EnumerableAnyMethod.MakeGenericMethod(elementType), collectionBody, leafLambda);

            Trace.WriteLine($"Paging.ApplyFilter: {context.PropertyPath} {filterOperator} (collection Any)");
            return Expression.Lambda<Func<TEntity, bool>>(anyCall, collectionSelector.Parameters[0]);
        }

        /// <summary>
        /// Builds the boolean body expression for a condition against the given property/element
        /// selector lambda. The body is expressed over <c>propertyLambda.Parameters[0]</c> so it can
        /// be wrapped into a predicate over an entity or reused as a collection element predicate.
        /// </summary>
        internal static Expression? BuildBody(
            LambdaExpression propertyLambda,
            in FilterConditionContext context,
            FilterOperator filterOperator,
            object? filterValue)
        {
            switch (filterOperator)
            {
                case FilterOperator.Contains:
                case FilterOperator.StartsWith:
                case FilterOperator.EndsWith:
                    return BuildStringBody(propertyLambda, context, filterOperator, filterValue, negate: false);

                case FilterOperator.NotContains:
                case FilterOperator.NotStartsWith:
                case FilterOperator.NotEndsWith:
                    return BuildStringBody(propertyLambda, context, filterOperator, filterValue, negate: true);

                case FilterOperator.In:
                    return BuildInBody(propertyLambda, context, filterValue, negate: false);

                case FilterOperator.NotIn:
                    return BuildInBody(propertyLambda, context, filterValue, negate: true);

                default:
                    return BuildComparisonBody(propertyLambda, context, filterOperator, filterValue);
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
                _ => throw new ArgumentOutOfRangeException(nameof(filterOperator), filterOperator, "Operator is not a binary comparison."),
            };
        }

        private static Expression? BuildComparisonBody(
            LambdaExpression propertyLambda,
            in FilterConditionContext context,
            FilterOperator filterOperator,
            object? filterValue)
        {
            var comparisonType = MapComparison(filterOperator);
            try
            {
                if (filterValue == null)
                {
                    var nullConstant = Expression.Constant(null, propertyLambda.ReturnType);
                    return Expression.MakeBinary(comparisonType, propertyLambda.Body, nullConstant);
                }

                var propertyType = propertyLambda.ReturnType;
                var convertedValue = ConvertValue(filterValue, propertyType);

                // Case-insensitive, null-safe string (in)equality. Both sides are lowered with ToLower
                // (not ToLowerInvariant) so query providers such as EF Core translate it to LOWER(...);
                // the column is null-guarded so a null value is never dereferenced by ToLower in memory
                // and yields consistent set semantics across providers (== excludes null, != includes it).
                if (propertyType == typeof(string) &&
                    (comparisonType == ExpressionType.Equal || comparisonType == ExpressionType.NotEqual))
                {
                    var column = propertyLambda.Body;
                    var nullConstant = Expression.Constant(null, typeof(string));
                    var loweredColumn = Expression.Call(column, StringToLowerMethod);
                    var loweredConstant = Expression.Constant(((string)convertedValue).ToLower(), typeof(string));

                    var caseInsensitiveBody = comparisonType == ExpressionType.Equal
                        ? Expression.AndAlso(Expression.NotEqual(column, nullConstant), Expression.Equal(loweredColumn, loweredConstant))
                        : (Expression)Expression.OrElse(Expression.Equal(column, nullConstant), Expression.NotEqual(loweredColumn, loweredConstant));

                    Trace.WriteLine($"Paging.ApplyFilter: {context.PropertyPath} {comparisonType} {convertedValue} (case-insensitive)");
                    return caseInsensitiveBody;
                }

                var constant = Expression.Constant(convertedValue, propertyType);
                var body = Expression.MakeBinary(comparisonType, propertyLambda.Body, constant);

                Trace.WriteLine($"Paging.ApplyFilter: {context.PropertyPath} {comparisonType} {convertedValue}");
                return body;
            }
            catch (Exception ex)
            {
                return context.Invalid($"Comparison '{context.PropertyPath} {comparisonType} {filterValue}' failed: {ex.Message}", ex);
            }
        }

        private static Expression? BuildStringBody(
            LambdaExpression propertyLambda,
            in FilterConditionContext context,
            FilterOperator filterOperator,
            object? filterValue,
            bool negate)
        {
            try
            {
                if (filterValue == null)
                {
                    return context.Invalid($"String filter value for property '{context.PropertyPath}' is null.");
                }

                // ToLower (not ToLowerInvariant) so the constant is lowered with the same casing rules as
                // the column (which query providers lower via ToLower/collation), avoiding culture mismatches.
                var searchValue = Convert.ToString(filterValue, CultureInfo.InvariantCulture)?
                    .Replace("\"", string.Empty)
                    .ToLower();

                if (string.IsNullOrEmpty(searchValue))
                {
                    // A positive match against an empty needle matches everything (no constraint -> skip);
                    // its negation matches nothing. Well-defined semantics -> never an invalid value.
                    Trace.WriteLine($"Paging.ApplyFilter: String filter value for property '{context.PropertyPath}' is empty.");
                    return negate ? Expression.Constant(false) : null;
                }

                var column = propertyLambda.Body;
                if (column.Type != typeof(string))
                {
                    var toStringMethod = column.Type.GetMethod(nameof(ToString), Type.EmptyTypes)
                        ?? throw new InvalidOperationException($"Type '{column.Type.Name}' does not have a parameterless ToString method.");
                    column = Expression.Call(column, toStringMethod);
                }

                var stringMethod = filterOperator switch
                {
                    FilterOperator.StartsWith or FilterOperator.NotStartsWith => StringStartsWithMethod,
                    FilterOperator.EndsWith or FilterOperator.NotEndsWith => StringEndsWithMethod,
                    _ => StringContainsMethod,
                };

                var match = Expression.Call(Expression.Call(column, StringToLowerMethod), stringMethod, Expression.Constant(searchValue));
                var body = negate ? (Expression)Expression.Not(match) : match;

                // Null-safe: a null (reference-typed) property never satisfies a positive match and always
                // satisfies its negation, instead of dereferencing ToLower/ToString on null in memory.
                if (!propertyLambda.ReturnType.IsValueType)
                {
                    var nullConstant = Expression.Constant(null, propertyLambda.ReturnType);
                    body = negate
                        ? Expression.OrElse(Expression.Equal(propertyLambda.Body, nullConstant), body)
                        : Expression.AndAlso(Expression.NotEqual(propertyLambda.Body, nullConstant), body);
                }

                Trace.WriteLine($"Paging.ApplyFilter: {context.PropertyPath} {filterOperator} \"{searchValue}\"");
                return body;
            }
            catch (Exception ex)
            {
                return context.Invalid($"{filterOperator} filter '{context.PropertyPath}' ~ '{filterValue}' failed: {ex.Message}", ex);
            }
        }

        private static Expression? BuildInBody(
            LambdaExpression propertyLambda,
            in FilterConditionContext context,
            object? filterValue,
            bool negate)
        {
            try
            {
                if (filterValue is not IEnumerable enumerable || filterValue is string)
                {
                    return context.Invalid($"IN filter for property '{context.PropertyPath}' requires a collection of values.");
                }

                var propertyType = propertyLambda.ReturnType;
                var isString = propertyType == typeof(string);

                var listType = typeof(List<>).MakeGenericType(propertyType);
                var typedList = (IList)Activator.CreateInstance(listType)!;

                foreach (var item in enumerable)
                {
                    if (item == null)
                    {
                        // Null list elements are ignored: a null column can never equal a non-nullable
                        // value type (would throw on List<T>.Add), and for nullable/reference types a null
                        // column is matched by the null-guard below rather than by a NULL in the IN set
                        // (which trips SQL three-valued logic on NOT IN).
                        continue;
                    }

                    var converted = ConvertValue(item, propertyType);
                    if (isString)
                    {
                        converted = ((string)converted).ToLower();
                    }

                    typedList.Add(converted);
                }

                if (typedList.Count == 0)
                {
                    // Well-defined set semantics -> never an invalid value (see A6/empty-list handling).
                    Trace.WriteLine($"Paging.ApplyFilter: IN filter collection for property '{context.PropertyPath}' is empty.");
                    return null;
                }

                var constant = Expression.Constant(typedList, typeof(IEnumerable<>).MakeGenericType(propertyType));

                var column = propertyLambda.Body;
                var member = isString ? (Expression)Expression.Call(column, StringToLowerMethod) : column;

                var contains = Expression.Call(EnumerableContainsMethod.MakeGenericMethod(propertyType), constant, member);
                var body = negate ? (Expression)Expression.Not(contains) : contains;

                // Null-safe: a null column is never IN the set and is always NOT IN it.
                if (!propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null)
                {
                    var nullConstant = Expression.Constant(null, propertyType);
                    body = negate
                        ? Expression.OrElse(Expression.Equal(column, nullConstant), body)
                        : Expression.AndAlso(Expression.NotEqual(column, nullConstant), body);
                }

                Trace.WriteLine($"Paging.ApplyFilter: {context.PropertyPath} {(negate ? "!in" : "in")} [{typedList.Count} values]");
                return body;
            }
            catch (Exception ex)
            {
                return context.Invalid($"IN filter for property '{context.PropertyPath}' failed: {ex.Message}", ex);
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

                if (underlyingType == typeof(TimeSpan))
                {
                    // TimeSpan is not IConvertible; Convert.ChangeType cannot produce it.
                    return TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture);
                }

#if NET8_0_OR_GREATER
                if (underlyingType == typeof(DateOnly))
                {
                    return DateOnly.Parse(stringValue, CultureInfo.InvariantCulture);
                }

                if (underlyingType == typeof(TimeOnly))
                {
                    return TimeOnly.Parse(stringValue, CultureInfo.InvariantCulture);
                }
#else
                // netstandard builds cannot reference DateOnly/TimeOnly (net6+ types), but consumers
                // on net6/net7 resolve the netstandard binary while the types exist at runtime.
                if (underlyingType.FullName == "System.DateOnly" || underlyingType.FullName == "System.TimeOnly")
                {
                    return ParseViaReflection(underlyingType, stringValue);
                }
#endif

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

        private static MethodInfo GetEnumerableMethod(string name, int parameterCount)
        {
            return typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == name && m.IsGenericMethodDefinition && m.GetParameters().Length == parameterCount);
        }

#if !NET8_0_OR_GREATER
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, MethodInfo> ParseMethods =
            new System.Collections.Concurrent.ConcurrentDictionary<Type, MethodInfo>();

        private static object ParseViaReflection(Type type, string value)
        {
            var parseMethod = ParseMethods.GetOrAdd(
                type,
                t => t.GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) })
                     ?? throw new InvalidOperationException($"Type '{t.FullName}' does not have a Parse(string, IFormatProvider) method."));

            return parseMethod.Invoke(null, new object[] { value, CultureInfo.InvariantCulture })!;
        }
#endif
    }
}
