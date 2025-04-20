using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de amostras com capacidade de fade in/out
/// </summary>
public class DelayFadeOutSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float _fadeDurationMs;
    private int _fadeSampleCount;
    private int _fadeSamplesRemaining;
    private bool _isFadingOut;

    /// <summary>
    /// Inicializa uma nova instância do provedor de fade
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    /// <param name="fadeDurationMs">Duração do fade em milissegundos</param>
    public DelayFadeOutSampleProvider(ISampleProvider source, float fadeDurationMs = 500f)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _fadeDurationMs = fadeDurationMs;
        WaveFormat = source.WaveFormat;
        
        // Calcular o número de amostras para o fade
        _fadeSampleCount = (int)((fadeDurationMs / 1000.0) * WaveFormat.SampleRate * WaveFormat.Channels);
        System.Diagnostics.Debug.WriteLine($"DelayFadeOutSampleProvider inicializado: Canais={WaveFormat.Channels}, " +
                                         $"Taxa={WaveFormat.SampleRate}, FadeSamples={_fadeSampleCount}");
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Inicia o processo de fade out
    /// </summary>
    public void BeginFadeOut()
    {
        _isFadingOut = true;
        _fadeSamplesRemaining = _fadeSampleCount;
        System.Diagnostics.Debug.WriteLine("Iniciando fade out");
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de fade
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // Ler da fonte
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead <= 0)
        {
            System.Diagnostics.Debug.WriteLine("Nenhuma amostra lida da fonte");
            return 0;
        }

        if (_isFadingOut)
        {
            // Aplica o fade out
            for (int i = 0; i < samplesRead; i++)
            {
                if (_fadeSamplesRemaining > 0)
                {
                    // Fator de atenuação: 1.0 -> 0.0 conforme _fadeSamplesRemaining diminui
                    float fadeOutFactor = (float)_fadeSamplesRemaining / _fadeSampleCount;
                    buffer[offset + i] *= fadeOutFactor;
                    _fadeSamplesRemaining--;
                }
                else
                {
                    // Quando o fade completa, silencia o áudio
                    buffer[offset + i] = 0;
                }
            }

            // Log periódico para diagnóstico (a cada 10000 amostras)
            if (_fadeSamplesRemaining % 10000 == 0 && _fadeSamplesRemaining > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Fade out em progresso: {_fadeSamplesRemaining} amostras restantes");
            }
        }
        else
        {
            // Verificação de integridade de áudio (para depuração)
            if (samplesRead > 0)
            {
                float maxSample = 0;
                for (int i = 0; i < Math.Min(100, samplesRead); i++)
                {
                    maxSample = Math.Max(maxSample, Math.Abs(buffer[offset + i]));
                }
                if (maxSample > 1.0f)
                {
                    System.Diagnostics.Debug.WriteLine($"AVISO: Amostras com valor acima de 1.0 detectadas: {maxSample}");
                }
            }
        }

        return samplesRead;
    }
}
