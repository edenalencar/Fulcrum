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

    /// <summary>
    /// Inicializa uma nova instância da classe AudioVisualizer
    /// </summary>
    /// <param name="sampleProvider">Provedor de amostras de áudio</param>
    /// <param name="waveformPolyline">Elemento Polyline do XAML para exibir a forma de onda</param>
    /// <param name="sampleCount">Número de amostras a serem analisadas (resolução da visualização)</param>
    public AudioVisualizer(ISampleProvider sampleProvider, Polyline waveformPolyline, int sampleCount = 100)
    {
        _sampleProvider = sampleProvider;
        _waveformPolyline = waveformPolyline;
        _sampleCount = sampleCount;
        _sampleBuffer = new float[sampleCount];

        // Configurar o timer para atualizar a visualização
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30) // ~33 fps
        };
        _updateTimer.Tick += UpdateTimer_Tick;
    }

    /// <summary>
    /// Inicia a visualização das ondas de áudio
    /// </summary>
    public void Start()
    {
        _updateTimer.Start();
    }

    /// <summary>
    /// Para a visualização das ondas de áudio
    /// </summary>
    public void Stop()
    {
        _updateTimer.Stop();
    }

    /// <summary>
    /// Manipulador de evento para atualizar a visualização da onda
    /// </summary>
    private void UpdateTimer_Tick(object? sender, object e)
    {
        UpdateWaveform();
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

        double width = _waveformPolyline.ActualWidth;
        double height = _waveformPolyline.ActualHeight;
        double centerY = height / 2;

        for (int i = 0; i < samplesRead; i++)
        {
            double x = (width / samplesRead) * i;
            double y = centerY + (_sampleBuffer[i] * centerY); // Amplificar para visualização

            _points.Add(new Point(x, y));
        }

        // Atualizar o Polyline com os novos pontos
        _waveformPolyline.Points.Clear();
        foreach (var point in _points)
        {
            _waveformPolyline.Points.Add(point);
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
    /// Define a cor da forma de onda
    /// </summary>
    /// <param name="color">Nova cor para a forma de onda</param>
    public void SetColor(Color color)
    {
        _waveformPolyline.Stroke = new SolidColorBrush(color);
    }

    /// <summary>
    /// Define a espessura da linha da forma de onda
    /// </summary>
    /// <param name="thickness">Nova espessura para a linha</param>
    public void SetThickness(double thickness)
    {
        _waveformPolyline.StrokeThickness = thickness;
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
        _sampleProvider = sampleProvider;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        return _sampleProvider.Read(buffer, offset, count);
    }
}