using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de reverberação
/// </summary>
public class ReverbSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float[] _delayBuffer;
    private int _bufferPosition;
    private float _reverbMix = 0.3f;
    private float _reverbTime = 1.0f;
    private bool _isEnabled = false;
    private readonly int _delaySamples;

    /// <summary>
    /// Inicializa uma nova instância do provedor de reverberação
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public ReverbSampleProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        
        // Calcula o tamanho do buffer de delay baseado no tempo máximo de reverberação
        float maxReverbTime = 5.0f; // 5 segundos de reverberação máxima
        _delaySamples = (int)(WaveFormat.SampleRate * maxReverbTime);
        _delayBuffer = new float[_delaySamples];
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
    /// Mix de reverberação (0.0 a 1.0)
    /// </summary>
    public float ReverbMix
    {
        get => _reverbMix;
        set => _reverbMix = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Tempo de reverberação em segundos (0.1 a 10.0)
    /// </summary>
    public float ReverbTime
    {
        get => _reverbTime;
        set => _reverbTime = Math.Clamp(value, 0.1f, 5.0f);
    }

    /// <summary>
    /// Processa amostras de áudio aplicando o efeito de reverberação
    /// </summary>
    /// <param name="buffer">Buffer de amostras</param>
    /// <param name="offset">Offset no buffer</param>
    /// <param name="count">Número de amostras a processar</param>
    public void ProcessInPlace(float[] buffer, int offset, int count)
    {
        if (!_isEnabled || _reverbMix <= 0.001f) return;

        // Calcula o decaimento baseado no tempo de reverberação
        float decay = (float)Math.Exp(-3.0 / (WaveFormat.SampleRate * _reverbTime));
        
        for (int i = 0; i < count; i++)
        {
            // Obtém a amostra original
            float sample = buffer[offset + i];
            
            // Calcula várias reflexões (simulando reverberação)
            float reverb = 0;
            
            // Tamanho dos atrasos para criar diferentes reflexões
            int[] delays = { 
                (int)(0.05f * WaveFormat.SampleRate), 
                (int)(0.08f * WaveFormat.SampleRate),
                (int)(0.12f * WaveFormat.SampleRate),
                (int)(0.17f * WaveFormat.SampleRate) 
            };
            
            // Soma várias reflexões atrasadas e atenuadas
            foreach (int delay in delays)
            {
                int delayIndex = (_bufferPosition - delay + _delaySamples) % _delaySamples;
                reverb += _delayBuffer[delayIndex] * 0.5f;
            }
            
            // Adiciona feedback para criar cauda de reverberação
            reverb *= decay;
            
            // Armazena a amostra atual + reverb no buffer de delay
            _delayBuffer[_bufferPosition] = sample + (reverb * 0.6f);
            _bufferPosition = (_bufferPosition + 1) % _delaySamples;
            
            // Combina o sinal seco com o reverberado
            buffer[offset + i] = (sample * (1 - _reverbMix)) + (reverb * _reverbMix);
        }
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de reverberação
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);
        
        if (samplesRead > 0 && _isEnabled)
        {
            ProcessInPlace(buffer, offset, samplesRead);
        }
        
        return samplesRead;
    }

    /// <summary>
    /// Limpa o buffer de reverberação
    /// </summary>
    public void Clear()
    {
        Array.Clear(_delayBuffer, 0, _delaySamples);
        _bufferPosition = 0;
    }
}