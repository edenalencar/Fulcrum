using Fulcrum.Bu;
using Fulcrum.Util; // Adicionado para usar o LocalizationHelper
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Text;

namespace Fulcrum.View;

/// <summary>
/// Página para gerenciar perfis de som personalizados
/// </summary>
public sealed partial class PerfisPage : Page
{
    private ObservableCollection<PerfilSom> _perfisCollection = new();

    /// <summary>
    /// Inicializa uma nova instância da classe PerfisPage
    /// </summary>
    public PerfisPage()
    {
        this.InitializeComponent();

        // Garante que o helper de localização esteja inicializado
        LocalizationHelper.Initialize();
    }

    /// <summary>
    /// Manipula o evento de carregamento da página
    /// </summary>
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Carrega os perfis salvos
        await GerenciadorPerfis.Instance.CarregarPerfisAsync();

        // Atualiza a coleção de perfis para exibição
        _perfisCollection.Clear();
        foreach (var perfil in GerenciadorPerfis.Instance.Perfis)
        {
            _perfisCollection.Add(perfil);
        }

        // Se não houver perfis, cria um perfil padrão
        if (_perfisCollection.Count == 0)
        {
            var perfilPadrao = GerenciadorPerfis.Instance.CriarPerfilPadrao();
            _perfisCollection.Add(perfilPadrao);
            await GerenciadorPerfis.Instance.SalvarPerfilAsync(perfilPadrao);
        }

