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
        InitializeComponent();
        
        // Aplica o idioma preferido do sistema
        ApplySystemLanguage();
        
        // Inicializa o AudioManager
        InitializeAudioManager();

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

        // Configurar propriedades de acessibilidade
        ConfigurarAcessibilidade();

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
    /// Inicializa o gerenciador de áudio central do aplicativo
    /// </summary>
    private void InitializeAudioManager()
    {
        try
        {
            // Obtém a instância do AudioManager (Singleton)
            var audioManager = AudioManager.Instance;
            System.Diagnostics.Debug.WriteLine("AudioManager inicializado com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar AudioManager: {ex.Message}");
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
        
        Type? targetPage = null;
        
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

    /// <summary>
    /// Configura as propriedades de acessibilidade para leitores de tela
    /// </summary>
    private void ConfigurarAcessibilidade()
    {
        try
        {
            // Configura o NavigationView como uma região de navegação
            AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                navigationView,
                "Navegação principal",
                "Menu principal do aplicativo Fulcrum",
                Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Navigation);

            // Configura o ContentFrame como a região principal
            AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                contentFrame,
                "Conteúdo principal",
                "Área principal de conteúdo do aplicativo",
                Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Main);
            
            // Registra um evento para configurar a acessibilidade quando a página é navegada
            contentFrame.Navigated += ContentFrame_Navigated;
            
            System.Diagnostics.Debug.WriteLine("Propriedades de acessibilidade configuradas com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar propriedades de acessibilidade: {ex.Message}");
        }
    }

    /// <summary>
    /// Manipula o evento de navegação completada para configurar a acessibilidade da página carregada
    /// </summary>
    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (contentFrame.Content is Page page)
        {
            // Configuração específica por tipo de página
            if (page is HomePage homePage)
            {
                // Verificando se a conversão não resultou em nulo antes de chamar o método
                ConfigurarAcessibilidadeHomePage(homePage);
            }
            else if (page is PerfisPage perfisPage)
            {
                // Verificando se a conversão não resultou em nulo antes de chamar o método
                ConfigurarAcessibilidadePerfisPage(perfisPage);
            }
            else if (page is SettingsPage settingsPage)
            {
                // Verificando se a conversão não resultou em nulo antes de chamar o método
                ConfigurarAcessibilidadeSettingsPage(settingsPage);
            }
        }
    }

    /// <summary>
    /// Configura a acessibilidade específica para a página inicial
    /// </summary>
    private void ConfigurarAcessibilidadeHomePage(HomePage page)
    {
        try
        {
            // Obtém os elementos da página para configurar
            var painelControles = page.FindName("painelControles") as FrameworkElement;
            if (painelControles != null)
            {
                AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                    painelControles,
                    "Controles principais",
                    "Controles para reprodução e ajuste de volume",
                    Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Main);
            }

            // Configura cada cartão de som como uma região de conteúdo
            ConfigurarCardSons(page);
            
            System.Diagnostics.Debug.WriteLine("Propriedades de acessibilidade da HomePage configuradas");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar acessibilidade da HomePage: {ex.Message}");
        }
    }

    /// <summary>
    /// Configura os cartões de som com propriedades de acessibilidade corretas
    /// </summary>
    private void ConfigurarCardSons(HomePage page)
    {
        var sons = new[] { "chuva", "fogueira", "lancha", "ondas", "passaros", "praia", "trem", "ventos", "cafeteria" };
        
        foreach (var som in sons)
        {
            try
            {
                var card = page.FindName($"card{som.Substring(0, 1).ToUpper()}{som.Substring(1)}") as FrameworkElement;
                if (card != null)
                {
                    AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                        card,
                        $"Card de som {som}",
                        $"Controles para o som de {som}",
                        Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Custom);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao configurar card de som {som}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Configura a acessibilidade específica para a página de perfis
    /// </summary>
    private void ConfigurarAcessibilidadePerfisPage(PerfisPage page)
    {
        try
        {
            // Configura a lista de perfis como uma região de navegação
            var perfisList = page.FindName("perfisList") as FrameworkElement;
            if (perfisList != null)
            {
                AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                    perfisList,
                    "Lista de perfis",
                    "Lista de perfis de som disponíveis",
                    Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Custom);
            }

            // Configura a área de detalhes como uma região de conteúdo
            var detalhesGrid = page.FindName("detalhesGrid") as FrameworkElement;
            if (detalhesGrid != null)
            {
                AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                    detalhesGrid,
                    "Detalhes do perfil",
                    "Informações detalhadas sobre o perfil selecionado",
                    Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Custom);
            }
            
            System.Diagnostics.Debug.WriteLine("Propriedades de acessibilidade da PerfisPage configuradas");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar acessibilidade da PerfisPage: {ex.Message}");
        }
    }

    /// <summary>
    /// Configura a acessibilidade específica para a página de configurações
    /// </summary>
    private void ConfigurarAcessibilidadeSettingsPage(SettingsPage page)
    {
        try
        {
            // Configura o painel de tema como formulário
            var temaGrid = page.FindName("temaGrid") as FrameworkElement;
            if (temaGrid != null)
            {
                AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                    temaGrid,
                    "Configurações de tema",
                    "Opções para personalizar o tema visual do aplicativo",
                    Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Form);
            }

            // Configura o painel de atalhos como região
            var atalhosGrid = page.FindName("atalhosGrid") as FrameworkElement;
            if (atalhosGrid != null)
            {
                AcessibilidadeHelper.ConfigurarParaLeitoresEscreens(
                    atalhosGrid,
                    "Configurações de teclas de atalho",
                    "Opções para configurar as teclas de atalho do aplicativo",
                    Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Custom);
            }
            
            System.Diagnostics.Debug.WriteLine("Propriedades de acessibilidade da SettingsPage configuradas");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar acessibilidade da SettingsPage: {ex.Message}");
        }
    }

    /// <summary>
    /// Aplica o idioma do sistema ao aplicativo
    /// </summary>
    private void ApplySystemLanguage()
    {
        try
        {
            // Obtém o idioma preferido do usuário
            //string currentLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
            string currentLanguage = "en-US"; // Para fins de teste, forçando o inglês

            // Se não for português ou inglês, usa o inglês como padrão
            if (currentLanguage.StartsWith("en") || !currentLanguage.StartsWith("pt"))
            {
                currentLanguage = "en-US";
            }            
            else if (currentLanguage.StartsWith("pt"))
            {
                currentLanguage = "pt-BR";
            }            

                // Define o idioma para a aplicação
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = currentLanguage;
            
            System.Diagnostics.Debug.WriteLine($"[IDIOMA] Aplicando idioma: {currentLanguage}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IDIOMA] Erro ao aplicar idioma: {ex.Message}");
        }
    }
}
