using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de reverberação simples
/// </summary>
public class SimpleReverbProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float[] _reverbBuffer;
    private int _bufferPosition;
    private int _bufferLength;
    private float _decayTime = 1.0f;
    private float _mix = 0.3f;
    private float[] _tapDelays;
    private float[] _tapGains;

    /// <summary>
    /// Inicializa uma nova instância do provedor de reverberação
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public SimpleReverbProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        
        // Configura o buffer de reverberação
        RecalcularBuffer();
        
        // Inicializa os taps (pontos de atraso) para criar o efeito de reverberação
        ConfigurarTaps();
    }

    /// <summary>
    /// Recalcula o tamanho do buffer baseado no tempo de decaimento
    /// </summary>
    private void RecalcularBuffer()
    {
        // Cria um buffer baseado no tempo de reverberação e taxa de amostragem
        _bufferLength = (int)(WaveFormat.SampleRate * Math.Max(0.1f, _decayTime));
        _reverbBuffer = new float[_bufferLength];
        _bufferPosition = 0;
    }

    /// <summary>
    /// Configura os taps (pontos de atraso) para criar o efeito de reverberação
    /// </summary>
    private void ConfigurarTaps()
    {
        // Define diferentes pontos de atraso (em milissegundos) para simular reflexões sonoras
        int[] delaysMs = { 50, 100, 170, 270, 370, 470, 600, 700, 850, 1000 };
        float[] gains = { 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.3f, 0.2f, 0.15f, 0.1f };
        
        _tapDelays = new float[delaysMs.Length];
        _tapGains = new float[gains.Length];
        
        // Converte ms para amostras
        for (int i = 0; i < delaysMs.Length; i++)
        {
            _tapDelays[i] = (delaysMs[i] * WaveFormat.SampleRate) / 1000.0f;
            _tapGains[i] = gains[i];
        }
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Tempo de decaimento da reverberação em segundos (0.1 a 10.0)
    /// </summary>
    public float DecayTime
    {
        get => _decayTime;
        set
        {
            float newValue = Math.Clamp(value, 0.1f, 10.0f);
            if (Math.Abs(newValue - _decayTime) > 0.01f)
            {
                _decayTime = newValue;
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
    /// Lê amostras de áudio e aplica o efeito de reverberação
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0 && _mix > 0.001f) // Só aplica se o mix não for zero
        {
            // Calcula o fator de dry/wet mix
            float dryFactor = 1.0f - _mix;
            
            // Para cada amostra no buffer
            for (int i = 0; i < samplesRead; i++)
            {
                float originalSample = buffer[offset + i];
                float reverbSample = 0;
                
                // Soma as contribuições de cada tap
                for (int tapIndex = 0; tapIndex < _tapDelays.Length; tapIndex++)
                {
                    // Calcula a posição do tap no buffer circular
                    int tapPosition = (_bufferPosition - (int)_tapDelays[tapIndex]);
                    if (tapPosition < 0) tapPosition += _bufferLength;
                    
                    // Adiciona a contribuição deste tap
                    reverbSample += _reverbBuffer[tapPosition] * _tapGains[tapIndex] * (_decayTime / 1.0f);
                }
                
                // Armazena a amostra original + feedback no buffer de reverberação
                _reverbBuffer[_bufferPosition] = originalSample + (reverbSample * 0.4f); // 40% feedback
                
                // Avança a posição no buffer circular
                _bufferPosition = (_bufferPosition + 1) % _bufferLength;
                
                // Combina sinal seco com o reverberado
                buffer[offset + i] = (originalSample * dryFactor) + (reverbSample * _mix);
            }
        }

        return samplesRead;
    }
}