        // Atualiza a interface com o perfil ativo
        AtualizarPerfilAtivoUI();
    }

    /// <summary>
    /// Atualiza a interface para mostrar qual perfil está ativo
    /// </summary>
    private void AtualizarPerfilAtivoUI()
    {
        // Obtém o perfil ativo
        var perfilAtivo = GerenciadorPerfis.Instance.PerfilAtivo;

        // Atualiza o indicador global
        if (perfilAtivo != null)
        {
            // Usa a string formatada com o nome do perfil
            string perfilAtivoTexto = LocalizationHelper.GetString("PerfilAtivo", "Perfil Ativo:");
            txtPerfilAtivo.Text = $"{perfilAtivoTexto} {perfilAtivo.Nome}";
            perfilAtivoIndicador.Visibility = Visibility.Visible;
        }
        else
        {
            // Usa a string do arquivo de recursos
            txtPerfilAtivo.Text = LocalizationHelper.GetString("NoActiveProfileText/Text", "Nenhum perfil ativo");
            perfilAtivoIndicador.Visibility = Visibility.Collapsed;
        }

        // Atualiza os indicadores em cada item da lista
        foreach (var item in perfisList.Items)
        {
            if (item is PerfilSom perfil)
            {
                // Obtém o contêiner visual do item
                var container = perfisList.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    // Encontra o elemento de indicador ativo dentro do template
                    var indicador = FindVisualChild<Border>(container, "ativoIndicador");
                    if (indicador != null)
                    {
                        // Define a visibilidade com base no perfil ativo
                        indicador.Visibility = GerenciadorPerfis.Instance.EhPerfilAtivo(perfil)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Encontra um elemento filho dentro da árvore visual
    /// </summary>
    private T? FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
    {
        if (parent == null)
            return null;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T && (child as FrameworkElement)?.Name == name)
                return (T)child;

            var result = FindVisualChild<T>(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Manipula o evento de clique no botão Novo Perfil
    /// </summary>
    private async void BtnNovoPerfil_Click(object sender, RoutedEventArgs e)
    {
        // Exibe diálogo para inserir nome e descrição
        var dialogNome = new ContentDialog
        {
            Title = LocalizationHelper.GetString("NewProfileTitle", "Novo Perfil"),
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = LocalizationHelper.GetString("ProfileNameLabel", "Nome:") },
                    new TextBox { Name = "txtNome", PlaceholderText = LocalizationHelper.GetString("ProfileNamePlaceholder", "Digite o nome do perfil") },
                    new TextBlock { Text = LocalizationHelper.GetString("ProfileDescriptionLabel", "Descrição:"), Margin = new Thickness(0, 10, 0, 0) },
                    new TextBox { Name = "txtDescricao", PlaceholderText = LocalizationHelper.GetString("ProfileDescriptionPlaceholder", "Digite uma descrição (opcional)") }
                }
            },
            PrimaryButtonText = LocalizationHelper.GetString("CreateButtonText", "Criar"),
            CloseButtonText = LocalizationHelper.GetString("CancelButtonText", "Cancelar"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var resultado = await dialogNome.ShowAsync();
        if (resultado == ContentDialogResult.Primary)
        {
            var panel = (StackPanel)dialogNome.Content;
            var nome = "";
            var descricao = "";

            // Obtém os valores dos controles pelo nome
            foreach (var child in panel.Children)
            {
                if (child is TextBox textBox)
                {
                    if (textBox.Name == "txtNome")
                        nome = textBox.Text;
                    else if (textBox.Name == "txtDescricao")
                        descricao = textBox.Text;
                }
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                nome = LocalizationHelper.GetString("DefaultProfileName", "Novo Perfil");
            }

            // Cria o perfil baseado nas configurações atuais
            var perfil = GerenciadorPerfis.Instance.CriarPerfilDaConfigAtual(nome, descricao);
            _perfisCollection.Add(perfil);

            // Salva o perfil
            await GerenciadorPerfis.Instance.SalvarPerfilAsync(perfil);
        }
    }

    /// <summary>
    /// Manipula o evento de clique no botão Aplicar Perfil
    /// </summary>
    private void BtnAplicarPerfil_Click(object sender, RoutedEventArgs e)
    {
        if (perfisList.SelectedItem is PerfilSom perfilSelecionado)
        {
            // Aplica o perfil de som
            perfilSelecionado.Aplicar();

            // Define como o perfil ativo
            GerenciadorPerfis.Instance.DefinirPerfilAtivo(perfilSelecionado);

            // Atualiza a interface para mostrar qual perfil está ativo
            AtualizarPerfilAtivoUI();

            // Mostra animação de confirmação visual
            MostrarConfirmacaoVisual(perfilSelecionado.Nome);

            // Exibe mensagem de confirmação
            infoBar.Title = LocalizationHelper.GetString("ProfileAppliedTitle", "Perfil Aplicado");
            infoBar.Message = LocalizationHelper.GetFormattedString(
                "ProfileAppliedMessage",
                "Perfil '{0}' aplicado com sucesso!",
                perfilSelecionado.Nome);
            infoBar.Severity = InfoBarSeverity.Success;
            infoBar.IsOpen = true;

            // Destaca o item na lista (efeito visual)
            var item = perfisList.ContainerFromItem(perfilSelecionado) as ListViewItem;
            if (item != null)
            {
                // Aplica uma animação suave de destaque
                var originalBrush = item.Background;
                item.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);

                // Cria um timer para voltar à cor original
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (s, args) =>
                {
                    item.Background = originalBrush;
                    timer.Stop();
                };
                timer.Start();
            }

            // Oculta a mensagem após 3 segundos
            var infoBarTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            infoBarTimer.Tick += (s, args) =>
            {
                infoBar.IsOpen = false;
                infoBarTimer.Stop();
            };
            infoBarTimer.Start();
        }
    }

    /// <summary>
    /// Mostra uma animação de confirmação visual para indicar que o perfil foi aplicado
    /// </summary>
    private void MostrarConfirmacaoVisual(string nomePerfil)
    {
        // Cria um popup de confirmação animado
        var popupConfirmacao = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = "\uE930", // Checkmark symbol
                        FontSize = 36,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green)
                    },
                    new TextBlock
                    {
                        Text = LocalizationHelper.GetString("ProfileAppliedTitle", "Perfil Aplicado!"),
                        Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = LocalizationHelper.GetFormattedString(
                            "ProfileAppliedDescription",
                            "O perfil '{0}' foi aplicado com sucesso.",
                            nomePerfil),
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Opacity = 0.8
                    }
                }
            },
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary
        };

        // Exibe o popup por um curto período e então fecha automaticamente
        _ = DispatcherQueue.TryEnqueue(async () =>
        {
            // Configura um temporizador para fechar automaticamente
            var autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            autoCloseTimer.Tick += (s, args) =>
            {
                popupConfirmacao.Hide();
                autoCloseTimer.Stop();
            };

            // Exibe o dialog
            autoCloseTimer.Start();
            await popupConfirmacao.ShowAsync();
        });
    }

    /// <summary>
    /// Manipula o evento de clique no botão Remover Perfil
    /// </summary>
    private async void BtnRemoverPerfil_Click(object sender, RoutedEventArgs e)
    {
        if (perfisList.SelectedItem is PerfilSom perfilSelecionado)
        {
            // Verifica se este é o perfil ativo
            bool eraPerfilAtivo = GerenciadorPerfis.Instance.EhPerfilAtivo(perfilSelecionado);

            // Pede confirmação antes de remover
            var dialog = new ContentDialog
            {
                Title = LocalizationHelper.GetString("DeleteProfileTitle", "Remover Perfil"),
                Content = LocalizationHelper.GetFormattedString(
                    "DeleteProfileConfirmation",
                    "Tem certeza que deseja remover o perfil '{0}'? Esta ação não pode ser desfeita.",
                    perfilSelecionado.Nome),
                PrimaryButtonText = LocalizationHelper.GetString("DeleteButton", "Remover"),
                CloseButtonText = LocalizationHelper.GetString("CancelButtonText", "Cancelar"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary)
            {
                // Remove o perfil
                GerenciadorPerfis.Instance.RemoverPerfil(perfilSelecionado);
                _perfisCollection.Remove(perfilSelecionado);

                // Se era o perfil ativo, remove a referência
                if (eraPerfilAtivo)
                {
                    // Utiliza a sobrecarga que aceita PerfilSom explicitamente
                    PerfilSom? perfilNulo = null;
                    GerenciadorPerfis.Instance.DefinirPerfilAtivo(perfilNulo);
                    AtualizarPerfilAtivoUI();
                }

                // Exibe mensagem de confirmação
                infoBar.Title = LocalizationHelper.GetString("ProfileDeletedTitle", "Perfil Removido");
                infoBar.Message = LocalizationHelper.GetFormattedString(
                    "ProfileDeletedMessage",
                    "Perfil '{0}' removido com sucesso!",
                    perfilSelecionado.Nome);
                infoBar.Severity = InfoBarSeverity.Informational;
                infoBar.IsOpen = true;

                // Oculta a mensagem após 3 segundos
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, args) =>
                {
                    infoBar.IsOpen = false;
                    timer.Stop();
                };
                timer.Start();
            }
        }
    }

    /// <summary>
    /// Manipula o evento de seleção alterada na lista de perfis
    /// </summary>
    private void PerfisList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Atualiza o estado dos botões com base na seleção
        bool temPerfilSelecionado = perfisList.SelectedItem != null;
        btnAplicarPerfil.IsEnabled = temPerfilSelecionado;
        btnRemoverPerfil.IsEnabled = temPerfilSelecionado;

        // Atualiza os detalhes do perfil quando um item é selecionado
        if (perfisList.SelectedItem is PerfilSom perfilSelecionado)
        {
            // Oculta a InfoBar que pede para selecionar um perfil
            infoBarPerfil.IsOpen = false;

            // Exibe as informações do perfil em um formato simples no TextBlock
            StringBuilder sb = new StringBuilder();

            foreach (var configuracao in perfilSelecionado.ConfiguracoesSom)
            {
                // Formata o volume como porcentagem
                var volumePorcentagem = (int)(configuracao.Value * 100);
                string nomeSom = ObterNomeLegivel(configuracao.Key);
                sb.AppendLine($"{nomeSom}: {volumePorcentagem}%");
            }

            // Define o texto no TextBlock
            txtConfiguracoesInfo.Text = sb.ToString();
        }
        else
        {
            // Se nenhum perfil estiver selecionado, exibe mensagem padrão
            txtConfiguracoesInfo.Text = LocalizationHelper.GetString("ConfigurationInfoText/Text", "Selecione um perfil para ver as configurações de som");
            infoBarPerfil.IsOpen = true;
        }
    }

    /// <summary>
    /// Converte a chave técnica do som para um nome mais amigável para exibição
    /// </summary>
    private string ObterNomeLegivel(string chaveSom)
    {
        return chaveSom switch
        {
            "chuva" => LocalizationHelper.GetString("Rain/Header", "Chuva"),
            "fogueira" => LocalizationHelper.GetString("Bonfire/Header", "Fogueira"),
            "lancha" => LocalizationHelper.GetString("Motorboat/Header", "Lancha"),
            "ondas" => LocalizationHelper.GetString("Waves/Header", "Ondas do Mar"),
            "passaros" => LocalizationHelper.GetString("Birds/Header", "Pássaros"),
            "praia" => LocalizationHelper.GetString("Beach/Header", "Praia"),
            "trem" => LocalizationHelper.GetString("Train/Header", "Trem"),
            "ventos" => LocalizationHelper.GetString("Wind/Header", "Ventos"),
            "cafeteria" => LocalizationHelper.GetString("CoffeeShop/Header", "Cafeteria"),
            _ => chaveSom // Se não encontrar correspondência, retorna a própria chave
        };
    }
}