using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Fulcrum.View;

/// <summary>
/// Converte valores nulos para visibilidade (Visible se não for nulo, Collapsed se for nulo)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converte um DateTime para um formato legível
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.ToString("dd/MM/yyyy HH:mm");
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converte um valor de volume (0.0 a 1.0) para um percentual formatado
/// </summary>
public class VolumePercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float floatValue)
        {
            return $"{(int)(floatValue * 100)}%";
        }
        
        if (value is double doubleValue)
        {
            return $"{(int)(doubleValue * 100)}%";
        }
        
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}