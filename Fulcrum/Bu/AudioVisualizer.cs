using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.Foundation; // Adicionando o namespace para Point

namespace Fulcrum.Bu;

/// <summary>
/// Classe responsável pela visualização das ondas de áudio
/// </summary>
public class AudioVisualizer : IDisposable
{
    private readonly ISampleProvider _sampleProvider;
    private readonly Polyline _waveformPolyline;
    private readonly int _sampleCount;
    private readonly float[] _sampleBuffer;
    private readonly DispatcherTimer _updateTimer;
    private readonly List<Point> _points = new();
    private Color _color = Microsoft.UI.Colors.Green;
    private double _lineThickness = 2.0;
    private bool _isActive = false;

    /// <summary>
    /// Inicializa uma nova instância da classe AudioVisualizer
    /// </summary>
    /// <param name="sampleProvider">Provedor de amostras de áudio</param>
    /// <param name="waveformPolyline">Elemento Polyline do XAML para exibir a forma de onda</param>
    /// <param name="sampleCount">Número de amostras a serem analisadas (resolução da visualização)</param>
    public AudioVisualizer(ISampleProvider sampleProvider, Polyline waveformPolyline, int sampleCount = 100)
    {
        _sampleProvider = sampleProvider ?? throw new ArgumentNullException(nameof(sampleProvider));
        _waveformPolyline = waveformPolyline ?? throw new ArgumentNullException(nameof(waveformPolyline));
        _sampleCount = sampleCount;
        _sampleBuffer = new float[sampleCount];

        // Configurar visualmente o Polyline
        _waveformPolyline.Stroke = new SolidColorBrush(_color);
        _waveformPolyline.StrokeThickness = _lineThickness;

        // Configurar o timer para atualizar a visualização
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30) // ~33 fps
        };
        _updateTimer.Tick += UpdateTimer_Tick;

        System.Diagnostics.Debug.WriteLine($"AudioVisualizer inicializado: {sampleCount} amostras");
    }

    /// <summary>
    /// Inicia a visualização das ondas de áudio
    /// </summary>
    public void Start()
    {
        if (!_isActive)
        {
            System.Diagnostics.Debug.WriteLine("Iniciando visualização de áudio");
            _updateTimer.Start();
            _isActive = true;
        }
    }

    /// <summary>
    /// Para a visualização das ondas de áudio
    /// </summary>
    public void Stop()
    {
        if (_isActive)
        {
            System.Diagnostics.Debug.WriteLine("Parando visualização de áudio");
            _updateTimer.Stop();
            _isActive = false;
            ClearWaveform();
        }
    }

    /// <summary>
    /// Define a cor da forma de onda
    /// </summary>
    /// <param name="color">Nova cor para a forma de onda</param>
    public void SetColor(Color color)
    {
        _color = color;
        if (_waveformPolyline != null)
        {
            _waveformPolyline.Stroke = new SolidColorBrush(color);
        }
    }

    /// <summary>
    /// Define a espessura da linha da forma de onda
    /// </summary>
    /// <param name="thickness">Nova espessura para a linha</param>
    public void SetThickness(double thickness)
    {
        _lineThickness = thickness;
        if (_waveformPolyline != null)
        {
            _waveformPolyline.StrokeThickness = thickness;
        }
    }

    /// <summary>
    /// Manipulador de evento para atualizar a visualização da onda
    /// </summary>
    private void UpdateTimer_Tick(object? sender, object e)
    {
        try
        {
            UpdateWaveform();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar forma de onda: {ex.Message}");
            // Não propaga a exceção para evitar que o timer seja interrompido
        }
    }

    /// <summary>
    /// Atualiza a visualização da forma de onda
    /// </summary>
    private void UpdateWaveform()
    {
        try
        {
            // Verificar se o provedor de amostras ainda é válido
            if (_sampleProvider == null)
            {
                ClearWaveform();
                return;
            }

            // Ler amostras do provedor de áudio
            var reader = new SampleProviderWaveReader(_sampleProvider);
            int samplesRead = reader.Read(_sampleBuffer, 0, _sampleCount);

            if (samplesRead == 0)
            {
                // Não há amostras para ler
                ClearWaveform();
                return;
            }

            // Atualizar os pontos da forma de onda
            _points.Clear();

            // Verificar se o controle tem dimensões válidas
            if (_waveformPolyline.ActualWidth <= 0 || _waveformPolyline.ActualHeight <= 0)
            {
                // Dimensões inválidas, não podemos desenhar
                return;
            }

            double width = _waveformPolyline.ActualWidth;
            double height = _waveformPolyline.ActualHeight;
            double centerY = height / 2;
            float maxSample = 0;
            
            // Encontrar o valor máximo para normalização
            for (int i = 0; i < samplesRead; i++)
            {
                maxSample = Math.Max(maxSample, Math.Abs(_sampleBuffer[i]));
            }
            
            // Fator de escala para evitar clipping e amplificar sinais fracos
            float scaleFactor = maxSample > 0.01f ? 0.9f / Math.Max(1.0f, maxSample) : 0.9f;

            for (int i = 0; i < samplesRead; i++)
            {
                double x = (width / samplesRead) * i;
                double y = centerY + (_sampleBuffer[i] * scaleFactor * centerY); // Amplificar para visualização

                _points.Add(new Point(x, y));
            }

            // Atualizar o Polyline com os novos pontos
            _waveformPolyline.Points.Clear();
            foreach (var point in _points)
            {
                _waveformPolyline.Points.Add(point);
            }
            
            // Log periódico (1 a cada 10 atualizações)
            if (DateTime.Now.Second % 10 == 0 && DateTime.Now.Millisecond < 100)
            {
                System.Diagnostics.Debug.WriteLine($"Forma de onda atualizada: {samplesRead} amostras, Max={maxSample:F3}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar forma de onda: {ex.Message}");
            ClearWaveform();
        }
    }

    /// <summary>
    /// Limpa a visualização da forma de onda
    /// </summary>
    private void ClearWaveform()
    {
        _waveformPolyline.Points.Clear();
    }

    /// <summary>
    /// Libera os recursos utilizados pelo visualizador
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Para a visualização se estiver em execução
            Stop();
            
            // Se temos acesso ao provedor de amostras, marcamos que ele não deve mais ser acessado
            if (_sampleProvider is ISampleProvider provider && 
                _waveformPolyline?.Dispatcher?.HasThreadAccess == true)
            {
                var reader = new SampleProviderWaveReader(provider);
                reader.MarkAsDisposed();
            }
            
            // Limpa os pontos da forma de onda para liberar memória
            _points.Clear();
            
            System.Diagnostics.Debug.WriteLine("Recursos do visualizador liberados com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao liberar recursos do visualizador: {ex.Message}");
        }
    }
}

