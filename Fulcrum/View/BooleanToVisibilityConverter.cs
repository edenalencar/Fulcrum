using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Fulcrum.View;

/// <summary>
/// Converte um valor booleano para visibilidade (Visible se verdadeiro, Collapsed se falso)
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Inverte o resultado (Collapsed se verdadeiro, Visible se falso)
    /// </summary>
    public bool Invert { get; set; } = false;
    
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool bValue = false;
        
        if (value is bool b)
        {
            bValue = b;
        }
        else if (value != null)
        {
            bValue = System.Convert.ToBoolean(value);
        }
        
        return (bValue != Invert) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return (visibility == Visibility.Visible) != Invert;
        }
        
        return !Invert;
    }
}