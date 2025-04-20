using Microsoft.UI.Xaml.Shapes;
using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Classe base para todos os reprodutores de som
/// </summary>
public abstract class Reprodutor : IDisposable
{
    public WaveOutEvent WaveOut { get; protected set; } = null!;
    public AudioFileReader Reader { get; protected set; } = null!;
    public DelayFadeOutSampleProvider FadeOutProvider { get; protected set; } = null!;
    public AudioVisualizer Visualizer { get; protected set; } = null!;
    
    // Novos componentes para equalização e efeitos
    public EqualizadorAudio Equalizer { get; protected set; } = null!;
    public GerenciadorEfeitos EffectsManager { get; protected set; } = null!;
    
    // Bandas de equalização padrão (baixo, médio, agudo)
    private readonly EqualizerBand[] _defaultBands = new EqualizerBand[]
    {
        new EqualizerBand(100, 1.0f, 0, "Baixa"),
        new EqualizerBand(1000, 1.0f, 0, "Média"),
        new EqualizerBand(8000, 1.0f, 0, "Alta")
    };
    
    // Flag para controlar se os efeitos e equalização estão ativos
    private bool _equalizerEnabled = false;
    private bool _effectsEnabled = false;
    
    /// <summary>
    /// Inicializa um novo reprodutor de som
    /// </summary>
    /// <param name="soundPath">Caminho relativo para o arquivo de áudio</param>
    /// <param name="initialVolume">Volume inicial (0.0 a 1.0)</param>
    protected void Initialize(string soundPath, float initialVolume = 0.0f)
    {
        try
        {
            string fullPath = GetSoundPath(soundPath);
            System.Diagnostics.Debug.WriteLine($"Carregando arquivo de áudio: {fullPath}");
            
            Reader = new AudioFileReader(fullPath);
            System.Diagnostics.Debug.WriteLine($"Formato do áudio: {Reader.WaveFormat}, Length: {Reader.Length} bytes");
            
            // Define o volume inicial para evitar ruídos
            Reader.Volume = initialVolume > 0.001f ? initialVolume : 0.0f;
            
            // Abordagem simplificada de inicialização - conecta diretamente ao reader
            WaveOut = new WaveOutEvent();
            WaveOut.DeviceNumber = -1; // Usar o dispositivo padrão
            WaveOut.DesiredLatency = 100; // Valor em milissegundos que pode ajudar com a reprodução
            WaveOut.Init(Reader);
            System.Diagnostics.Debug.WriteLine("WaveOut inicializado diretamente com o Reader");
            
            // Inicializa o equalizador com configurações padrão separadamente
            Equalizer = new EqualizadorAudio(Reader, _defaultBands);
            
            // Cria um gerenciador de efeitos separado
            EffectsManager = new GerenciadorEfeitos(Reader);
            
            // Configura o FadeOutProvider para usar com visualização
            FadeOutProvider = new DelayFadeOutSampleProvider(Reader);
            
            // Verifica se deve iniciar a reprodução ou não com base no volume
            if (initialVolume > 0.001f)
            {
                WaveOut.Play();
                System.Diagnostics.Debug.WriteLine($"Reprodução iniciada com volume {initialVolume}");
            }
            else
            {
                // NÃO inicia reprodução se volume for zero
                WaveOut.Stop();
            }
            
            // Configura o evento de reposicionamento
            WaveOut.PlaybackStopped += (s, e) =>
            {
                try
                {
                    // Só reinicia a reprodução se o volume não for zero
                    if (Reader.Volume > 0.001f)
                    {
                        Reader.Position = 0; // Reinicia o áudio do início
                        WaveOut.Play();      // Começa a tocar novamente
                        System.Diagnostics.Debug.WriteLine("Reiniciando reprodução após parada");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao reiniciar após parada: {ex.Message}");
                }
            };
            
            System.Diagnostics.Debug.WriteLine($"Reprodutor inicializado com sucesso. Volume: {initialVolume}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERRO na inicialização do reprodutor: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Re-lança a exceção para tratamento superior
        }
    }
    
    /// <summary>
    /// Configura o visualizador de áudio para este reprodutor
    /// </summary>
    /// <param name="waveformElement">Elemento polyline do XAML para exibir a forma de onda</param>
    /// <param name="sampleCount">Número de amostras a serem analisadas</param>
    public void ConfigureVisualizer(Polyline waveformElement, int sampleCount = 100)
    {
        if (Reader != null)
        {
            Visualizer = new AudioVisualizer(Reader, waveformElement, sampleCount);
            Visualizer.SetThickness(2);
            
            // Inicia a visualização somente se o reprodutor estiver tocando
            if (WaveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                Visualizer.Start();
                System.Diagnostics.Debug.WriteLine("Visualizador iniciado para reprodutor ativo");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Visualizador configurado, mas não iniciado (reprodutor não está tocando)");
            }
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
        // Verifica se o volume é zero - não reproduz ou pausa se for zero
        if (Reader.Volume <= 0.001f)
        {
            Pause();
            return;
        }
        
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
    /// Para completamente a reprodução (não apenas pausa)
    /// </summary>
    public void Stop()
    {
        WaveOut.Stop();
        StopVisualization();
    }
    
    /// <summary>
    /// Altera o volume do áudio
    /// </summary>
    /// <param name="volume">Volume (0.0 a 1.0)</param>
    public void AlterarVolume(double volume)
    {
        float newVolume = (float)Math.Clamp(volume, 0.0, 1.0);
        Reader.Volume = newVolume;
        
        System.Diagnostics.Debug.WriteLine($"Volume alterado para: {newVolume}, Estado atual: {WaveOut.PlaybackState}");
        
        // Automáticamente pausa a reprodução se o volume for zero
        if (newVolume <= 0.001f && WaveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
        {
            System.Diagnostics.Debug.WriteLine("Volume zero - pausando reprodução");
            Pause();
        }
        // Inicia reprodução se estiver parado e volume for acima de zero
        else if (newVolume > 0.001f)
        {
            // Verifica se a reprodução não está ativa
            if (WaveOut.PlaybackState != NAudio.Wave.PlaybackState.Playing)
            {
                System.Diagnostics.Debug.WriteLine($"Iniciando reprodução com volume {newVolume}");
                // Reinicia a posição para garantir a reprodução desde o início
                Reader.Position = 0;
                WaveOut.Play();
                StartVisualization();
            }
        }
    }
    
    /// <summary>
    /// Obtém o caminho completo para um arquivo de áudio
    /// </summary>
    /// <param name="relativePath">Caminho relativo para o arquivo</param>
    /// <returns>Caminho completo para o arquivo</returns>
    private string GetSoundPath(string relativePath)
    {
        try
        {
            // Tenta obter o diretório do executável
            var executableLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableDirectory = System.IO.Path.GetDirectoryName(executableLocation);
            
            if (string.IsNullOrEmpty(executableDirectory))
            {
                throw new InvalidOperationException("Não foi possível determinar o diretório do executável");
            }
            
            // Construir o caminho completo
            var fullPath = System.IO.Path.Combine(executableDirectory, relativePath);
            
            // Verificar se o arquivo existe
            if (!System.IO.File.Exists(fullPath))
            {
                // Tentar caminhos alternativos - o arquivo pode estar em um diretório superior na hierarquia
                var projectDirectory = GetProjectRootDirectory(executableDirectory);
                fullPath = System.IO.Path.Combine(projectDirectory, relativePath);
                
                if (!System.IO.File.Exists(fullPath))
                {
                    // Tentar com o caminho relativo diretamente do AppContext.BaseDirectory
                    var baseDirectory = AppContext.BaseDirectory;
                    fullPath = System.IO.Path.Combine(baseDirectory, relativePath);
                    
                    if (!System.IO.File.Exists(fullPath))
                    {
                        // Tentar encontrar o arquivo em diretórios acima
                        var dir = new System.IO.DirectoryInfo(baseDirectory);
                        while (dir != null && !System.IO.File.Exists(fullPath))
                        {
                            dir = dir.Parent;
                            if (dir != null)
                            {
                                fullPath = System.IO.Path.Combine(dir.FullName, relativePath);
                            }
                        }
                        
                        // Se ainda não encontrou, tenta com o caminho do pacote para aplicativos UWP
                        if (!System.IO.File.Exists(fullPath))
                        {
                            // Para aplicações UWP/WinUI o caminho pode incluir AppX no meio
                            var appxPath = System.IO.Path.Combine(baseDirectory, "..\\..\\", relativePath);
                            fullPath = System.IO.Path.GetFullPath(appxPath);
                        }
                    }
                }
            }
            
            // Registra o caminho encontrado para diagnóstico
            System.Diagnostics.Debug.WriteLine($"Caminho do arquivo de som: {fullPath}");
            
            // Verificação final
            if (!System.IO.File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine($"AVISO: Arquivo não encontrado em {fullPath}");
                throw new System.IO.FileNotFoundException($"Arquivo de som não encontrado: {relativePath}", relativePath);
            }
            
            return fullPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao resolver caminho do arquivo de som: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Tenta encontrar o diretório raiz do projeto subindo na hierarquia
    /// </summary>
    private string GetProjectRootDirectory(string startDirectory)
    {
        var dir = new System.IO.DirectoryInfo(startDirectory);
        
        // Sobe até 5 níveis na hierarquia de diretórios procurando a pasta Assets
        int maxLevels = 5;
        int currentLevel = 0;
        
        while (dir != null && currentLevel < maxLevels)
        {
            // Verifica se este diretório contém a pasta Assets
            if (System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "Assets")))
            {
                return dir.FullName;
            }
            
            dir = dir.Parent;
            currentLevel++;
        }
        
        // Se não encontrou, retorna o diretório original
        return startDirectory;
    }
    
    /// <summary>
    /// Ativa ou desativa o equalizador
    /// </summary>
    public bool EqualizerEnabled
    {
        get => _equalizerEnabled;
        set
        {
            _equalizerEnabled = value;
            if (!value)
            {
                // Resetar todas as bandas para ganho 0
                foreach (var band in Equalizer.Bands)
                {
                    band.Gain = 0;
                }
                Equalizer.UpdateAllBands();
            }
        }
    }
    
    /// <summary>
    /// Ativa ou desativa os efeitos
    /// </summary>
    public bool EffectsEnabled
    {
        get => _effectsEnabled;
        set
        {
            _effectsEnabled = value;
            if (!value)
            {
                // Desativar efeitos
                EffectsManager.TipoEfeito = TipoEfeito.Nenhum;
            }
        }
    }
    
    /// <summary>
    /// Ajusta o ganho de uma banda específica do equalizador
    /// </summary>
    /// <param name="bandIndex">Índice da banda (0=baixos, 1=médios, 2=agudos)</param>
    /// <param name="gain">Ganho em dB (-15 a +15)</param>
    public void AjustarBandaEqualizador(int bandIndex, float gain)
    {
        if (bandIndex >= 0 && bandIndex < Equalizer.Bands.Length)
        {
            Equalizer.Bands[bandIndex].Gain = gain;
            Equalizer.UpdateBand(bandIndex);
        }
    }
    
    /// <summary>
    /// Define o tipo de efeito a ser aplicado
    /// </summary>
    /// <param name="tipoEfeito">Tipo de efeito</param>
    public virtual void DefinirTipoEfeito(TipoEfeito tipoEfeito)
    {
        if (EffectsEnabled)
        {
            EffectsManager.TipoEfeito = tipoEfeito;
        }
    }
    
    /// <summary>
    /// Ajusta o parâmetro de reverberação (mix)
    /// </summary>
    /// <param name="mix">Valor do mix (0.0 a 1.0)</param>
    public void AjustarReverbMix(float mix)
    {
        EffectsManager.ReverbMix = mix;
    }
    
    /// <summary>
    /// Ajusta o parâmetro de tempo de reverberação
    /// </summary>
    /// <param name="time">Tempo de reverberação (0.1 a 10.0 segundos)</param>
    public void AjustarReverbTime(float time)
    {
        EffectsManager.ReverbTime = time;
    }
    
    /// <summary>
    /// Ajusta o fator de alteração de tom (pitch)
    /// </summary>
    /// <param name="factor">Fator de pitch (0.5 a 2.0)</param>
    public void AjustarPitchFactor(float factor)
    {
        EffectsManager.PitchFactor = factor;
    }
    
    /// <summary>
    /// Ajusta os parâmetros de eco
    /// </summary>
    /// <param name="delay">Tempo de atraso em ms (10 a 1000)</param>
    /// <param name="mix">Mix do eco (0.0 a 1.0)</param>
    public void AjustarEcho(float delay, float mix)
    {
        EffectsManager.EchoDelay = delay;
        EffectsManager.EchoMix = mix;
    }
    
    /// <summary>
    /// Ajusta os parâmetros de flanger
    /// </summary>
    /// <param name="rate">Taxa em Hz (0.1 a 5.0)</param>
    /// <param name="depth">Profundidade (0.001 a 0.01)</param>
    public void AjustarFlanger(float rate, float depth)
    {
        EffectsManager.FlangerRate = rate;
        EffectsManager.FlangerDepth = depth;
    }
    
    /// <summary>
    /// Libera recursos gerenciados e não-gerenciados utilizados pelo reprodutor
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Para a visualização se estiver em execução
            StopVisualization();
            
            // Para a reprodução
            if (WaveOut != null)
            {
                WaveOut.Stop();
                WaveOut.Dispose();
            }
            
            // Libera o leitor de áudio
            if (Reader != null)
            {
                Reader.Dispose();
            }
            
            // Libera recursos do equalizador
            if (Equalizer != null)
            {
                Equalizer.Dispose();
            }
            
            // Libera recursos do gerenciador de efeitos
            EffectsManager?.Dispose();
            
            // Libera o visualizador
            Visualizer?.Dispose();
            
            System.Diagnostics.Debug.WriteLine("Recursos do reprodutor liberados com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao liberar recursos do reprodutor: {ex.Message}");
        }
    }
}
