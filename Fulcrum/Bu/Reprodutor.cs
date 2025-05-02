using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml;
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
    public AudioVisualizer Visualizer { get; set; } = null!;
    
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

    // Flag para controlar se o áudio deve ser repetido automaticamente
    private bool _shouldLoopPlayback = true; // Alterado para true por padrão
    
    // Número do dispositivo de saída (-1 para dispositivo padrão)
    protected int DeviceOut { get; set; } = -1;
    
    // Fonte atual de áudio ativa na cadeia de processamento
    private ISampleProvider _activeSource = null!;
    
    // Timer para debounce das alterações de equalização
    private DispatcherTimer _equalizerUpdateTimer;
    private bool _pendingEqualizerUpdate = false;
    
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
            
            // Garante que o volume inicial seja exatamente zero
            Reader.Volume = 0.0f;
            
            // Mantém a referência para a fonte ativa na cadeia de processamento
            _activeSource = Reader;
            
            // Inicializa o equalizador com configurações padrão separadamente
            Equalizer = new EqualizadorAudio(Reader, _defaultBands);
            
            // Cria um gerenciador de efeitos separado
            EffectsManager = new GerenciadorEfeitos(Reader);
            
            // Configura o FadeOutProvider para usar com visualização
            FadeOutProvider = new DelayFadeOutSampleProvider(Reader);
            
            // Abordagem simplificada de inicialização - conecta diretamente ao reader
            WaveOut = new WaveOutEvent();
            WaveOut.DeviceNumber = -1; // Usar o dispositivo padrão
            WaveOut.DesiredLatency = 100; // Valor em milissegundos que pode ajudar com a reprodução
            WaveOut.Init(Reader);
            System.Diagnostics.Debug.WriteLine("WaveOut inicializado diretamente com o Reader com volume zero");
            
            // Garante que o reprodutor NÃO inicie automaticamente, independente do volume
            WaveOut.Stop();
            System.Diagnostics.Debug.WriteLine($"Inicialização completa. Reprodutor parado com volume 0.0");
            
            // Ativa a reprodução em loop
            _shouldLoopPlayback = true;
            
            // Configura o evento de reposicionamento
            WaveOut.PlaybackStopped += OnPlaybackStopped;
            
            System.Diagnostics.Debug.WriteLine($"Reprodutor inicializado com sucesso. Volume: {Reader.Volume}");
            
            // Inicializa o timer de debounce para atualizações do equalizador
            _equalizerUpdateTimer = new DispatcherTimer();
            _equalizerUpdateTimer.Interval = TimeSpan.FromMilliseconds(250); // Atualiza a cada 250ms durante deslizamento
            _equalizerUpdateTimer.Tick += (s, e) => {
                if (_pendingEqualizerUpdate)
                {
                    UpdateProcessingChain();
                    _pendingEqualizerUpdate = false;
                    System.Diagnostics.Debug.WriteLine("[EQUALIZER] Debounce timer executado: cadeia de áudio atualizada");
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERRO na inicialização do reprodutor: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Re-lança a exceção para tratamento superior
        }
    }
    
    /// <summary>
    /// Manipulador do evento PlaybackStopped para implementar o loop
    /// </summary>
    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        try
        {
            // Verifica se os objetos ainda são válidos
            if (Reader == null || WaveOut == null)
            {
                return;
            }
            
            // Se o áudio foi apenas parado e não pausado, reinicia do início
            if (_shouldLoopPlayback && Reader.Volume > 0.001f)
            {
                System.Diagnostics.Debug.WriteLine("Loop: Reiniciando áudio do início");
                Reader.Position = 0; // Reinicia o áudio do início
                WaveOut.Play();      // Começa a tocar novamente
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao processar loop de áudio: {ex.Message}");
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
        try
        {
            // Verifica se já existe um visualizador configurado
            if (Visualizer != null)
            {
                Visualizer.Start();
                System.Diagnostics.Debug.WriteLine("Visualização de onda iniciada com sucesso");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Visualizador não está configurado, não é possível iniciar visualização");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao iniciar visualização: {ex.Message}");
        }
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
        
        // Automáticamente inicia a reprodução se o volume for maior que zero e o áudio estiver parado
        if (newVolume > 0.001f && WaveOut.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
        {
            Play();
            System.Diagnostics.Debug.WriteLine($"Reprodução iniciada automaticamente ao ajustar volume para {newVolume}");
        }
        // Automáticamente pausa a reprodução se o volume for zero
        else if (newVolume <= 0.001f && WaveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
        {
            Pause();
            System.Diagnostics.Debug.WriteLine($"Reprodução pausada automaticamente ao ajustar volume para zero");
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
            bool oldValue = _equalizerEnabled;
            _equalizerEnabled = value;
            
            System.Diagnostics.Debug.WriteLine($"[EQUALIZER] Estado alterado: {oldValue} -> {value}");
            
            if (oldValue != value)
            {
                if (!value)
                {
                    // Resetar todas as bandas para ganho 0
                    System.Diagnostics.Debug.WriteLine("[EQUALIZER] Resetando bandas para ganho 0");
                    foreach (var band in Equalizer.Bands)
                    {
                        band.Gain = 0;
                    }
                }
                
                // Garantir que a atualização seja aplicada mesmo se o valor não mudar
                Equalizer.UpdateAllBands();
                
                // Reconectar a cadeia de processamento de áudio
                UpdateProcessingChain();
                
                // Verificar se o efeito está adicionado à cadeia
                System.Diagnostics.Debug.WriteLine($"[EQUALIZER] Cadeia de processamento atualizada. Equalizador ativo: {_equalizerEnabled}");
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
            bool oldValue = _effectsEnabled;
            _effectsEnabled = value;
            
            System.Diagnostics.Debug.WriteLine($"[EFFECTS] Estado alterado: {oldValue} -> {value}");
            
            if (oldValue != value)
            {
                if (!value)
                {
                    // Desativar efeitos
                    EffectsManager.TipoEfeito = TipoEfeito.Nenhum;
                }
                
                // Reconectar a cadeia de processamento de áudio
                UpdateProcessingChain();
                
                System.Diagnostics.Debug.WriteLine($"[EFFECTS] Cadeia de processamento atualizada. Efeitos ativos: {_effectsEnabled}");
            }
        }
    }
    
    /// <summary>
    /// Atualiza a cadeia de processamento de áudio
    /// </summary>
    private void UpdateProcessingChain()
    {
        try
        {
            // Pausar temporariamente a reprodução se estiver ativa
            bool wasPlaying = WaveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing;
            NAudio.Wave.PlaybackState originalState = WaveOut?.PlaybackState ?? NAudio.Wave.PlaybackState.Stopped;
            
            // Salvar a posição atual
            long currentPosition = 0;
            if (Reader != null)
            {
                currentPosition = Reader.Position;
            }
            
            // Usar uma abordagem mais suave - pausa temporariamente sem parar completamente
            if (wasPlaying)
            {
                WaveOut.Pause();
            }
            
            // Definir a fonte base
            ISampleProvider processingChain = Reader;
            
            // Aplica o equalizador se estiver ativo
            if (_equalizerEnabled && Equalizer != null)
            {
                // Reconecta o equalizador à cadeia
                Equalizer = new EqualizadorAudio(processingChain, Equalizer.Bands);
                processingChain = Equalizer;
                System.Diagnostics.Debug.WriteLine("[AUDIO CHAIN] Equalizador adicionado à cadeia de processamento");
            }
            
            // Aplica os efeitos se estiverem ativos
            if (_effectsEnabled && EffectsManager != null)
            {
                // Reconecta o gerenciador de efeitos à cadeia
                EffectsManager = new GerenciadorEfeitos(processingChain);
                processingChain = EffectsManager;
                System.Diagnostics.Debug.WriteLine("[AUDIO CHAIN] Efeitos adicionados à cadeia de processamento");
            }
            
            // Atualiza o FadeOutProvider para usar a cadeia final
            FadeOutProvider = new DelayFadeOutSampleProvider(processingChain);
            processingChain = FadeOutProvider;
            
            // Atualiza a referência à fonte ativa
            _activeSource = processingChain;
            
            // Reinicializa o WaveOut com a nova cadeia de processamento
            // Preserva a instância atual para uma transição mais suave
            WaveOut?.Stop();
            WaveOut?.Dispose();
            
            // Cria um novo WaveOut e inicializa com a cadeia de processamento
            WaveOut = new WaveOutEvent();
            WaveOut.DeviceNumber = DeviceOut;
            WaveOut.DesiredLatency = 75; // Reduzido para 75ms para menor latência
            
            // Reconecta o evento de PlaybackStopped - Removendo a definição inline
            WaveOut.PlaybackStopped += OnPlaybackStopped;
            
            // Inicializa o WaveOut com a cadeia final
            WaveOut.Init(processingChain);
            
            // Restaura a posição
            if (Reader != null && currentPosition < Reader.Length)
            {
                Reader.Position = currentPosition;
            }
            
            // Continua a reprodução se estivesse tocando
            if (wasPlaying)
            {
                WaveOut.Play();
            }
            
            System.Diagnostics.Debug.WriteLine("[AUDIO CHAIN] Cadeia de processamento reconstruída com sucesso");
            System.Diagnostics.Debug.WriteLine($"[AUDIO CHAIN] Equalizador ativo: {_equalizerEnabled}, Efeitos ativos: {_effectsEnabled}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUDIO CHAIN] Erro ao atualizar cadeia de processamento: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"[EQUALIZER] Ajustando banda {bandIndex} para ganho {gain}dB");
            Equalizer.Bands[bandIndex].Gain = gain;
            Equalizer.UpdateBand(bandIndex);
            
            // Tenta aplicar um teste de diagnóstico com um sinal extremo
            if (Math.Abs(gain) > 10)
            {
                System.Diagnostics.Debug.WriteLine($"[EQUALIZER] Teste de diagnóstico: ganho extremo ({gain}dB) detectado na banda {bandIndex}");
            }
            
            // Força a atualização da cadeia se o equalizador estiver ativo
            if (_equalizerEnabled)
            {
                System.Diagnostics.Debug.WriteLine("[EQUALIZER] Agendando atualização da cadeia de processamento");
                _pendingEqualizerUpdate = true;
                
                // Reinicia o timer para que ele espere mais tempo antes de processar
                _equalizerUpdateTimer.Stop();
                _equalizerUpdateTimer.Start();
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[EQUALIZER] ERRO: Tentativa de ajustar banda inválida: {bandIndex}");
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
    /// Configura o player de áudio
    /// </summary>
    private void SetupPlayer()
    {
        try
        {
            // Criar um novo WaveOut device para reproduzir o áudio
            WaveOut = new WaveOutEvent();
            WaveOut.DeviceNumber = DeviceOut;
            
            // Anexar o WaveOut ao Reader para iniciar a saída de áudio
            WaveOut.Init(Reader);
            
            // Configura o WaveOut para NÃO iniciar automaticamente
            if (WaveOut.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
            {
                WaveOut.Stop();
            }
            
            System.Diagnostics.Debug.WriteLine($"Player configurado com volume inicial {Reader.Volume} e em estado parado");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao configurar o player: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Controla se o áudio deve ser reproduzido em loop
    /// </summary>
    public bool LoopPlayback
    {
        get => _shouldLoopPlayback;
        set 
        { 
            _shouldLoopPlayback = value;
            System.Diagnostics.Debug.WriteLine($"Reprodução em loop {(_shouldLoopPlayback ? "ativada" : "desativada")}");
        }
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
