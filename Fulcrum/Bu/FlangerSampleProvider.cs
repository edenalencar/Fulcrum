using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito flanger
/// </summary>
public class FlangerSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _buffer;
    private int _bufferPosition;
    private readonly int _bufferLength;
    private float _depth = 0.45f;
    private float _feedback = 0.5f;
    private float _rate = 0.25f;
    private float _mix = 0.5f;
    private float _phase;
    private bool _isEnabled = true;

    /// <summary>
    /// Inicializa uma nova instância do provedor de flanger
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public FlangerSampleProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;

        // Criação de buffer para o efeito flanger
        // O buffer precisa ser grande o suficiente para acomodar as maiores profundidades de delay
        _bufferLength = source.WaveFormat.SampleRate / 5; // 200ms de buffer máximo
        _buffer = new float[_bufferLength];
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Determina se o efeito está ativo
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// Profundidade do efeito flanger (0.01 a 1.0)
    /// </summary>
    public float Depth
    {
        get => _depth;
        set => _depth = Math.Clamp(value, 0.01f, 1.0f);
    }

    /// <summary>
    /// Taxa de variação do flanger em Hz (0.1 a 5.0)
    /// </summary>
    public float Rate
    {
        get => _rate;
        set => _rate = Math.Clamp(value, 0.1f, 5.0f);
    }

    /// <summary>
    /// Feedback do efeito flanger (0.0 a 0.9)
    /// </summary>
    public float Feedback
    {
        get => _feedback;
        set => _feedback = Math.Clamp(value, 0.0f, 0.9f);
    }

    /// <summary>
    /// Mix de sinal seco/molhado (0.0 a 1.0)
    /// </summary>
    public float Mix
    {
        get => _mix;
        set => _mix = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Configura os parâmetros do flanger de uma só vez
    /// </summary>
    /// <param name="rate">Taxa de oscilação em Hz</param>
    /// <param name="depth">Profundidade do efeito</param>
    /// <param name="feedback">Feedback do efeito</param>
    /// <param name="mix">Mix de efeito molhado/seco</param>
    public void ConfigurarParametros(float rate, float depth, float feedback = 0.5f, float mix = 0.5f)
    {
        Rate = rate;
        Depth = depth;
        Feedback = feedback;
        Mix = mix;
    }

    /// <summary>
    /// Processa amostras de áudio aplicando o efeito flanger diretamente no buffer
    /// </summary>
    /// <param name="buffer">Buffer de amostras</param>
    /// <param name="offset">Offset no buffer</param>
    /// <param name="count">Número de amostras a processar</param>
    public void ProcessSamples(float[] buffer, int offset, int count)
    {
        if (!_isEnabled || _mix <= 0.001f) return;

        // Calcula o fator de dry/wet mix
        float dryFactor = 1.0f - _mix;

        // Processa cada amostra
        for (int i = 0; i < count; i++)
        {
            // Atualiza a fase do LFO (oscilador de baixa frequência)
            _phase += _rate / WaveFormat.SampleRate;
            if (_phase > 1.0f) _phase -= 1.0f;

            // Função seno para criar um delay variável
            float delayOffset = ((float)Math.Sin(_phase * 2 * Math.PI) * 0.5f + 0.5f) * _depth;

            // Calcula o atraso em amostras (1ms a ~20ms)
            int delaySamples = (int)(delayOffset * WaveFormat.SampleRate / 50);

            // Salva a amostra original
            float originalSample = buffer[offset + i];

            // Armazena no buffer circular
            _buffer[_bufferPosition] = originalSample;

            // Calcula a posição atrasada no buffer circular
            int delayPos = (_bufferPosition - delaySamples + _bufferLength) % _bufferLength;

            // Obtém a amostra atrasada
            float delaySample = _buffer[delayPos];

            // Aplica feedback ao buffer
            _buffer[_bufferPosition] = originalSample + (delaySample * _feedback);

            // Avança a posição no buffer circular
            _bufferPosition = (_bufferPosition + 1) % _bufferLength;

            // Combina sinal seco com o processado
            buffer[offset + i] = (originalSample * dryFactor) + (delaySample * _mix);
        }
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito flanger
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0 && _isEnabled && _mix > 0.001f) // Só aplica se estiver ativo e o mix não for zero
        {
            ProcessSamples(buffer, offset, samplesRead);
        }

        return samplesRead;
    }

    /// <summary>
    /// Reseta a fase do LFO do flanger
    /// </summary>
    public void Reset()
    {
        _phase = 0f;
        Array.Clear(_buffer, 0, _buffer.Length);
    }
}