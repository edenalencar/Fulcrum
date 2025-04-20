using Fulcrum.View;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Storage; // Adicionado para o ApplicationDataContainer

namespace Fulcrum;

/// <summary>
/// Janela principal do aplicativo
/// </summary>
public sealed partial class MainWindow : Window
{
    // Armazenamento local para as configurações
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    /// <summary>
    /// Inicializa uma nova instância da classe MainWindow
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Fulcrum - Sons Ambientes";

        // Configura a barra de título personalizada
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Define o tamanho inicial da janela para ser mais adequado ao conteúdo
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
        // Define um tamanho inicial mais compacto: 1000x700 pixels
        appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));

        // Registra manipuladores de evento após a inicialização
        this.Activated += MainWindow_Activated;

        // Configura a navegação inicial
        ConfigureNavigation();

        // Aplica o tema escolhido pelo usuário
        AplicarTema();
    }

    /// <summary>
    /// Manipulador do evento ativado da janela
    /// </summary>
    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        // Ajusta a opacidade da barra de título quando a janela estiver ativa/inativa
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            AppTitleBar.Opacity = 0.8;
        }
        else
        {
            AppTitleBar.Opacity = 1.0;
        }
    }

    /// <summary>
    /// Aplica o tema escolhido pelo usuário
    /// </summary>
    private void AplicarTema()
    {
        // Obtém o tema das configurações
        var tema = _localSettings.Values["TemaAppSelecionado"] as string ?? "Default";

        // Define o tema da aplicação
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = tema switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    /// <summary>
    /// Configura a navegação inicial para a página inicial (HomePage)
    /// </summary>
    private void ConfigureNavigation()
    {
        // Navega para a página inicial
        contentFrame.Navigate(typeof(HomePage), null, new EntranceNavigationTransitionInfo());
    }

    /// <summary>
    /// Manipula a mudança de seleção na barra de navegação
    /// </summary>
    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            // Navega para a página de configurações
            contentFrame.Navigate(typeof(SettingsPage), null, new SlideNavigationTransitionInfo());
        }
        else if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            // Identifica a página selecionada com base na tag
            Type? pageType = null;

            // Usa a tag para determinar a página de destino
            var tag = selectedItem.Tag as string;
            if (tag == "HomeSelected")
            {
                pageType = typeof(HomePage);
            }

            // Navega para a página selecionada se foi identificada
            if (pageType != null && contentFrame.CurrentSourcePageType != pageType)
            {
                contentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo());
            }
        }
    }
}
