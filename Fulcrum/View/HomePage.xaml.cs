using Fulcrum.Bu;
using Fulcrum.Util;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using NAudio.Wave;
using System;
using Windows.UI;

namespace Fulcrum.View;

/// <summary>
/// Página principal que exibe os controles de som ambiente
/// </summary>
public sealed partial class HomePage : Page
{
    private AudioManager _audioManager;
    private bool _isInitialized = false;
    private bool _isPageActive = false;
    private bool _isUpdatingSliders = false;
    private DispatcherTimer _sleepTimerDisplayUpdateTimer;

    /// <summary>
    /// Inicializa uma nova instância da classe HomePage
    /// </summary>
    public HomePage()
    {
        this.InitializeComponent();
        
        // Inicializa gerenciador de áudio
        _audioManager = AudioManager.Instance;

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
        _isPageActive = true;

        // Aplica animações iniciais aos cards
        ApplyEntryAnimations();
        
        if (!_isInitialized)
        {
            // Configura os sliders para exibir o valor atual
            ConfigureSliders();
            
            // Inicializa os visualizadores de forma de onda
            ConfigureWaveformVisualizers();
            
            // Verifica se já existem reprodutores antes de inicializar novamente
            if (AudioManager.Instance.GetQuantidadeReprodutor == 0)
            {
                // Só inicializa os reprodutores se não existirem
                InitializeAudioPlayers();
                _isInitialized = true;
            }
            
            System.Diagnostics.Debug.WriteLine("HomePage: Primeira inicialização concluída");
        }
        else
        {
            // Apenas atualiza os sliders com os valores atuais sem alterar os volumes
            UpdateSlidersWithCurrentVolumes();
            System.Diagnostics.Debug.WriteLine("HomePage: Restaurando estado anterior");
        }
        
        // Restaura estado dos áudios, garantindo que reprodutores com volume > 0 estejam tocando
        RestoreAudioStates();
        
        // Sincroniza os valores dos sliders com os valores dos reprodutores
        SyncSliderValues();
        
        // Atualiza o estado visual do botão de reprodução principal
        UpdatePlayButtonState(AudioManager.Instance.IsPlaying);
    }
    
    /// <summary>
    /// Manipula o evento de página descarregada
    /// </summary>
    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _isPageActive = false;

        // Salva o estado dos volumes antes de navegar para outra página
        AudioManager.Instance.SalvarEstadoVolumes();
        AudioManager.Instance.SalvarEstadoEfeitos();

        // Importante: não parar reprodução quando sair desta página
        System.Diagnostics.Debug.WriteLine("HomePage: Descarregada, estado de reprodução preservado");
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
            
