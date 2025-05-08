using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de delay (eco)
/// </summary>
public class DelayProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _delayBuffer;
    private int _delayBufferPosition;
    private readonly int _delayBufferLength;
    private float _delayAmplification = 0.5f;
    private float _wetDryMix = 0.5f;
    private int _delayMilliseconds = 500;

    /// <summary>
    /// Inicializa um novo provedor de efeito de eco
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    /// <param name="delayMilliseconds">Tempo do delay em milissegundos</param>
    public DelayProvider(ISampleProvider source, int delayMilliseconds = 500)
    {
        _source = source;
        WaveFormat = source.WaveFormat;

        // Inicializa o atraso
        _delayMilliseconds = delayMilliseconds;

        // Calcula o tamanho do buffer baseado no tempo do delay
        // Acrescenta um buffer adicional para segurança
        _delayBufferLength = (int)(source.WaveFormat.SampleRate * (delayMilliseconds / 1000.0f) * 2);
        _delayBuffer = new float[_delayBufferLength];
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Tempo de delay em milissegundos
    /// </summary>
    public int DelayMilliseconds
    {
        get => _delayMilliseconds;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "O tempo de atraso não pode ser negativo");

            _delayMilliseconds = value;
        }
    }

    /// <summary>
    /// Amplitude do eco (0.0 a 1.0)
    /// </summary>
    public float DelayAmplification
    {
        get => _delayAmplification;
        set => _delayAmplification = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Mix entre sinal seco e sinal molhado (0.0 a 1.0)
    /// </summary>
    public float WetDryMix
    {
        get => _wetDryMix;
        set => _wetDryMix = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de delay
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0 && _wetDryMix > 0)
        {
            // Calcula o número de amostras para o delay
            int delaySamples = (int)(_delayMilliseconds * (WaveFormat.SampleRate / 1000.0));

            // Calcula o fator dry/wet
            float dryFactor = 1.0f - _wetDryMix;

            // Para cada amostra no buffer de entrada
            for (int i = 0; i < samplesRead; i++)
            {
                // Guarda a amostra original antes de modificar
                float originalSample = buffer[offset + i];

                // Obtém a amostra atrasada do buffer circular
                int delayPosition = (_delayBufferPosition - delaySamples + _delayBufferLength) % _delayBufferLength;
                float delaySample = _delayBuffer[delayPosition];

                // Escreve a amostra atual no buffer de delay (com feedback)
                _delayBuffer[_delayBufferPosition] = originalSample + (delaySample * _delayAmplification);

                // Avança a posição do buffer de delay
                _delayBufferPosition = (_delayBufferPosition + 1) % _delayBufferLength;

                // Mistura sinal seco (original) com o sinal de delay (molhado)
                buffer[offset + i] = (originalSample * dryFactor) + (delaySample * _wetDryMix);
            }
        }

        return samplesRead;
    }
}