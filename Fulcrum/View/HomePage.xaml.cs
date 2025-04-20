using Fulcrum.Bu;
using Fulcrum.Util;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NAudio.Wave;
using System;
using Windows.UI;

namespace Fulcrum.View;

/// <summary>
/// Página principal que exibe os controles de som ambiente
/// </summary>
public sealed partial class HomePage : Page
{
    private bool _isInitialized = false;
    private DispatcherTimer _sleepTimerDisplayUpdateTimer;

    /// <summary>
    /// Inicializa uma nova instância da classe HomePage
    /// </summary>
    public HomePage()
    {
        this.InitializeComponent();
        
        // Se já existirem reprodutores, não inicializa novamente
        if (AudioManager.Instance.GetQuantidadeReprodutor > 0)
        {
            _isInitialized = true;
        }
        else
        {
            InitializeAudioPlayers();
            _isInitialized = true;
        }
        
        // Inicializar timer de atualização do display
        _sleepTimerDisplayUpdateTimer = new DispatcherTimer();
        _sleepTimerDisplayUpdateTimer.Interval = TimeSpan.FromSeconds(10);
        _sleepTimerDisplayUpdateTimer.Tick += SleepTimerDisplayUpdateTimer_Tick;
        
        // Registrar manipuladores de eventos para o SleepTimerService
        SleepTimerService.Instance.TimerStarted += SleepTimer_TimerStarted;
        SleepTimerService.Instance.TimerCancelled += SleepTimer_TimerCancelled;
        SleepTimerService.Instance.TimerCompleted += SleepTimer_TimerCompleted;
        SleepTimerService.Instance.TimerUpdated += SleepTimer_TimerUpdated;
        
        // Atualizar visibilidade do timer
        UpdateTimerDisplay();
        
        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;
    }

    /// <summary>
    /// Manipula o evento de página carregada
    /// </summary>
    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        // Aplica animações iniciais aos cards
        ApplyEntryAnimations();
        
        // Configura os visualizadores de áudio
        ConfigureWaveformVisualizers();
        
