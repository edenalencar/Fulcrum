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
public class AudioVisualizer
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

    /// <summary>
    /// Limpa a visualização da forma de onda
    /// </summary>
    private void ClearWaveform()
    {
        _waveformPolyline.Points.Clear();
    }
}

/// <summary>
/// Classe auxiliar para ler amostras de um ISampleProvider como se fosse um WaveStream
/// </summary>
internal class SampleProviderWaveReader
{
    private readonly ISampleProvider _sampleProvider;

    public SampleProviderWaveReader(ISampleProvider sampleProvider)
    {
        _sampleProvider = sampleProvider ?? throw new ArgumentNullException(nameof(sampleProvider));
    }

    public int Read(float[] buffer, int offset, int count)
    {
        try
        {
            return _sampleProvider.Read(buffer, offset, count);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ler amostras: {ex.Message}");
            return 0;
        }
    }
}