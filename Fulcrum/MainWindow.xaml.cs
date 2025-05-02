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
using Fulcrum.Bu; // Adicionado para acessar o AudioManager
using Fulcrum.Util; // Adicionado para acessar a classe Constantes

namespace Fulcrum;

/// <summary>
/// Janela principal do aplicativo
/// </summary>
public sealed partial class MainWindow : Window
{
    // Armazena o frame de conteúdo atual
    private Frame ContentFrame { get; set; }

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
    
    // Flag para controlar a inicialização dos reprodutores de áudio
    private bool _audioInitialized = false;

    /// <summary>
    /// Inicializa uma nova instância da classe MainWindow
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();

        // Configuração da janela
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Title = "Fulcrum - Sons Ambientes";

        // Define a altura e largura inicial da janela
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var recommendedSize = displayArea.WorkArea;
        
        // Define uma altura maior para evitar barra de rolagem na aba início
        AppWindow.Resize(new Windows.Graphics.SizeInt32 
        { 
            Width = (int)(recommendedSize.Width * 0.85), 
            Height = (int)(recommendedSize.Height * 0.90) 
        });

        // Centraliza a janela
        AppWindow.Move(new Windows.Graphics.PointInt32(
            (recommendedSize.Width - AppWindow.Size.Width) / 2,
            (recommendedSize.Height - AppWindow.Size.Height) / 2
        ));

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
            // Tratamento para quando o aplicativo for fechado
            _appWindow.Closing += AppWindow_Closing;
        }

        // Registra manipuladores de eventos
        this.Activated += MainWindow_Activated;
        navigationView.SelectionChanged += NavigationView_SelectionChanged;
        
        // Adicionar evento para mudanças de tamanho
        this.SizeChanged += MainWindow_SizeChanged;

        // Navegação inicial
        ContentFrame = contentFrame;
        navigationView.SelectedItem = navigationView.MenuItems[0];

        // Aplica o tema escolhido pelo usuário
        AplicarTema();

        // Ajustar NavigationView para o tamanho atual da janela
        AdjustNavigationViewForWindowSize();

        // Inicializa todos os reprodutores de áudio com volume 0.0
        InitializeAudioPlayers();
        
        // Restaurar estado do painel de navegação
        RestoreNavigationViewState();
    }
    
    /// <summary>
    /// Inicializa os reprodutores de áudio apenas uma vez
    /// </summary>
    private void InitializeAudioPlayers()
    {
        if (!_audioInitialized)
        {
            AudioManager.Instance.InitializePlayers(0.0);
            _audioInitialized = true;
            System.Diagnostics.Debug.WriteLine("Reprodutores de áudio inicializados com volume zero");
        }
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
        
        // Salva o estado dos volumes antes de fechar
        AudioManager.Instance.SalvarEstadoVolumes();
        
        // Salva o estado do painel de navegação antes de fechar
        SaveNavigationViewState();
        
        // Libera recursos do AudioManager
        AudioManager.Instance.Dispose();
        
        // Libera recursos do serviço de notificações
        NotificationService.Instance.Dispose();
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
        try
        {
            // Obtém o tema das configurações
            var tema = _localSettings.Values[Constantes.Tema.TemaAppSelecionado] as string ?? "Default";
            System.Diagnostics.Debug.WriteLine($"Aplicando tema: {tema}");

            // Define o tema da aplicação
            if (Content is FrameworkElement rootElement)
            {
                ElementTheme elementTheme = tema switch
                {
                    Constantes.Tema.Light => ElementTheme.Light,
                    Constantes.Tema.Dark => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
                
                rootElement.RequestedTheme = elementTheme;
                System.Diagnostics.Debug.WriteLine($"Tema aplicado com sucesso: {elementTheme}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao aplicar tema: {ex.Message}");
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
        // Salva o estado atual dos volumes antes de navegar para outra página
        AudioManager.Instance.SalvarEstadoVolumes();
        
        Type targetPage = null;
        
        if (args.IsSettingsSelected)
        {
            // Navegar para a página de configurações
            targetPage = typeof(View.SettingsPage);
        }
        else if (args.SelectedItemContainer != null)
        {
            var navItemTag = args.SelectedItemContainer.Tag.ToString();

            switch (navItemTag)
            {
                case "home":
                    targetPage = typeof(View.HomePage);
                    break;
                case "perfis":
                    targetPage = typeof(View.PerfisPage);
                    break;
                case "settings":
                    targetPage = typeof(View.SettingsPage);
                    break;
                case "sobre":
                    targetPage = typeof(View.AboutPage);
                    break;
            }
        }
        
        // Navega para a página selecionada usando SuppressNavigationTransitionInfo
        // para evitar que o áudio seja reinicializado durante a transição
        if (targetPage != null && contentFrame.CurrentSourcePageType != targetPage)
        {
            contentFrame.Navigate(targetPage, null, new SuppressNavigationTransitionInfo());
        }
    }

    /// <summary>
    /// Ajusta o NavigationView com base no tamanho atual da janela
    /// </summary>
    private void AdjustNavigationViewForWindowSize()
    {
        double windowWidth = this.Bounds.Width;
        double windowHeight = this.Bounds.Height;
        
        if (windowWidth < 640)
        {
            // Em telas muito estreitas, use o modo mínimo
            navigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
            System.Diagnostics.Debug.WriteLine($"NavigationView ajustado para modo mínimo (largura: {windowWidth})");
        }
        else if (windowWidth < 900)
        {
            // Em telas médias, use o modo compacto
            navigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
            System.Diagnostics.Debug.WriteLine($"NavigationView ajustado para modo compacto (largura: {windowWidth})");
        }
        else
        {
            // Em telas largas, use o modo expandido
            navigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            
            // Verifica se a orientação é paisagem com tela grande para decidir se abre o painel
            bool isLandscape = windowWidth > windowHeight;
            if (isLandscape && windowWidth > 1200)
            {
                navigationView.IsPaneOpen = true;
                System.Diagnostics.Debug.WriteLine($"NavigationView ajustado para modo expandido com painel aberto (largura: {windowWidth})");
            }
            else
            {
                // Em landscape com tela menor ou em portrait, deixa o painel fechado por padrão
                // mas permite que as configurações salvas definam o estado
                System.Diagnostics.Debug.WriteLine($"NavigationView ajustado para modo expandido (largura: {windowWidth})");
            }
        }
    }

    /// <summary>
    /// Salva o estado do NavigationView
    /// </summary>
    private void SaveNavigationViewState()
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["NavigationViewIsPaneOpen"] = navigationView.IsPaneOpen;
            localSettings.Values["NavigationViewPaneDisplayMode"] = (int)navigationView.PaneDisplayMode;
            System.Diagnostics.Debug.WriteLine($"Estado do NavigationView salvo: IsPaneOpen={navigationView.IsPaneOpen}, DisplayMode={navigationView.PaneDisplayMode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar estado do NavigationView: {ex.Message}");
        }
    }

    /// <summary>
    /// Restaura o estado do NavigationView
    /// </summary>
    private void RestoreNavigationViewState()
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            
            // Restaura o modo de exibição se estiver salvo
            if (localSettings.Values.TryGetValue("NavigationViewPaneDisplayMode", out var displayMode))
            {
                var savedMode = (NavigationViewPaneDisplayMode)(int)displayMode;
                // Só restauramos o modo de exibição se for compatível com o tamanho atual da tela
                if ((savedMode == NavigationViewPaneDisplayMode.Left && this.Bounds.Width >= 900) ||
                    (savedMode == NavigationViewPaneDisplayMode.LeftCompact && this.Bounds.Width >= 640))
                {
                    navigationView.PaneDisplayMode = savedMode;
                    System.Diagnostics.Debug.WriteLine($"Modo de exibição do NavigationView restaurado: {savedMode}");
                }
            }
            
            // Restaura o estado de abertura do painel (apenas no modo esquerdo)
            if (navigationView.PaneDisplayMode == NavigationViewPaneDisplayMode.Left &&
                localSettings.Values.TryGetValue("NavigationViewIsPaneOpen", out var isPaneOpen))
            {
                navigationView.IsPaneOpen = (bool)isPaneOpen;
                System.Diagnostics.Debug.WriteLine($"Estado de abertura do NavigationView restaurado: {navigationView.IsPaneOpen}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao restaurar estado do NavigationView: {ex.Message}");
        }
    }

    /// <summary>
    /// Manipula o evento de mudança de tamanho da janela
    /// </summary>
    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        AdjustNavigationViewForWindowSize();
        System.Diagnostics.Debug.WriteLine($"Janela redimensionada para {args.Size.Width}x{args.Size.Height}");
    }
}
