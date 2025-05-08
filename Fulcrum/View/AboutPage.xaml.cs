using Fulcrum.Util;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        // Inicializa o LocalizationHelper se ainda não foi inicializado
        LocalizationHelper.Initialize();

        // Preenche informações dinâmicas com texto localizado
        var appVersion = GetAppVersion();

        // Pega o texto do recurso AboutVersion e formata com a versão
        // usando o LocalizationHelper em vez do ResourceLoader direto
        string versionTextFormat = LocalizationHelper.GetString("AboutVersion/Text", "Versão {0}");
        txtVersion.Text = string.Format(versionTextFormat, appVersion);

        // Atualiza o texto de direitos autorais com o ano atual
        // usando o LocalizationHelper em vez do ResourceLoader direto
        string rightsTextFormat = LocalizationHelper.GetString("Rights/Text", "@{0} Éden Alencar. Todos os direitos reservados");
        txtDireitos.Text = string.Format(rightsTextFormat, DateTime.Now.Year);
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
        await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9PNHBQJWWK3J"));
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