            // Verifica que todos os reprodutores tenham volume zero inicialmente
            foreach (var player in AudioManager.Instance.GetListReprodutores())
            {
                if (player.Value.Reader.Volume > 0.001f)
                {
                    System.Diagnostics.Debug.WriteLine($"Forçando volume zero para o reprodutor {player.Key}");
                    AudioManager.Instance.AlterarVolume(player.Key, 0.0f);
                }
            }
            
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
    /// Configura os sliders para ajuste de volume
    /// </summary>
    private void ConfigureSliders()
    {
        // Desconectar temporariamente os eventos para evitar alterações indesejadas durante a configuração
        chuva.ValueChanged -= Slider_ValueChanged;
        fogueira.ValueChanged -= Slider_ValueChanged;
        lancha.ValueChanged -= Slider_ValueChanged;
        ondas.ValueChanged -= Slider_ValueChanged;
        passaros.ValueChanged -= Slider_ValueChanged;
        praia.ValueChanged -= Slider_ValueChanged;
        trem.ValueChanged -= Slider_ValueChanged;
        ventos.ValueChanged -= Slider_ValueChanged;
        cafeteria.ValueChanged -= Slider_ValueChanged;

        // Definir valores iniciais para os sliders (todos começam em zero)
        chuva.Minimum = 0.0;
        chuva.Maximum = 1.0;
        chuva.Value = 0.0;
        
        fogueira.Minimum = 0.0;
        fogueira.Maximum = 1.0;
        fogueira.Value = 0.0;
        
        lancha.Minimum = 0.0;
        lancha.Maximum = 1.0;
        lancha.Value = 0.0;
        
        ondas.Minimum = 0.0;
        ondas.Maximum = 1.0;
        ondas.Value = 0.0;
        
        passaros.Minimum = 0.0;
        passaros.Maximum = 1.0;
        passaros.Value = 0.0;
        
        praia.Minimum = 0.0;
        praia.Maximum = 1.0;
        praia.Value = 0.0;
        
        trem.Minimum = 0.0;
        trem.Maximum = 1.0;
        trem.Value = 0.0;
        
        ventos.Minimum = 0.0;
        ventos.Maximum = 1.0;
        ventos.Value = 0.0;
        
        cafeteria.Minimum = 0.0;
        cafeteria.Maximum = 1.0;
        cafeteria.Value = 0.0;

        // Reconectar os eventos após configuração
        chuva.ValueChanged += Slider_ValueChanged;
        fogueira.ValueChanged += Slider_ValueChanged;
        lancha.ValueChanged += Slider_ValueChanged;
        ondas.ValueChanged += Slider_ValueChanged;
        passaros.ValueChanged += Slider_ValueChanged;
        praia.ValueChanged += Slider_ValueChanged;
        trem.ValueChanged += Slider_ValueChanged;
        ventos.ValueChanged += Slider_ValueChanged;
        cafeteria.ValueChanged += Slider_ValueChanged;
        
        System.Diagnostics.Debug.WriteLine("Sliders configurados com valores iniciais zero");
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
    /// Atualiza os sliders com os valores atuais dos reprodutores sem alterar os volumes
    /// </summary>
    private void UpdateSlidersWithCurrentVolumes()
    {
        // Ativa a flag para indicar que estamos fazendo uma atualização em massa dos sliders
        _isUpdatingSliders = true;
        
        try
        {
            // Desconecta temporariamente os eventos de alteração de valor
            chuva.ValueChanged -= Slider_ValueChanged;
            fogueira.ValueChanged -= Slider_ValueChanged;
            lancha.ValueChanged -= Slider_ValueChanged;
            ondas.ValueChanged -= Slider_ValueChanged;
            passaros.ValueChanged -= Slider_ValueChanged;
            praia.ValueChanged -= Slider_ValueChanged;
            trem.ValueChanged -= Slider_ValueChanged;
            ventos.ValueChanged -= Slider_ValueChanged;
            cafeteria.ValueChanged -= Slider_ValueChanged;
            VolumeSlider.ValueChanged -= Volume_ValueChanged;

            // Resetar o slider principal para zero inicialmente
            VolumeSlider.Value = 0.0;

            // Atualiza os sliders com os valores atuais dos reprodutores
            var players = _audioManager.GetListReprodutores();

            if (players.TryGetValue(Constantes.Sons.Chuva, out var chuvaPlayer))
                chuva.Value = chuvaPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Fogueira, out var fogueiraPlayer))
                fogueira.Value = fogueiraPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Lancha, out var lanchaPlayer))
                lancha.Value = lanchaPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Ondas, out var ondasPlayer))
                ondas.Value = ondasPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Passaros, out var passarosPlayer))
                passaros.Value = passarosPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Praia, out var praiaPlayer))
                praia.Value = praiaPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Trem, out var tremPlayer))
                trem.Value = tremPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Ventos, out var ventosPlayer))
                ventos.Value = ventosPlayer.Reader.Volume;

            if (players.TryGetValue(Constantes.Sons.Cafeteria, out var cafeteriaPlayer))
                cafeteria.Value = cafeteriaPlayer.Reader.Volume;

            // Reconecta os eventos de alteração de valor
            chuva.ValueChanged += Slider_ValueChanged;
            fogueira.ValueChanged += Slider_ValueChanged;
            lancha.ValueChanged += Slider_ValueChanged;
            ondas.ValueChanged += Slider_ValueChanged;
            passaros.ValueChanged += Slider_ValueChanged;
            praia.ValueChanged += Slider_ValueChanged;
            trem.ValueChanged += Slider_ValueChanged;
            ventos.ValueChanged += Slider_ValueChanged;
            cafeteria.ValueChanged += Slider_ValueChanged;
            VolumeSlider.ValueChanged += Volume_ValueChanged;
            
            System.Diagnostics.Debug.WriteLine("Sliders atualizados com os valores atuais dos reprodutores");
        }
        finally
        {
            // Garante que a flag seja desativada mesmo se ocorrer uma exceção
            _isUpdatingSliders = false;
        }
    }

    /// <summary>
    /// Restaura o estado dos áudios garantindo que reprodutores com volume > 0 estejam tocando
    /// </summary>
    private void RestoreAudioStates()
    {
        var players = _audioManager.GetListReprodutores();
        foreach (var player in players)
        {
            if (player.Value.Reader.Volume > 0.001f)
            {
                // Se o volume for maior que zero, garante que esteja tocando
                if (player.Value.WaveOut?.PlaybackState != NAudio.Wave.PlaybackState.Playing)
                {
                    _audioManager.Play(player.Key);
                    System.Diagnostics.Debug.WriteLine($"Retomando reprodução de {player.Key} (volume: {player.Value.Reader.Volume})");
                }
            }
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
            // Obtém o reprodutor correspondente
            var reprodutor = AudioManager.Instance.GetReprodutorPorId(soundId);
            if (reprodutor == null) return;
            
            // Verifica se o volume era zero antes da mudança
            bool eraZero = reprodutor.Reader.Volume <= 0.001f;
            
            // Atualiza o volume do reprodutor
            AudioManager.Instance.AlterarVolume(soundId, (float)e.NewValue);
            System.Diagnostics.Debug.WriteLine($"Volume alterado para {soundId}: {e.NewValue:F2}");
            
            // Se o volume mudou de zero para maior que zero, garante que a visualização seja iniciada
            if (eraZero && e.NewValue > 0.001f)
            {
                // Forçar configuração e inicialização da visualização
                ConfigurarEIniciarVisualizacao(soundId);
                System.Diagnostics.Debug.WriteLine($"Visualização forçada para {soundId} após aumento de volume");
            }
            
            // Anunciar a mudança de volume para leitores de tela
            string nomeSomAcessivel = ObterNomeSomAmigavel(soundId);
            bool ativo = e.NewValue > 0.001f;
            AcessibilidadeHelper.AnunciarEstadoSom(slider, nomeSomAcessivel, e.NewValue, ativo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar volume: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Configura e inicia a visualização para um som específico
    /// </summary>
    private void ConfigurarEIniciarVisualizacao(string soundId)
    {
        try
        {
            var reprodutor = AudioManager.Instance.GetReprodutorPorId(soundId);
            if (reprodutor == null) return;
            
            // Obtém a referência ao elemento Polyline correspondente
            var waveformElement = ObterElementoWaveform(soundId);
            if (waveformElement == null) return;
            
            // Configura a visualização com a cor apropriada
            Color cor = ObterCorParaSom(soundId);
            
            // Reconfigura o visualizador
            if (reprodutor.Visualizer != null)
            {
                reprodutor.Visualizer.Stop();
                reprodutor.Visualizer.Dispose();
                reprodutor.Visualizer = null!; // Usando null! para indicar que será inicializado antes do uso
            }
            
            // Cria novo visualizador
            reprodutor.ConfigureVisualizer(waveformElement);
            waveformElement.Stroke = new SolidColorBrush(cor);
            
            // Inicia a visualização explicitamente se o volume for maior que zero
            if (reprodutor.Reader.Volume > 0.001f)
            {
                reprodutor.StartVisualization();
                System.Diagnostics.Debug.WriteLine($"Visualização iniciada para {soundId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar visualização: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Obtém o elemento Polyline correspondente ao ID do som
    /// </summary>
    private Polyline? ObterElementoWaveform(string soundId)
    {
        return soundId switch
        {
            Constantes.Sons.Chuva => chuvaWaveform,
            Constantes.Sons.Fogueira => fogueiraWaveform,
            Constantes.Sons.Lancha => lanchaWaveform,
            Constantes.Sons.Ondas => ondasWaveform,
            Constantes.Sons.Passaros => passarosWaveform,
            Constantes.Sons.Praia => praiaWaveform,
            Constantes.Sons.Trem => tremWaveform,
            Constantes.Sons.Ventos => ventosWaveform,
            Constantes.Sons.Cafeteria => cafeteriaWaveform,
            _ => null
        };
    }
    
    /// <summary>
    /// Obtém a cor associada ao som específico
    /// </summary>
    private Color ObterCorParaSom(string soundId)
    {
        return soundId switch
        {
            Constantes.Sons.Chuva => Colors.DeepSkyBlue,
            Constantes.Sons.Fogueira => Colors.OrangeRed,
            Constantes.Sons.Lancha => Colors.MediumPurple,
            Constantes.Sons.Ondas => Colors.LightSeaGreen,
            Constantes.Sons.Passaros => Colors.YellowGreen,
            Constantes.Sons.Praia => Colors.Gold,
            Constantes.Sons.Trem => Colors.Gray,
            Constantes.Sons.Ventos => Colors.LightBlue,
            Constantes.Sons.Cafeteria => Colors.SandyBrown,
            _ => Colors.Gray
        };
    }

    /// <summary>
    /// Obtém um nome amigável para o som baseado no ID
    /// </summary>
    private string ObterNomeSomAmigavel(string soundId)
    {
        return soundId switch
        {
            Constantes.Sons.Chuva => "Chuva",
            Constantes.Sons.Fogueira => "Fogueira",
            Constantes.Sons.Lancha => "Lancha",
            Constantes.Sons.Ondas => "Ondas",
            Constantes.Sons.Passaros => "Pássaros",
            Constantes.Sons.Praia => "Praia",
            Constantes.Sons.Trem => "Trem",
            Constantes.Sons.Ventos => "Ventos",
            Constantes.Sons.Cafeteria => "Cafeteria",
            _ => soundId
        };
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

        // Exibe notificação dentro do aplicativo (InfoBar)
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
                
                // Usar LocalizationHelper em vez de ResourceLoader direto
                try
                {
                    SetSleepTimerButton.Content = LocalizationHelper.GetString("ChangeTimerText", "Alterar Timer");
                }
                catch (Exception ex)
                {
                    // Em caso de falha, usa texto padrão em português
                    SetSleepTimerButton.Content = "Alterar Timer";
                    System.Diagnostics.Debug.WriteLine($"Erro ao acessar recurso localizado: {ex.Message}");
                }
                
                cancelTimerButton.Visibility = Visibility.Visible;
            }
            else
            {
                timerContainer.Visibility = Visibility.Collapsed;
                
                // Usar LocalizationHelper em vez de ResourceLoader direto
                try
                {
                    SetSleepTimerButton.Content = LocalizationHelper.GetString("SetTimerTextValue", "Definir Timer");
                }
                catch (Exception ex)
                {
                    // Em caso de falha, usa texto padrão em português
                    SetSleepTimerButton.Content = "Definir Timer";
                    System.Diagnostics.Debug.WriteLine($"Erro ao acessar recurso localizado: {ex.Message}");
                }
                
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

            var textBlock = new TextBlock
            {
                Text = "Escolha quanto tempo o Fulcrum deve reproduzir antes de pausar automaticamente:",
                TextWrapping = TextWrapping.Wrap
            };
            
            // Configurações de acessibilidade para o texto explicativo
            AutomationProperties.SetName(textBlock, "Instruções do temporizador de sono");
            
            panel.Children.Add(textBlock);

            var comboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                SelectedIndex = 2 // 30 minutos como padrão
            };
            
            // Configurações de acessibilidade para o ComboBox
            AutomationProperties.SetName(comboBox, "Selecionar duração do temporizador");
            AutomationProperties.SetHelpText(comboBox, "Escolha por quanto tempo os sons devem ser reproduzidos antes de parar");

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
                    
                    // Anunciar para leitores de tela
                    string duracao = comboBox.SelectedItem.ToString() ?? $"{minutes} minutos";
                    AcessibilidadeHelper.AnunciarParaLeitoresEscreens(
                        SetSleepTimerButton, 
                        $"Temporizador de sono definido para {duracao}. Os sons serão pausados automaticamente após este período.",
                        true);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao definir timer: {ex.Message}");
            
            // Anunciar erro
            AcessibilidadeHelper.AnunciarParaLeitoresEscreens(
                SetSleepTimerButton, 
                "Ocorreu um erro ao definir o temporizador de sono", 
                true);
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
        
        // Anunciar para leitores de tela
        AcessibilidadeHelper.AnunciarParaLeitoresEscreens(cancelTimerButton, 
            "Temporizador de sono cancelado. A reprodução continuará normalmente.", 
            true);
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
        // Verifica se devemos ignorar o evento durante a inicialização da página
        if (!_isPageActive || _isUpdatingSliders)
            return;

        try
        {
            // Ajusta o volume para todos os reprodutores que já têm volume maior que zero
            foreach (var reprodutor in AudioManager.Instance.GetListReprodutores())
            {
                // Altera apenas o volume para os players que já estão tocando ou têm volume > 0
                if (reprodutor.Value.Reader.Volume > 0.001f)
                {
                    AudioManager.Instance.AlterarVolume(reprodutor.Key, (float)e.NewValue);
                    System.Diagnostics.Debug.WriteLine($"Ajustando volume de {reprodutor.Key} para {e.NewValue} via controle principal");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar volume principal: {ex.Message}");
        }
    }

    /// <summary>
    /// Manipula o evento de clique no botão de reprodução principal
    /// </summary>
    private void PlayButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try 
        {
            // Verifica o estado atual da reprodução
            bool estaReproduzindo = AudioManager.Instance.IsPlaying;
            
            if (estaReproduzindo)
            {
                // Se estiver tocando, pausamos todos
                AudioManager.Instance.PauseAll();
                System.Diagnostics.Debug.WriteLine("Pausando todos os sons");
                
                // Anunciar para leitores de tela
                AcessibilidadeHelper.AnunciarParaLeitoresEscreens(PlayButton, "Todos os sons foram pausados", true);
            }
            else
            {
                // Se estiver pausado, verificamos quais players têm volume maior que zero
                var players = AudioManager.Instance.GetListReprodutores();
                bool peloMenosUmPlayerComVolume = false;
                
                foreach (var player in players)
                {
                    if (player.Value.Reader.Volume > 0.001f)
                    {
                        // Reproduz diretamente cada player com volume
                        player.Value.Play();
                        System.Diagnostics.Debug.WriteLine($"Reproduzindo {player.Key} com volume {player.Value.Reader.Volume}");
                        peloMenosUmPlayerComVolume = true;
                    }
                }
                
                if (!peloMenosUmPlayerComVolume)
                {
                    System.Diagnostics.Debug.WriteLine("Nenhum player com volume maior que zero para reproduzir");
                    // Anunciar a ausência de sons ativos
                    AcessibilidadeHelper.AnunciarParaLeitoresEscreens(PlayButton, 
                        "Não há sons com volume para reproduzir. Ajuste o volume de pelo menos um som.", true);
                }
                else
                {
                    // Anunciar início da reprodução
                    AcessibilidadeHelper.AnunciarParaLeitoresEscreens(PlayButton, "Reprodução iniciada", true);
                }
            }
            
            // Atualiza o estado visual do botão após a ação
            UpdatePlayButtonState(!estaReproduzindo);
            
            System.Diagnostics.Debug.WriteLine($"Botão de reprodução clicado - Novo estado: {(!estaReproduzindo ? "Reproduzindo" : "Pausado")}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao manipular botão de reprodução: {ex.Message}");
            
            // Anunciar erro
            AcessibilidadeHelper.AnunciarParaLeitoresEscreens(PlayButton, 
                "Ocorreu um erro ao tentar reproduzir ou pausar os sons", true);
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
            else if (playButton.Content is FontIcon)
            {
                var fontIcon = playButton.Content as FontIcon;
                if (fontIcon != null)
                {
                    fontIcon.Glyph = isPlaying ? "\uE769" : "\uE768"; // Unicode para Pause/Play no Segoe Fluent Icons
                }
            }
        }
    }
}

