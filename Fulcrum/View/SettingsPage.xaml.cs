using Fulcrum.Bu.Services;
using Fulcrum.Util;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;
using Windows.System;

namespace Fulcrum.View;

/// <summary>
/// Página para editar as configurações do aplicativo
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
    private AppHotKeyManager _hotKeyManager;

    /// <summary>
    /// Inicializa uma nova instância da página de configurações
    /// </summary>
    public SettingsPage()
    {
        // Inicializamos os componentes da interface uma única vez
        this.InitializeComponent();
        
        // Desabilitamos temporariamente os eventos para evitar alterações de tema
        // durante a carga inicial da página
        ThemeRadioButtons.SelectionChanged -= OnThemeSelectionChanged;
        
        // Carregamos o tema atual
        CarregarTemaAtual();
        
        // Carregamos as configurações de teclas de atalho
        CarregarConfiguracoesAtalhos();
        
        // Reativamos os eventos depois que a inicialização estiver completa
        ThemeRadioButtons.SelectionChanged += OnThemeSelectionChanged;
        
        // Registramos o evento de carregamento da página
        Loaded += SettingsPage_Loaded;
        
        // Obtém a instância do gerenciador de teclas de atalho
        _hotKeyManager = new AppHotKeyManager();
    }

    /// <summary>
    /// Manipulador do evento de carregamento da página
    /// </summary>
    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Define o texto de copyright
        txtDireitos.Text = "© 2024 Éden Alencar. Todos os direitos reservados.";
    }

    /// <summary>
    /// Carrega o tema atual das configurações
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
        
        // Importante: não alterar o tema aqui, apenas atualizar a UI
        // para refletir a configuração atual
    }

    /// <summary>
    /// Carrega as configurações de teclas de atalho
    /// </summary>
    private void CarregarConfiguracoesAtalhos()
    {
        // Carrega a configuração de habilitação de teclas de atalho globais
        bool atalhosGlobaisHabilitados = true; // Valor padrão é habilitado
        
        if (_localSettings.Values.ContainsKey(Constantes.Config.AtalhosGlobaisHabilitados))
        {
            atalhosGlobaisHabilitados = (bool)_localSettings.Values[Constantes.Config.AtalhosGlobaisHabilitados];
        }
        
        // Define o estado do toggle de atalhos globais
        toggleAtalhosGlobais.IsOn = atalhosGlobaisHabilitados;
    }

    /// <summary>
    /// Manipula a alteração na seleção de tema
    /// </summary>
    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Se não houver item selecionado ou se estivermos apenas carregando a página, sair
        if (ThemeRadioButtons.SelectedIndex < 0 || e.AddedItems.Count == 0) return;

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
    /// Manipula a alteração na opção de atalhos globais
    /// </summary>
    private void ToggleAtalhosGlobais_Toggled(object sender, RoutedEventArgs e)
    {
        bool isEnabled = toggleAtalhosGlobais.IsOn;
        
        // Salva a configuração
        _localSettings.Values[Constantes.Config.AtalhosGlobaisHabilitados] = isEnabled;
        
        // Atualiza o status no gerenciador de teclas de atalho
        // Verifica se o objeto foi inicializado antes de usá-lo
        if (_hotKeyManager == null)
        {
            _hotKeyManager = new AppHotKeyManager();
        }
        _hotKeyManager.HotKeysEnabled = isEnabled;
        
        // Mostra uma mensagem informativa
        atalhosMensagemInfo.Message = isEnabled
            ? "Teclas de atalho globais habilitadas. Você pode controlar o Fulcrum mesmo quando ele estiver em segundo plano."
            : "Teclas de atalho globais desabilitadas.";
            
        atalhosMensagemInfo.IsOpen = true;
        
        // Esconde a mensagem após alguns segundos
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
        {
            await System.Threading.Tasks.Task.Delay(4000);
            atalhosMensagemInfo.IsOpen = false;
        });
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
