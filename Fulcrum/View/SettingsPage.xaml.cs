using Fulcrum.Util;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;
using Windows.System;

namespace Fulcrum.View;

/// <summary>
/// Página de configurações do aplicativo
/// </summary>
public sealed partial class SettingsPage : Page
{
    // Armazenamento local para as configurações
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    /// <summary>
    /// Inicializa uma nova instância da classe SettingsPage
    /// </summary>
    public SettingsPage()
    {
        this.InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    /// <summary>
    /// Manipulador do evento de carregamento da página
    /// </summary>
    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Define o texto de copyright
        txtDireitos.Text = "© 2024 Éden Alencar. Todos os direitos reservados.";
        
        // Carrega a configuração salva do tema
        CarregarTemaAtual();
    }

    /// <summary>
    /// Carrega a configuração do tema salva nas preferências
    /// </summary>
    private void CarregarTemaAtual()
    {
        // Tenta obter a configuração salva
        var temaSalvo = _localSettings.Values[Constantes.Tema.TemaAppSelecionado] as string ?? Constantes.Tema.Default;
        
        // Define a seleção do RadioButtons baseado no tema salvo
        switch (temaSalvo)
        {
            case Constantes.Tema.Light:
                ThemeRadioButtons.SelectedIndex = 0;
                break;
            case Constantes.Tema.Dark:
                ThemeRadioButtons.SelectedIndex = 1;
                break;
            default:
                ThemeRadioButtons.SelectedIndex = 2;
                break;
        }
    }

    /// <summary>
    /// Altera o tema do aplicativo com base na seleção dos RadioButtons
    /// </summary>
    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeRadioButtons.SelectedIndex < 0) return;

        string temaKey;
        ElementTheme requestedTheme;

        switch (ThemeRadioButtons.SelectedIndex)
        {
            case 0:
                temaKey = Constantes.Tema.Light;
                requestedTheme = ElementTheme.Light;
                break;
            case 1:
                temaKey = Constantes.Tema.Dark;
                requestedTheme = ElementTheme.Dark;
                break;
            default:
                temaKey = Constantes.Tema.Default;
                requestedTheme = ElementTheme.Default;
                break;
        }

        // Salva a configuração
        _localSettings.Values[Constantes.Tema.TemaAppSelecionado] = temaKey;

        // Aplica o tema - utilizando método compatível com WinUI 3
        var app = Application.Current as Fulcrum.App;
        if (app?.Window?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = requestedTheme;
        }
    }

    /// <summary>
    /// Abre o cliente de email para enviar feedback
    /// </summary>
    private async void Feedback_Click(object sender, RoutedEventArgs e)
    {
        // Define o endereço de email para envio do feedback
        var emailUri = new Uri("mailto:feedback@fulcrum.app?subject=Feedback%20do%20Aplicativo%20Fulcrum");
        
        // Abre o cliente de email padrão
        await Launcher.LaunchUriAsync(emailUri);
    }
}
