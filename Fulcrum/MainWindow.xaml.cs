using Fulcrum.View;
using Fulcrum.Bu.Services;
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
using System.Runtime.InteropServices;
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
    private AppWindow? _appWindow;
    private AppHotKeyManager _hotKeyManager;
    private IntPtr _windowHandle;

    // Para interceptar mensagens do Windows
    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProc _newWndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Constante para substituir o procedimento de janela
    private const int GWLP_WNDPROC = -4;

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

        // Obtém o handle da janela para o gerenciamento de teclas de atalho
        _windowHandle = WindowNative.GetWindowHandle(this);

        // Configura o hook para mensagens do Windows
        _newWndProc = new WndProc(WindowProc);
        _oldWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_newWndProc));

        // Inicializa o serviço de teclas de atalho
        HotKeyService.Instance.Initialize(_windowHandle, DispatcherQueue);

        // Inicializa o gerenciador de teclas de atalho do aplicativo
        _hotKeyManager = new AppHotKeyManager();

        // Obtém a AppWindow para configuração adicional
        var windowId = Win32Interop.GetWindowIdFromWindow(_windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            // Configura o tamanho inicial da janela
            _appWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));

            // Tratamento para quando o aplicativo for fechado
            _appWindow.Closing += AppWindow_Closing;
        }

        // Registra manipuladores de evento após a inicialização
        this.Activated += MainWindow_Activated;

        // Configura a navegação inicial
        ConfigureNavigation();

        // Aplica o tema escolhido pelo usuário
        AplicarTema();
    }

    /// <summary>
    /// Procedimento de janela customizado para capturar mensagens do Windows
    /// </summary>
    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // Permite que o serviço de teclas de atalho processe mensagens
        if (HotKeyService.Instance.ProcessWindowMessage(hWnd, msg, wParam, lParam))
        {
            return IntPtr.Zero;
        }

        // Passa para o procedimento de janela original
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    /// <summary>
    /// Manipula o evento de fechamento da janela
    /// </summary>
    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Desregistra todas as teclas de atalho e libera recursos
        _hotKeyManager?.Dispose();
        HotKeyService.Instance.Dispose();
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
    /// Manipula o evento de seleção alterada na navegação
    /// </summary>
    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            // Navegar para a página de configurações
            contentFrame.Navigate(typeof(View.SettingsPage));
        }
        else if (args.SelectedItemContainer != null)
        {
            var navItemTag = args.SelectedItemContainer.Tag.ToString();

            switch (navItemTag)
            {
                case "home":
                    contentFrame.Navigate(typeof(View.HomePage));
                    break;
                case "perfis":
                    contentFrame.Navigate(typeof(View.PerfisPage));
                    break;
                case "settings":
                    contentFrame.Navigate(typeof(View.SettingsPage));
                    break;
                case "sobre":
                    contentFrame.Navigate(typeof(View.AboutPage));
                    break;
            }
        }
    }
}
