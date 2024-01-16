using Fulcrum.Util;
using Microsoft.UI.Xaml;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fulcrum
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            Window.ExtendsContentIntoTitleBar = true;
            Window.Activate();

            string temaSalvo = ApplicationData.Current.LocalSettings.Values[Constantes.TemaAppSelecionado]?.ToString();
            if (App.Window?.Content is FrameworkElement frameworkElement)
            {
                switch (temaSalvo)
                {
                    case Constantes.Light:
                        frameworkElement.RequestedTheme = ElementTheme.Light;
                        break;
                    case Constantes.Dark:
                        frameworkElement.RequestedTheme = ElementTheme.Dark;
                        break;
                    default:
                        frameworkElement.RequestedTheme = ElementTheme.Default;
                        break;
                }
            }

        }
        public static Window Window { get; private set; }
    }
}