/// <summary>
/// Classe auxiliar para ler amostras de um ISampleProvider como se fosse um WaveStream
/// </summary>
internal class SampleProviderWaveReader
{
    private readonly ISampleProvider _sampleProvider;
    private volatile bool _isDisposed = false;
    private readonly object _lock = new object();
    private WeakReference<AudioFileReader> _cachedAudioFileReader = null;

    public SampleProviderWaveReader(ISampleProvider sampleProvider)
    {
        _sampleProvider = sampleProvider ?? throw new ArgumentNullException(nameof(sampleProvider));
        
        // Armazena uma referência fraca se for um AudioFileReader
        if (sampleProvider is AudioFileReader afr)
        {
            _cachedAudioFileReader = new WeakReference<AudioFileReader>(afr);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        // Verificação rápida antes de fazer qualquer operação
        if (_isDisposed || _sampleProvider == null)
            return 0;

        try
        {
            // Usando um lock para garantir thread-safety
            lock (_lock)
            {
                // Verificação após obter o lock
                if (_isDisposed || _sampleProvider == null)
                    return 0;

                // Verificação de tipo e validade
                if (_sampleProvider is AudioFileReader || _cachedAudioFileReader != null)
                {
                    // Encapsular todo o código em um único try/catch
                    try
                    {
                        // Checar a referência do AudioFileReader
                        AudioFileReader audioReader = null;
                        
                        // Tentativa 1: Obter do cache WeakReference
                        if (_cachedAudioFileReader != null)
                        {
                            if (!_cachedAudioFileReader.TryGetTarget(out audioReader))
                            {
                                _isDisposed = true;
                                return 0;
                            }
                        }
                        
                        // Tentativa 2: Cast direto (fallback)
                        if (audioReader == null && _sampleProvider is AudioFileReader castReader)
                        {
                            audioReader = castReader;
                        }
                        
                        // Se ainda for nulo após as tentativas, desistir
                        if (audioReader == null)
                        {
                            _isDisposed = true;
                            return 0;
                        }
                        
                        // Verificação de propriedades críticas em um único bloco try/catch
                        try
                        {
                            // Usar um método de extensão que captura exceções internamente
                            if (!IsSafeToUse(audioReader))
                            {
                                _isDisposed = true;
                                return 0;
                            }
                        }
                        catch
                        {
                            // Qualquer exceção aqui indica que o objeto não é seguro para uso
                            _isDisposed = true;
                            return 0;
                        }
                    }
                    catch
                    {
                        // Qualquer falha no processo de verificação
                        _isDisposed = true;
                        return 0;
                    }
                }
                
                // Feitas todas as verificações, tentar ler as amostras
                try
                {
                    if (_isDisposed) return 0;
                    return _sampleProvider.Read(buffer, offset, count);
                }
                catch
                {
                    _isDisposed = true;
                    return 0;
                }
            }
        }
        catch
        {
            // Qualquer exceção no nível superior
            _isDisposed = true;
            return 0;
        }
    }
    
    // Método auxiliar que verifica de forma segura se um AudioFileReader está em estado válido
    private bool IsSafeToUse(AudioFileReader reader)
    {
        if (reader == null || _isDisposed) 
            return false;
            
        // Verificação ultra defensiva usando reflexão para verificar se o objeto
        // ainda é válido sem tentar acessar suas propriedades diretamente
        try
        {
            // Primeiro, tentar acessar WaveFormat que geralmente é mais estável
            var waveFormat = reader.WaveFormat;
            if (waveFormat == null)
                return false;
            
            // Verificação de Position usando reflexão para evitar acessar diretamente
            // objetos internos que possam ter sido liberados
            try
            {
                // Acessar o campo _waveStream usando reflexão (campo interno do AudioFileReader)
                var waveStreamField = reader.GetType()
                    .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => f.Name.Contains("Stream"));
                    
                if (waveStreamField == null || waveStreamField.GetValue(reader) == null)
                    return false;
                    
                // Se chegou até aqui, o objeto interno parece válido
                // Mas ainda assim, coloque o acesso à Position em um bloco try separado
                try
                {
                    var dummy = reader.Position;
                    if (dummy < 0)
                        return false;
                        
                    var length = reader.Length;
                    if (length <= 0)
                        return false;
                }
                catch
                {
                    // Qualquer falha ao acessar Position ou Length indica objeto inválido
                    return false;
                }
            }
            catch
            {
                return false;
            }
            
            // Todas as verificações passaram
            return true;
        }
        catch
        {
            // Qualquer exceção indica que o objeto está em estado inválido
            return false;
        }
    }

    public void MarkAsDisposed()
    {
        _isDisposed = true;
        _cachedAudioFileReader = null; // Libera a referência fraca
    }
}