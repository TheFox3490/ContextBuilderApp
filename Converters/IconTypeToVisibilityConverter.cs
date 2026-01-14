using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ContextBuilderApp.Views; // Чтобы видеть Enum, который мы создадим ниже

namespace ContextBuilderApp.Converters;

// Этот конвертер сравнивает значение свойства с параметром.
// Если IconType == Parameter, возвращает True (Visible).
public class IconTypeToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FilterIconType iconType && parameter is FilterIconType targetIconType)
        {
            return iconType == targetIconType;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}