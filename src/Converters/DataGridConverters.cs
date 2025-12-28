using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace Julien.Avalonia.DataGrid.Converters;

/// <summary>
/// Converts TextAlignment to HorizontalAlignment.
/// </summary>
public class TextAlignmentToHorizontalAlignmentConverter : IValueConverter
{
    public static readonly TextAlignmentToHorizontalAlignmentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TextAlignment textAlignment)
        {
            return textAlignment switch
            {
                TextAlignment.Left => HorizontalAlignment.Left,
                TextAlignment.Center => HorizontalAlignment.Center,
                TextAlignment.Right => HorizontalAlignment.Right,
                TextAlignment.Justify => HorizontalAlignment.Stretch,
                _ => HorizontalAlignment.Left
            };
        }
        return HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HorizontalAlignment horizontalAlignment)
        {
            return horizontalAlignment switch
            {
                HorizontalAlignment.Left => TextAlignment.Left,
                HorizontalAlignment.Center => TextAlignment.Center,
                HorizontalAlignment.Right => TextAlignment.Right,
                HorizontalAlignment.Stretch => TextAlignment.Justify,
                _ => TextAlignment.Left
            };
        }
        return TextAlignment.Left;
    }
}

/// <summary>
/// Converts a boolean to a visibility (returns null for false, the value for true).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true;
    }
}

/// <summary>
/// Multiplies a value by a factor (useful for indent calculations).
/// </summary>
public class MultiplyConverter : IValueConverter
{
    public static readonly MultiplyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is double factor)
        {
            return doubleValue * factor;
        }

        if (value is int intValue && double.TryParse(parameter?.ToString(), out var factorFromString))
        {
            return intValue * factorFromString;
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Formats a value using a format string.
/// </summary>
public class FormatStringConverter : IValueConverter
{
    public static readonly FormatStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        var format = parameter?.ToString();
        if (string.IsNullOrEmpty(format))
            return value.ToString();

        if (value is IFormattable formattable)
        {
            return formattable.ToString(format, culture);
        }

        return string.Format(culture, $"{{0:{format}}}", value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Returns the opposite boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }
}

/// <summary>
/// Checks if a collection has items.
/// </summary>
public class HasItemsConverter : IValueConverter
{
    public static readonly HasItemsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
            return count > 0;

        if (value is System.Collections.ICollection collection)
            return collection.Count > 0;

        if (value is System.Collections.IEnumerable enumerable)
            return enumerable.Cast<object>().Any();

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
