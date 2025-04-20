using Microsoft.UI.Xaml.Data;
using System;

namespace Fulcrum.View
{
    /// <summary>
    /// Conversor que transforma valores de volume (0-1) em porcentagens (0-100%)
    /// </summary>
    public class VolumePercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double volume)
            {
                // Converte o valor de volume (0-1) para porcentagem (0-100)
                int percentage = (int)(volume * 100);
                return $"{percentage}%";
            }
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}