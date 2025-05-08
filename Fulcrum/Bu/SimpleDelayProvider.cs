using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de atraso simples (eco)
/// </summary>
public class SimpleDelayProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float[]? _delayBuffer;
    private int _delayBufferPosition;
    private int _delayBufferLength;
    private float _delayMilliseconds = 250f;
    private float _mix = 0.5f;
    private float _feedback = 0.3f;

    /// <summary>
    /// Inicializa uma nova instância do provedor de atraso/eco
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public SimpleDelayProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;

        // Configura o buffer de atraso
        RecalcularBuffer();
    }

    /// <summary>
    /// Recalcula o tamanho do buffer baseado no tempo de atraso
    /// </summary>
    private void RecalcularBuffer()
    {
        // Calcula o tamanho do buffer baseado no tempo de atraso e taxa de amostragem
        _delayBufferLength = (int)(WaveFormat.SampleRate * (_delayMilliseconds / 1000.0f));
        _delayBuffer = new float[_delayBufferLength];
        _delayBufferPosition = 0;
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Tempo de atraso em milissegundos (10 a 1000)
    /// </summary>
    public float DelayMilliseconds
    {
        get => _delayMilliseconds;
        set
        {
            float newValue = Math.Clamp(value, 10f, 1000f);
            if (Math.Abs(newValue - _delayMilliseconds) > 0.01f)
            {
                _delayMilliseconds = newValue;
                RecalcularBuffer();
            }
        }
    }

    /// <summary>
    /// Mix de efeito molhado/seco (0.0 a 1.0)
    /// </summary>
    public float Mix
    {
        get => _mix;
        set => _mix = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Feedback do atraso (0.0 a 0.9)
    /// </summary>
    public float Feedback
    {
        get => _feedback;
        set => _feedback = Math.Clamp(value, 0.0f, 0.9f);
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de atraso
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // Garante que o buffer de atraso esteja inicializado
        if (_delayBuffer == null)
        {
            RecalcularBuffer();
        }

        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0 && _mix > 0.001f) // Só aplica se o mix não for zero
        {
            // Calcula o fator de dry/wet mix
            float dryFactor = 1.0f - _mix;

            // Para cada amostra no buffer
            for (int i = 0; i < samplesRead; i++)
            {
                float originalSample = buffer[offset + i];

                // Obtém a amostra atrasada do buffer circular
                float delaySample = _delayBuffer![_delayBufferPosition];

                // Combina a amostra original com a amostra atrasada (com feedback)
                _delayBuffer[_delayBufferPosition] = originalSample + (delaySample * _feedback);

                // Avança a posição no buffer circular
                _delayBufferPosition = (_delayBufferPosition + 1) % _delayBufferLength;

                // Combina sinal seco com o atrasado
                buffer[offset + i] = (originalSample * dryFactor) + (delaySample * _mix);
            }
        }

        return samplesRead;
    }
}