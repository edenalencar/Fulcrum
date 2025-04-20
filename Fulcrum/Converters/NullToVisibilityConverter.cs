using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Fulcrum.Converters;

/// <summary>
/// Converte um valor nulo para visibilidade (visível se não for nulo, colapsado se for nulo)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converte valores nulos para Visibility
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool invertido = parameter != null && parameter.ToString() == "Inverted";
        bool isNull = value == null;
        
        if (invertido)
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// Não implementado para este conversor
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}