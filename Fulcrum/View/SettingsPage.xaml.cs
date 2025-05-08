using Fulcrum.Bu.Services;
using Fulcrum.Util;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        // Carregamos o tamanho de fonte atual
        CarregarTamanhoFonteAtual();

        // Reativamos os eventos depois que a inicialização estiver completa
        ThemeRadioButtons.SelectionChanged += OnThemeSelectionChanged;
        FontSizeRadioButtons.SelectionChanged += OnFontSizeSelectionChanged;

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
        // O conteúdo desse método foi removido, pois o elemento txtDireitos
        // não existe mais após a remoção do card "Sobre o Fulcrum"
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
    /// Carrega o tamanho de fonte atual das configurações
    /// </summary>
    private void CarregarTamanhoFonteAtual()
    {
        // Tenta obter a configuração salva
        var tamanhoSalvo = _localSettings.Values[Constantes.Config.TamanhoFonte] as string ?? "Médio";

        // Define a seleção do RadioButtons baseado no tamanho salvo
        switch (tamanhoSalvo)
        {
            case "Pequeno":
                FontSizeRadioButtons.SelectedIndex = 0;
                break;
            case "Grande":
                FontSizeRadioButtons.SelectedIndex = 2;
                break;
            case "Extra Grande":
                FontSizeRadioButtons.SelectedIndex = 3;
                break;
            default: // Médio (padrão)
                FontSizeRadioButtons.SelectedIndex = 1;
                break;
        }
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
    /// Manipula a alteração na seleção de tamanho de fonte
    /// </summary>
    private void OnFontSizeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Se não houver item selecionado, sair
        if (FontSizeRadioButtons.SelectedIndex < 0 || e.AddedItems.Count == 0) return;

        string tamanhoFonte;
        double fatorEscala;

        switch (FontSizeRadioButtons.SelectedIndex)
        {
            case 0:
                tamanhoFonte = "Pequeno";
                fatorEscala = 0.85;
                break;
            case 2:
                tamanhoFonte = "Grande";
                fatorEscala = 1.2;
                break;
            case 3:
                tamanhoFonte = "Extra Grande";
                fatorEscala = 1.5;
                break;
            default:
                tamanhoFonte = "Médio";
                fatorEscala = 1.0;
                break;
        }

        // Salva a configuração
        _localSettings.Values[Constantes.Config.TamanhoFonte] = tamanhoFonte;

        // Aplica o tamanho de fonte
        AplicarTamanhoFonte(fatorEscala);
    }

    /// <summary>
    /// Aplica o fator de escala às fontes do aplicativo
    /// </summary>
    private void AplicarTamanhoFonte(double fatorEscala)
    {
        try
        {
            // Obtém o tamanho de fonte atualmente selecionado
            string tamanhoFonteSelecionado = _localSettings.Values[Constantes.Config.TamanhoFonte] as string ?? "Médio";

            var app = Application.Current as Fulcrum.App;
            if (app?.Window?.Content is FrameworkElement rootElement)
            {
                // Atualiza os recursos de tamanho de fonte
                var resources = rootElement.Resources;

                // Verifica se as chaves já existem, caso não, cria-as com valores padrão
                if (!resources.ContainsKey("TextControlThemeFontSize"))
                    resources.Add("TextControlThemeFontSize", 14.0);
                if (!resources.ContainsKey("BodyTextBlockFontSize"))
                    resources.Add("BodyTextBlockFontSize", 14.0);
                if (!resources.ContainsKey("SubtitleTextBlockFontSize"))
                    resources.Add("SubtitleTextBlockFontSize", 18.0);
                if (!resources.ContainsKey("TitleTextBlockFontSize"))
                    resources.Add("TitleTextBlockFontSize", 24.0);
                if (!resources.ContainsKey("TitleLargeTextBlockFontSize"))
                    resources.Add("TitleLargeTextBlockFontSize", 28.0);
                if (!resources.ContainsKey("HeaderTextBlockFontSize"))
                    resources.Add("HeaderTextBlockFontSize", 46.0);

                // Agora atualiza os tamanhos com o fator de escala
                resources["TextControlThemeFontSize"] = 14 * fatorEscala;
                resources["BodyTextBlockFontSize"] = 14 * fatorEscala;
                resources["SubtitleTextBlockFontSize"] = 18 * fatorEscala;
                resources["TitleTextBlockFontSize"] = 24 * fatorEscala;
                resources["TitleLargeTextBlockFontSize"] = 28 * fatorEscala;
                resources["HeaderTextBlockFontSize"] = 46 * fatorEscala;

                // Força uma atualização nos estilos existentes para aplicar os novos tamanhos
                rootElement.RequestedTheme = rootElement.RequestedTheme; // Isso força a reavaliação dos estilos

                // Anuncia a mudança para leitores de tela
                AcessibilidadeHelper.AnunciarParaLeitoresEscreens(
                    FontSizeRadioButtons,
                    $"Tamanho de fonte alterado para {tamanhoFonteSelecionado}",
                    true);

                System.Diagnostics.Debug.WriteLine($"Tamanho de fonte alterado: {tamanhoFonteSelecionado} (fator: {fatorEscala})");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao aplicar tamanho de fonte: {ex.Message}");
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
        try
        {
            // Usa LocalizationHelper em vez de ResourceLoader direto
            atalhosMensagemInfo.Message = isEnabled
                ? LocalizationHelper.GetString("ShortcutsEnabledMessage", "Atalhos de teclado globais ativados. Você pode controlar o Fulcrum mesmo quando ele estiver em segundo plano.")
                : LocalizationHelper.GetString("ShortcutsDisabledMessage", "Atalhos de teclado globais desativados.");
        }
        catch (Exception ex)
        {
            // Fallback para mensagens hardcoded em caso de erro
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar recursos: {ex.Message}");
            atalhosMensagemInfo.Message = isEnabled
                ? "Atalhos de teclado globais ativados. Você pode controlar o Fulcrum mesmo quando ele estiver em segundo plano."
                : "Atalhos de teclado globais desativados.";
        }

        atalhosMensagemInfo.IsOpen = true;

        // Esconde a mensagem após alguns segundos
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
        {
            await System.Threading.Tasks.Task.Delay(4000);
            atalhosMensagemInfo.IsOpen = false;
        });
    }
}
