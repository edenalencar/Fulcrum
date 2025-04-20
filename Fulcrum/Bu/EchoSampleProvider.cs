using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de eco
/// </summary>
public class EchoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float[] _echoBuffer;
    private int _bufferPosition;
    private float _delay = 250f; // delay em ms
    private float _mix = 0.5f;   // proporção do eco
    private bool _isEnabled = false;
    private readonly int _sampleRate;

    /// <summary>
    /// Inicializa uma nova instância do provedor de eco
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public EchoSampleProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        _sampleRate = WaveFormat.SampleRate;
        
        // Inicializa o buffer de eco com tamanho para até 2 segundos de delay
        _echoBuffer = new float[_sampleRate * 2];
        _bufferPosition = 0;
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
    /// Tempo de atraso do eco em milissegundos (10 a 1000)
    /// </summary>
    public float Delay
    {
        get => _delay;
        set => _delay = Math.Clamp(value, 10f, 1000f);
    }

    /// <summary>
    /// Mix do eco (0.0 a 1.0)
    /// </summary>
    public float Mix
    {
        get => _mix;
        set => _mix = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Processa amostras de áudio aplicando o efeito de eco
    /// </summary>
    /// <param name="buffer">Buffer de amostras</param>
    /// <param name="offset">Offset no buffer</param>
    /// <param name="count">Número de amostras a processar</param>
    public void ProcessInPlace(float[] buffer, int offset, int count)
    {
        if (!_isEnabled || _mix <= 0.001f) return;
        
        // Calcula o número de amostras correspondente ao delay em ms
        int delaySamples = (int)(_delay * _sampleRate / 1000);
        
        // Limita ao tamanho do buffer
        delaySamples = Math.Min(delaySamples, _echoBuffer.Length - 1);
        
        for (int i = 0; i < count; i++)
        {
            // Obtém a amostra original
            float sample = buffer[offset + i];
            
            // Calcula a posição do eco no buffer circular
            int echoPos = (_bufferPosition - delaySamples + _echoBuffer.Length) % _echoBuffer.Length;
            
            // Obtém a amostra atrasada
            float echoSample = _echoBuffer[echoPos];
            
            // Armazena a amostra atual no buffer
            _echoBuffer[_bufferPosition] = sample + (echoSample * 0.6f); // feedback para múltiplos ecos
            
            // Avança a posição no buffer circular
            _bufferPosition = (_bufferPosition + 1) % _echoBuffer.Length;
            
            // Combina a amostra original com o eco
            buffer[offset + i] = (sample * (1 - _mix)) + (echoSample * _mix);
        }
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de eco
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
    /// Limpa o buffer de eco
    /// </summary>
    public void Clear()
    {
        Array.Clear(_echoBuffer, 0, _echoBuffer.Length);
        _bufferPosition = 0;
    }
}