        // Sincroniza os valores dos sliders com os valores dos reprodutores
        if (_isInitialized)
        {
            SyncSliderValues();
        }
    }
    
    /// <summary>
    /// Manipula o evento de página descarregada
    /// </summary>
    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Salva o estado dos volumes antes de navegar para outra página
        AudioManager.Instance.SalvarEstadoVolumes();
        AudioManager.Instance.SalvarEstadoEfeitos();
    }

    /// <summary>
    /// Aplica animações de entrada em todos os cards
    /// </summary>
    private void ApplyEntryAnimations()
    {
        // As animações são controladas pelos estilos definidos no XAML
    }
    
    /// <summary>
    /// Configura os visualizadores de forma de onda para cada reprodutor de áudio
    /// </summary>
    private void ConfigureWaveformVisualizers()
    {
        try
        {
            // Verifica se os reprodutores foram inicializados corretamente antes de configurar os visualizadores
            if (AudioManager.Instance.GetQuantidadeReprodutor == 0)
            {
                System.Diagnostics.Debug.WriteLine("Não há reprodutores inicializados para configurar visualizadores");
                return;
            }

            // Configura cada visualizador apenas se o reprodutor correspondente existir
            var reprodutores = AudioManager.Instance.GetListReprodutores();
            
            if (reprodutores.ContainsKey(Constantes.Sons.Chuva))
                ConfigureVisualizer(Constantes.Sons.Chuva, chuvaWaveform, Colors.DeepSkyBlue);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Fogueira))
                ConfigureVisualizer(Constantes.Sons.Fogueira, fogueiraWaveform, Colors.OrangeRed);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Ondas))
                ConfigureVisualizer(Constantes.Sons.Ondas, ondasWaveform, Colors.LightSeaGreen);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Passaros))
                ConfigureVisualizer(Constantes.Sons.Passaros, passarosWaveform, Colors.YellowGreen);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Praia))
                ConfigureVisualizer(Constantes.Sons.Praia, praiaWaveform, Colors.Gold);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Trem))
                ConfigureVisualizer(Constantes.Sons.Trem, tremWaveform, Colors.Gray);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Ventos))
                ConfigureVisualizer(Constantes.Sons.Ventos, ventosWaveform, Colors.LightBlue);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Cafeteria))
                ConfigureVisualizer(Constantes.Sons.Cafeteria, cafeteriaWaveform, Colors.SandyBrown);
                
            if (reprodutores.ContainsKey(Constantes.Sons.Lancha))
                ConfigureVisualizer(Constantes.Sons.Lancha, lanchaWaveform, Colors.MediumPurple);
        }
        catch (Exception ex)
        {
            // Log de erro ou exibir mensagem para o usuário
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar visualizadores: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Configura um visualizador específico para um reprodutor de áudio
    /// </summary>
    private void ConfigureVisualizer(string soundId, Microsoft.UI.Xaml.Shapes.Polyline waveformElement, Color color)
    {
        try
        {
            // Verificação já foi feita no método principal, então podemos prosseguir diretamente
            var reprodutor = AudioManager.Instance.GetReprodutorPorId(soundId);
            if (reprodutor != null)
            {
                reprodutor.ConfigureVisualizer(waveformElement);
                
                // Define a cor da forma de onda
                waveformElement.Stroke = new SolidColorBrush(color);
                
                System.Diagnostics.Debug.WriteLine($"Visualizador configurado com sucesso para {soundId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar visualizador para {soundId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Inicializa todos os reprodutores de áudio
    /// </summary>
    private void InitializeAudioPlayers()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Iniciando carregamento dos reprodutores de áudio...");
            
            // Adiciona os reprodutores ao gerenciador de áudio, com tratamento de erros individual
            TryAddAudioPlayer(Constantes.Sons.Chuva, () => new ReprodutorChuva());
            TryAddAudioPlayer(Constantes.Sons.Fogueira, () => new ReprodutorFogueira());
            TryAddAudioPlayer(Constantes.Sons.Lancha, () => new ReprodutorLancha());
            TryAddAudioPlayer(Constantes.Sons.Ondas, () => new ReprodutorOndas());
            TryAddAudioPlayer(Constantes.Sons.Passaros, () => new ReprodutorPassaros());
            TryAddAudioPlayer(Constantes.Sons.Praia, () => new ReprodutorPraia());
            TryAddAudioPlayer(Constantes.Sons.Trem, () => new ReprodutorTrem());
            TryAddAudioPlayer(Constantes.Sons.Ventos, () => new ReprodutorVentos());
            TryAddAudioPlayer(Constantes.Sons.Cafeteria, () => new ReprodutorCafeteria());
            
            System.Diagnostics.Debug.WriteLine($"Reprodutores carregados: {AudioManager.Instance.GetQuantidadeReprodutor}");
        }
        catch (Exception ex)
        {
            // Log de erro ou exibir mensagem para o usuário
            System.Diagnostics.Debug.WriteLine($"Erro geral ao inicializar reprodutores: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Tenta adicionar um reprodutor de áudio com tratamento de exceção individual
    /// </summary>
    private void TryAddAudioPlayer(string soundId, Func<Reprodutor> creatorFunc)
    {
        try
        {
            AudioManager.Instance.AddAudioPlayer(soundId, creatorFunc());
            System.Diagnostics.Debug.WriteLine($"Reprodutor {soundId} inicializado com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar reprodutor {soundId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Sincroniza os valores dos sliders com os valores atuais dos reprodutores
    /// </summary>
    private void SyncSliderValues()
    {
        try
        {
            // Verifica se há reprodutores antes de tentar sincronizar
            if (AudioManager.Instance.GetQuantidadeReprodutor == 0)
            {
                System.Diagnostics.Debug.WriteLine("Não há reprodutores para sincronizar com os sliders");
                return;
            }
            
            var reprodutores = AudioManager.Instance.GetListReprodutores();
            
            // Sincroniza apenas sliders cujos reprodutores existem
            if (reprodutores.ContainsKey(Constantes.Sons.Chuva)) {
                var slider = FindName("chuva") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Chuva, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Fogueira)) {
                var slider = FindName("fogueira") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Fogueira, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Lancha)) {
                var slider = FindName("lancha") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Lancha, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Ondas)) {
                var slider = FindName("ondas") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Ondas, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Passaros)) {
                var slider = FindName("passaros") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Passaros, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Praia)) {
                var slider = FindName("praia") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Praia, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Trem)) {
                var slider = FindName("trem") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Trem, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Ventos)) {
                var slider = FindName("ventos") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Ventos, slider);
            }
                
            if (reprodutores.ContainsKey(Constantes.Sons.Cafeteria)) {
                var slider = FindName("cafeteria") as Slider;
                if (slider != null) SyncSliderValue(Constantes.Sons.Cafeteria, slider);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao sincronizar sliders: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Sincroniza um slider específico com o volume atual do reprodutor
    /// </summary>
    private void SyncSliderValue(string soundId, Slider slider)
    {
        try
        {
            if (slider == null)
            {
                System.Diagnostics.Debug.WriteLine($"Slider para {soundId} não encontrado");
                return;
            }
            
            var reprodutor = AudioManager.Instance.GetReprodutorPorId(soundId);
            if (reprodutor != null && reprodutor.Reader != null)
            {
                slider.Value = reprodutor.Reader.Volume;
                System.Diagnostics.Debug.WriteLine($"Slider para {soundId} sincronizado com volume {reprodutor.Reader.Volume}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao sincronizar slider para {soundId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Evento de mudança de valor do controle deslizante (slider)
    /// </summary>
    private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;

        // Obtém o ID do som baseado no nome do slider
        string soundId = slider.Name;
        
        try
        {
            // Atualiza o volume do reprodutor correspondente - convertendo explicitamente para float
            AudioManager.Instance.AlterarVolume(soundId, (float)e.NewValue);
        }
        catch (Exception ex)
        {
            // Log de erro ou exibir mensagem para o usuário
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Evento de clique no botão reproduzir todos
    /// </summary>
    private void Play_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AudioManager.Instance.PlayAll();
        }
        catch (Exception ex)
        {
            // Log de erro ou exibir mensagem para o usuário
            System.Diagnostics.Debug.WriteLine($"Erro ao reproduzir: {ex.Message}");
        }
    }

    /// <summary>
    /// Evento de clique no botão pausar todos
    /// </summary>
    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AudioManager.Instance.PauseAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao pausar: {ex.Message}");
        }
    }

    /// <summary>
    /// Atualiza o temporizador de exibição a cada 10 segundos
    /// </summary>
    private void SleepTimerDisplayUpdateTimer_Tick(object? sender, object e)
    {
        UpdateTimerDisplay();
    }

    /// <summary>
    /// Evento chamado quando o temporizador de sono é iniciado
    /// </summary>
    private void SleepTimer_TimerStarted(object? sender, EventArgs e)
    {
        _sleepTimerDisplayUpdateTimer.Start();
        UpdateTimerDisplay();
    }

    /// <summary>
    /// Evento chamado quando o temporizador de sono é cancelado
    /// </summary>
    private void SleepTimer_TimerCancelled(object? sender, EventArgs e)
    {
        _sleepTimerDisplayUpdateTimer.Stop();
        UpdateTimerDisplay();
    }

    /// <summary>
    /// Evento chamado quando o temporizador de sono é completado
    /// </summary>
    private void SleepTimer_TimerCompleted(object? sender, EventArgs e)
    {
        _sleepTimerDisplayUpdateTimer.Stop();
        UpdateTimerDisplay();

        // Exibe notificação de que o timer foi concluído
        DispatcherQueue.TryEnqueue(() =>
        {
            var infoBar = new InfoBar
            {
                Title = "Temporizador concluído",
                Message = "O temporizador de sono foi encerrado. Todos os sons foram pausados.",
                Severity = InfoBarSeverity.Informational,
                IsOpen = true
            };

            // Adiciona a InfoBar diretamente ao ScrollViewer
            if (this.Content is ScrollViewer scrollViewer && 
                scrollViewer.Content is StackPanel mainStackPanel)
            {
                mainStackPanel.Children.Insert(0, infoBar);

                // Configurar um timer para remover a notificação após 5 segundos
                var notificationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                notificationTimer.Tick += (s, e) =>
                {
                    infoBar.IsOpen = false;
                    mainStackPanel.Children.Remove(infoBar);
                    (s as DispatcherTimer).Stop();
                };
                notificationTimer.Start();
            }
        });
    }

    /// <summary>
    /// Evento chamado quando o temporizador de sono é atualizado
    /// </summary>
    private void SleepTimer_TimerUpdated(object? sender, int remainingMinutes)
    {
        UpdateTimerDisplay();
    }

    /// <summary>
    /// Atualiza a exibição do temporizador de sono
    /// </summary>
    private void UpdateTimerDisplay()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var isActive = SleepTimerService.Instance.IsTimerActive;
            if (isActive)
            {
                timerDisplay.Text = SleepTimerService.Instance.GetFormattedTimeRemaining();
                timerContainer.Visibility = Visibility.Visible;
                SetSleepTimerButton.Content = "Alterar Timer";
                cancelTimerButton.Visibility = Visibility.Visible;
            }
            else
            {
                timerContainer.Visibility = Visibility.Collapsed;
                SetSleepTimerButton.Content = "Definir Timer";
                cancelTimerButton.Visibility = Visibility.Collapsed;
            }
        });
    }

    /// <summary>
    /// Manipulador para o botão de definir temporizador
    /// </summary>
    private async void SetSleepTimer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Opções de tempo para o temporizador
            var timeOptions = new string[]
            {
                "5 minutos",
                "15 minutos",
                "30 minutos",
                "45 minutos", 
                "1 hora",
                "2 horas",
                "3 horas",
                "4 horas",
                "8 horas"
            };

            var dialog = new ContentDialog
            {
                Title = "Temporizador de Sono",
                PrimaryButtonText = "Confirmar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
            };

            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Escolha quanto tempo o Fulcrum deve reproduzir antes de pausar automaticamente:",
                TextWrapping = TextWrapping.Wrap
            });

            var comboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                SelectedIndex = 2 // 30 minutos como padrão
            };

            foreach (var option in timeOptions)
            {
                comboBox.Items.Add(option);
            }

            panel.Children.Add(comboBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            
            // Corrigido: verifica se comboBox não é nulo e se tem um item selecionado
            if (result == ContentDialogResult.Primary && comboBox != null && comboBox.SelectedIndex >= 0)
            {
                // Converter a seleção em minutos
                int minutes = GetMinutesFromSelection(comboBox.SelectedIndex);
                if (minutes > 0)
                {
                    SleepTimerService.Instance.StartTimer(minutes);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao definir timer: {ex.Message}");
        }
    }

    /// <summary>
    /// Converte o índice de seleção em minutos
    /// </summary>
    private int GetMinutesFromSelection(int selectedIndex)
    {
        return selectedIndex switch
        {
            0 => 5,      // 5 minutos
            1 => 15,     // 15 minutos
            2 => 30,     // 30 minutos
            3 => 45,     // 45 minutos
            4 => 60,     // 1 hora
            5 => 120,    // 2 horas
            6 => 180,    // 3 horas
            7 => 240,    // 4 horas
            8 => 480,    // 8 horas
            _ => 30      // Padrão: 30 minutos
        };
    }

    /// <summary>
    /// Manipulador para o botão de cancelar temporizador
    /// </summary>
    private void CancelTimer_Click(object sender, RoutedEventArgs e)
    {
        SleepTimerService.Instance.CancelTimer();
    }

    /// <summary>
    /// Manipula o evento de clique nos botões de equalização de cada card
    /// </summary>
    private void BtnEqualizer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string soundId)
        {
            // Navega para a página de equalização/efeitos com o ID do som como parâmetro
            Frame.Navigate(typeof(EqualizadorEfeitosPage), soundId);
        }
    }

    /// <summary>
    /// Manipula o evento de alteração de volume do controle principal
    /// </summary>
    private void Volume_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Slider slider)
        {
            // Ajusta o volume para todos os reprodutores
            foreach (var reprodutor in AudioManager.Instance.GetListReprodutores())
            {
                AudioManager.Instance.AlterarVolume(reprodutor.Key, (float)slider.Value);
            }
        }
    }

    /// <summary>
    /// Manipula o evento de clique no botão de reprodução principal
    /// </summary>
    private void PlayButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try {
            // Verificamos se algum reprodutor está tocando
            bool isPlaying = false;
            
            // Verifica se há reprodutores antes de tentar manipulá-los
            if (AudioManager.Instance.GetQuantidadeReprodutor == 0)
            {
                System.Diagnostics.Debug.WriteLine("Não há reprodutores para controlar");
                return;
            }
            
            foreach (var reprodutor in AudioManager.Instance.GetListReprodutores())
            {
                if (reprodutor.Value.WaveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                {
                    isPlaying = true;
                    break;
                }
            }

            if (isPlaying)
            {
                AudioManager.Instance.PauseAll();
            }
            else
            {
                AudioManager.Instance.PlayAll();
            }
            
            // Atualiza o estado visual do botão
            UpdatePlayButtonState(!isPlaying);  // Invertemos o estado atual
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao manipular botão de reprodução: {ex.Message}");
        }
    }

    /// <summary>
    /// Atualiza o estado visual do botão de reprodução
    /// </summary>
    private void UpdatePlayButtonState(bool isPlaying)
    {
        if (PlayButton is Button playButton)
        {
            // Se estiver usando um ícone ou símbolo para o botão de play/pause
            if (playButton.Content is SymbolIcon symbolIcon)
            {
                symbolIcon.Symbol = isPlaying ? Symbol.Pause : Symbol.Play;
            }
            // Se estiver usando texto
            else if (playButton.Content is string)
            {
                playButton.Content = isPlaying ? "Pausar" : "Reproduzir";
            }
            // Se estiver usando FontIcon
            else if (playButton.Content is FontIcon fontIcon)
            {
                fontIcon.Glyph = isPlaying ? "\uE769" : "\uE768"; // Unicode para Pause/Play no Segoe MDL2 Assets
            }
        }
    }
}

