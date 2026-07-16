using System.ComponentModel;
using System.Globalization;

namespace Paging
{
    /// <summary>
    /// Converts between <see cref="FilterNode"/> and its filter expression string form
    /// (e.g. <c>Year &gt;= 2020 &amp;&amp; Name contains "bmw"</c>).
    /// This enables query-string model binding of <see cref="PagingInfo.Filter"/> in web frameworks
    /// that honor <see cref="TypeConverter"/> (e.g. ASP.NET Core <c>[FromQuery]</c>):
    /// <c>?filter=Year%20%3E%3D%202020</c> binds to a parsed <see cref="FilterNode"/> tree.
    /// A syntactically invalid expression throws <see cref="FormatException"/>, which model binding
    /// surfaces as a validation error (HTTP 400) on the <c>filter</c> key.
    /// </summary>
    public class FilterNodeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string expression)
            {
                // Null for empty/whitespace input; FormatException propagates on syntax errors.
                return FilterNode.Parse(expression);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is FilterNode filterNode)
            {
                return filterNode.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
