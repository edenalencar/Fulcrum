using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

// Certifique-se que o pacote NuGet NAudio esteja instalado corretamente.
// O arquivo de projeto já deve ter a referência: <PackageReference Include="NAudio" Version="2.2.1" />

/// <summary>
/// Provedor de efeito de flanger
/// </summary>
public class SimpleFlangerProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _delayBuffer;
    private int _bufferPosition;
    private readonly int _bufferLength;
    private float _depth = 0.005f; // Profundidade padrão
    private float _rate = 0.5f;    // Taxa em Hz
    private float _phase;

    /// <summary>
    /// Inicializa uma nova instância do provedor de flanger
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public SimpleFlangerProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        
        // O buffer deve ser grande o suficiente para acomodar o atraso máximo
        _bufferLength = (int)(source.WaveFormat.SampleRate * 0.02f); // 20ms de buffer
        _delayBuffer = new float[_bufferLength];
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; private set; }

    /// <summary>
    /// Profundidade do efeito flanger (0.001 a 0.01)
    /// </summary>
    public float Depth
    {
        get => _depth;
        set => _depth = Math.Clamp(value, 0.001f, 0.01f);
    }

    /// <summary>
    /// Taxa de modulação do flanger em Hz (0.1 a 5.0)
    /// </summary>
    public float Rate
    {
        get => _rate;
        set => _rate = Math.Clamp(value, 0.1f, 5.0f);
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de flanger
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0)
        {
            // Aplica o efeito de flanger
            for (int i = 0; i < samplesRead; i++)
            {
                // Armazena a amostra atual no buffer circular
                _delayBuffer[_bufferPosition] = buffer[offset + i];
                
                // Calcula a posição modulada para o efeito de flanger
                float modulationDepth = _depth * WaveFormat.SampleRate;
                float lfoValue = (float)Math.Sin(_phase);
                
                float delayOffset = (float)((1.0f + lfoValue) * modulationDepth / 2.0f);
                
                // Atualiza a fase do LFO
                _phase += 2 * (float)Math.PI * _rate / WaveFormat.SampleRate;
                if (_phase > 2 * Math.PI)
                    _phase -= 2 * (float)Math.PI;
                // Atualiza a fase do LFO
                _phase += 2 * (float)Math.PI * _rate / WaveFormat.SampleRate;
                if (_phase > 2 * Math.PI)
                    _phase -= 2 * (float)Math.PI;
                
                // Calcula a posição exata no buffer (pode ser um valor fracionário)
                float exactPosition = _bufferPosition - delayOffset;
                if (exactPosition < 0)
                    exactPosition += _bufferLength;
                
                // Interpolação linear entre as duas amostras mais próximas
                int index1 = (int)exactPosition;
                int index2 = (index1 + 1) % _bufferLength;
                float fraction = exactPosition - index1;
                
                float delayed = _delayBuffer[index1] * (1 - fraction) + _delayBuffer[index2] * fraction;
                
                // Combina o sinal original com o sinal defasado
                buffer[offset + i] = buffer[offset + i] * 0.5f + delayed * 0.5f;
                
                // Avança o ponteiro do buffer circular
                _bufferPosition = (_bufferPosition + 1) % _bufferLength;
            }
        }

        return samplesRead;
    }
}