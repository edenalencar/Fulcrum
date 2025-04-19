using Fulcrum.Bu;
using Fulcrum.Util;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Fulcrum.View;

/// <summary>
/// Página principal que exibe os controles de som ambiente
/// </summary>
public sealed partial class HomePage : Page
{
    private bool _isInitialized = false;

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
            // Configura cada visualizador com uma cor diferente baseada no tipo de som
            ConfigureVisualizer(Constantes.Sons.Chuva, chuvaWaveform, Colors.DeepSkyBlue);
            ConfigureVisualizer(Constantes.Sons.Fogueira, fogueiraWaveform, Colors.OrangeRed);
            ConfigureVisualizer(Constantes.Sons.Ondas, ondasWaveform, Colors.LightSeaGreen);
            ConfigureVisualizer(Constantes.Sons.Passaros, passarosWaveform, Colors.YellowGreen);
            ConfigureVisualizer(Constantes.Sons.Praia, praiaWaveform, Colors.Gold);
            ConfigureVisualizer(Constantes.Sons.Trem, tremWaveform, Colors.Gray);
            ConfigureVisualizer(Constantes.Sons.Ventos, ventosWaveform, Colors.LightBlue);
            ConfigureVisualizer(Constantes.Sons.Cafeteria, cafeteriaWaveform, Colors.SandyBrown);
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
            var reprodutor = AudioManager.Instance.GetReprodutorPorId(soundId);
            reprodutor.ConfigureVisualizer(waveformElement);
            
            // Define a cor da forma de onda
            waveformElement.Stroke = new SolidColorBrush(color);
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
        // Inicializa e registra os reprodutores
        try
        {
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Chuva, new ReprodutorChuva());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Fogueira, new ReprodutorFogueira());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Lancha, new ReprodutorLancha());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Ondas, new ReprodutorOndas());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Passaros, new ReprodutorPassaros());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Praia, new ReprodutorPraia());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Trem, new ReprodutorTrem());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Ventos, new ReprodutorVentos());
            AudioManager.Instance.AddAudioPlayer(Constantes.Sons.Cafeteria, new ReprodutorCafeteria());
        }
        catch (Exception ex)
        {
            // Log de erro ou exibir mensagem para o usuário
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar reprodutores: {ex.Message}");
        }
    }

    /// <summary>
    /// Sincroniza os valores dos sliders com os valores atuais dos reprodutores
    /// </summary>
    private void SyncSliderValues()
    {
        try
        {
            // Sincroniza cada slider com o volume atual do reprodutor correspondente
            SyncSliderValue(Constantes.Sons.Chuva, FindName("chuva") as Slider);
            SyncSliderValue(Constantes.Sons.Fogueira, FindName("fogueira") as Slider);
            SyncSliderValue(Constantes.Sons.Lancha, FindName("lancha") as Slider);
            SyncSliderValue(Constantes.Sons.Ondas, FindName("ondas") as Slider);
            SyncSliderValue(Constantes.Sons.Passaros, FindName("passaros") as Slider);
            SyncSliderValue(Constantes.Sons.Praia, FindName("praia") as Slider);
            SyncSliderValue(Constantes.Sons.Trem, FindName("trem") as Slider);
            SyncSliderValue(Constantes.Sons.Ventos, FindName("ventos") as Slider);
            SyncSliderValue(Constantes.Sons.Cafeteria, FindName("cafeteria") as Slider);
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
            slider.Value = reprodutor.Reader.Volume;
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
            // Atualiza o volume do reprodutor correspondente
            AudioManager.Instance.AlterarVolume(soundId, e.NewValue);
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
}

