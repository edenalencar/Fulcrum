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
    /// <remarks>
    /// Se parameter for fornecido, será usado como formato personalizado.
    /// Se parameter for "Basic", retorna apenas a data e hora formatadas.
    /// Se parameter for nulo ou vazio, retorna "Criado em: {data formatada}".
    /// </remarks>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime date)
        {
            string format = "dd/MM/yyyy HH:mm";
            
            // Permite formatos personalizados
            if (parameter is string formatParam && !string.IsNullOrEmpty(formatParam))
            {
                if (formatParam == "Basic")
                {
                    return date.ToString(format);
                }
                else
                {
                    // Usa o parâmetro como formato personalizado
                    return date.ToString(formatParam);
                }
            }
            
            // Formato padrão com prefixo
            return $"Criado em: {date.ToString(format)}";
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