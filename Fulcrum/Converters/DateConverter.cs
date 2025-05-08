using Microsoft.UI.Xaml.Data;

namespace Fulcrum.Converters;

/// <summary>
/// Converte um objeto DateTime para uma string formatada
/// </summary>
public class DateConverter : IValueConverter
{
    /// <summary>
    /// Converte DateTime para string no formato desejado
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime date)
        {
            return $"Criado em: {date:dd/MM/yyyy HH:mm}";
        }
        return string.Empty;
    }

    /// <summary>
    /// Não implementado para este conversor
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}