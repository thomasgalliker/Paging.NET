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
                case IEnumerable enumerable:
                    var items = new List<string>();
                    foreach (var item in enumerable)
                    {
                        items.Add(FormatValue(item));
                    }
                    return "[" + string.Join(", ", items) + "]";
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
