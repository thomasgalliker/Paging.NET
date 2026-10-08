using System.Collections;
using System.Globalization;
using System.Text;

namespace Paging
{
    /// <summary>
    /// Serializes a <see cref="FilterNode"/> tree into its canonical filter expression string,
    /// e.g. <c>(Brand contains "bmw" &amp;&amp; Year &gt;= 2020) || IsElectric == true</c>.
    /// Parentheses are added only where required by operator precedence (<c>||</c> binds looser than <c>&amp;&amp;</c>).
    /// </summary>
    internal static class FilterExpressionWriter
    {
        private const int OrPrecedence = 1;
        private const int AndPrecedence = 2;

        internal static string Write(FilterNode node)
        {
            var builder = new StringBuilder();
            WriteNode(builder, node, parentPrecedence: 0);
            return builder.ToString();
        }

        private static void WriteNode(StringBuilder builder, FilterNode node, int parentPrecedence)
        {
            switch (node)
            {
                case FilterGroup group:
                    WriteGroup(builder, group, parentPrecedence);
                    break;

                case FilterCondition condition:
                    builder.Append(condition.Property)
                        .Append(' ')
                        .Append(FilterOperatorTokens.ToToken(condition.Operator))
                        .Append(' ')
                        .Append(FormatValue(condition.Value));
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported filter node type '{node.GetType().Name}'.");
            }
        }

        private static void WriteGroup(StringBuilder builder, FilterGroup group, int parentPrecedence)
        {
            var precedence = group.Logic == FilterLogic.Or ? OrPrecedence : AndPrecedence;
            var wrap = precedence < parentPrecedence;

            if (wrap)
            {
                builder.Append('(');
            }

            var separator = group.Logic == FilterLogic.Or ? " || " : " && ";
            for (var i = 0; i < group.Nodes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(separator);
                }

                WriteNode(builder, group.Nodes[i], precedence);
            }

            if (wrap)
            {
                builder.Append(')');
            }
        }

        private static string FormatValue(object? value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case bool boolValue:
                    return boolValue ? "true" : "false";
                case string stringValue:
                    return Quote(stringValue);
                case DateTime dateTime:
                    return Quote(dateTime.ToString("o", CultureInfo.InvariantCulture));
                case DateTimeOffset dateTimeOffset:
                    return Quote(dateTimeOffset.ToString("o", CultureInfo.InvariantCulture));
                case Guid guid:
                    return Quote(guid.ToString());
                case TimeSpan timeSpan:
                    return Quote(timeSpan.ToString("c", CultureInfo.InvariantCulture));
                case char charValue:
                    return Quote(charValue.ToString());
                case Enum enumValue:
                    // Member name(s), parsed back case-insensitively via Enum.Parse (incl. flags "A, B").
                    return Quote(enumValue.ToString());
#if NET8_0_OR_GREATER
                case DateOnly dateOnly:
                    return Quote(dateOnly.ToString("O", CultureInfo.InvariantCulture));
                case TimeOnly timeOnly:
                    return Quote(timeOnly.ToString("O", CultureInfo.InvariantCulture));
#endif
                case IEnumerable enumerable:
                    var items = new List<string>();
                    foreach (var item in enumerable)
                    {
                        items.Add(FormatValue(item));
                    }
                    return "[" + string.Join(", ", items) + "]";
                default:
                    // netstandard builds cannot reference DateOnly/TimeOnly (net6+ types), but consumers
                    // on net6/net7 resolve the netstandard binary while the types exist at runtime.
                    var typeName = value.GetType().FullName;
                    if (typeName == "System.DateOnly" || typeName == "System.TimeOnly")
                    {
                        return Quote(((IFormattable)value).ToString("O", CultureInfo.InvariantCulture));
                    }

                    // Numbers (incl. decimal/float) are emitted as unquoted invariant literals; the parser
                    // reads them back as long/double and ConvertValue restores the property's CLR type
                    // (precision beyond double's ~15-17 significant digits is not preserved).
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
