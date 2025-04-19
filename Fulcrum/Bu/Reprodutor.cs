using Microsoft.UI.Xaml.Shapes;
using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Classe base para todos os reprodutores de som
/// </summary>
public abstract class Reprodutor
{
    public WaveOutEvent WaveOut { get; protected set; } = null!;
    public AudioFileReader Reader { get; protected set; } = null!;
    public DelayFadeOutSampleProvider FadeOutProvider { get; protected set; } = null!;
    public AudioVisualizer Visualizer { get; protected set; } = null!;
    
    /// <summary>
    /// Inicializa um novo reprodutor de som
    /// </summary>
    /// <param name="soundPath">Caminho relativo para o arquivo de áudio</param>
    /// <param name="initialVolume">Volume inicial (0.0 a 1.0)</param>
    protected void Initialize(string soundPath, float initialVolume = 0.0f)
    {
        Reader = new AudioFileReader(GetSoundPath(soundPath));
        Reader.Volume = initialVolume;
        
        FadeOutProvider = new DelayFadeOutSampleProvider(Reader);
        
        WaveOut = new WaveOutEvent();
        WaveOut.Init(FadeOutProvider);
        WaveOut.Play();
        
        WaveOut.PlaybackStopped += (s, e) =>
        {
            Reader.Position = 0; // Reinicia o áudio do início
            WaveOut.Play(); // Começa a tocar novamente
        };
    }
    
    /// <summary>
    /// Configura o visualizador de áudio para este reprodutor
    /// </summary>
    /// <param name="waveformElement">Elemento polyline do XAML para exibir a forma de onda</param>
    /// <param name="sampleCount">Número de amostras a serem analisadas</param>
    public void ConfigureVisualizer(Polyline waveformElement, int sampleCount = 100)
    {
        if (FadeOutProvider != null)
        {
            Visualizer = new AudioVisualizer(FadeOutProvider, waveformElement, sampleCount);
            Visualizer.SetThickness(2);
            Visualizer.Start();
        }
    }
    
    /// <summary>
    /// Inicia a visualização da forma de onda
    /// </summary>
    public void StartVisualization()
    {
        Visualizer?.Start();
    }
    
    /// <summary>
    /// Para a visualização da forma de onda
    /// </summary>
    public void StopVisualization()
    {
        Visualizer?.Stop();
    }
    
    /// <summary>
    /// Inicia a reprodução do áudio
    /// </summary>
    public void Play() 
    { 
        WaveOut.Play();
        StartVisualization();
    }
    
    /// <summary>
    /// Pausa a reprodução do áudio
    /// </summary>
    public void Pause() 
    { 
        WaveOut.Pause();
        StopVisualization();
    }
    
    /// <summary>
    /// Altera o volume do áudio
    /// </summary>
    /// <param name="volume">Volume (0.0 a 1.0)</param>
    public void AlterarVolume(double volume) => Reader.Volume = (float)volume;

    /// <summary>
    /// Obtém o caminho completo para um arquivo de áudio
    /// </summary>
    /// <param name="relativePath">Caminho relativo para o arquivo</param>
    /// <returns>Caminho completo para o arquivo</returns>
    private string GetSoundPath(string relativePath)
    {
        var executableLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var executableDirectory = System.IO.Path.GetDirectoryName(executableLocation) 
            ?? throw new InvalidOperationException("Não foi possível determinar o diretório do executável");
        
        return System.IO.Path.Combine(executableDirectory, relativePath);
    }
}
