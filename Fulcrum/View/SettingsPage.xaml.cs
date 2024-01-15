using Fulcrum.Util;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fulcrum.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            string temaSalvo = ApplicationData.Current.LocalSettings.Values[Constantes.TemaAppSelecionado]?.ToString();
            switch (temaSalvo)
            {
                case Constantes.Light:
                    Tema.SelectedIndex = 0;
                    break;
                case Constantes.Dark:
                    Tema.SelectedIndex = 1;
                    break;
                default:
                    Tema.SelectedIndex = 2;
                    break;
            }
        }
        private void themeMode_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (App.Window?.Content is FrameworkElement frameworkElement)
            {
                switch (((Microsoft.UI.Xaml.Controls.ContentControl)Tema.SelectedValue).Tag)
                {
                    case Constantes.Light:
                        frameworkElement.RequestedTheme = ElementTheme.Light;
                        break;
                    case Constantes.Dark:
                        frameworkElement.RequestedTheme = ElementTheme.Dark;
                        break;
                    case "Default":
                        frameworkElement.RequestedTheme = ElementTheme.Default;
                        break;
                }
                ApplicationData.Current.LocalSettings.Values[Constantes.TemaAppSelecionado] = frameworkElement.RequestedTheme.ToString();
            }
        }

        public string RightsText
        {
            get
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
                var rightsText = resourceLoader.GetString("Rights/Text");
                return string.Format(rightsText, DateTime.Now.Year);
            }
        }
    }



}
