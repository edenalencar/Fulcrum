using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

namespace Fulcrum.View;

/// <summary>
/// Página com informações sobre o aplicativo
/// </summary>
public sealed partial class AboutPage : Page
{
    /// <summary>
    /// Inicializa uma nova instância da classe AboutPage
    /// </summary>
    public AboutPage()
    {
        this.InitializeComponent();
        Loaded += AboutPage_Loaded;
    }

    /// <summary>
    /// Manipulador do evento de carregamento da página
    /// </summary>
    private void AboutPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Preenche informações dinâmicas
        var appVersion = GetAppVersion();
        txtVersion.Text = $"Versão {appVersion}";
    }

    /// <summary>
    /// Manipulador do evento de clique no botão para ver o código fonte
    /// </summary>
    private async void BtnSourceCode_Click(object sender, RoutedEventArgs e)
    {
        // Abre o repositório do GitHub no navegador padrão
        await Launcher.LaunchUriAsync(new Uri("https://github.com/edenalencar/fulcrum"));
    }

    /// <summary>
    /// Manipulador do evento de clique no botão para entrar em contato
    /// </summary>
    private async void BtnContactDeveloper_Click(object sender, RoutedEventArgs e)
    {
        // Abre o cliente de email padrão com o endereço de contato oficial
        await Launcher.LaunchUriAsync(new Uri("mailto:contact@fulcrumappofficial.com?subject=Feedback%20sobre%20Fulcrum"));
    }

    /// <summary>
    /// Manipulador do evento de clique no botão para relatar problemas
    /// </summary>
    private async void BtnReportIssue_Click(object sender, RoutedEventArgs e)
    {
        // Abre a página de issues do GitHub
        await Launcher.LaunchUriAsync(new Uri("https://github.com/edenalencar/fulcrum/issues"));
    }

    /// <summary>
    /// Manipulador do evento de clique no botão para avaliar na Microsoft Store
    /// </summary>
    private async void BtnRateApp_Click(object sender, RoutedEventArgs e)
    {
        // Abre a página do aplicativo na Microsoft Store para avaliação
        await Launcher.LaunchUriAsync(new Uri("https://apps.microsoft.com/detail/9PNHBQJWWK3J?hl=pt-br&gl=BR&ocid=pdpshare"));
    }

    /// <summary>
    /// Obtém a versão do aplicativo
    /// </summary>
    private string GetAppVersion()
    {
        var package = Windows.ApplicationModel.Package.Current;
        var packageId = package.Id;
        var version = packageId.Version;
        
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}