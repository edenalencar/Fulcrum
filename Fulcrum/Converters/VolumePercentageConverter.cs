using Microsoft.UI.Xaml.Data;

namespace Fulcrum.Converters;

/// <summary>
/// Converte um valor de volume (0.0 a 1.0) para um percentual formatado
/// </summary>
public class VolumePercentageConverter : IValueConverter
{
    /// <summary>
    /// Converte valores de volume para percentual formatado
    /// </summary>
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

    /// <summary>
    /// Não implementado para este conversor
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
