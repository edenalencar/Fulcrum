using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de alteração de tonalidade (pitch)
/// </summary>
public class SimplePitchProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float _pitchFactor = 1.0f;
    private float _sampleBuffer;
    private float _position;
    private float _previousSample;

    /// <summary>
    /// Inicializa uma nova instância do provedor de alteração de pitch
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public SimplePitchProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Fator de alteração de pitch (0.5 a 2.0)
    /// Valores abaixo de 1.0 tornam o som mais grave
    /// Valores acima de 1.0 tornam o som mais agudo
    /// </summary>
    public float PitchFactor
    {
        get => _pitchFactor;
        set => _pitchFactor = Math.Clamp(value, 0.5f, 2.0f);
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de alteração de pitch
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // Se o fator de pitch for 1.0 (sem alteração), passamos o áudio diretamente
        if (Math.Abs(_pitchFactor - 1.0f) < 0.01f)
        {
            return _source.Read(buffer, offset, count);
        }

        float[] sourceBuffer = new float[count];
        int samplesRead = _source.Read(sourceBuffer, 0, count);

        if (samplesRead > 0)
        {
            // Implementação simples de alteração de pitch usando interpolação linear
            for (int i = 0; i < samplesRead; i++)
            {
                // Calcula a posição de leitura no áudio fonte
                _position += _pitchFactor;
                
                // Converte posição para índice (inteiro) e fração (para interpolação)
                int intPosition = (int)_position;
                float fraction = _position - intPosition;
                
                // Lê amostra atual e próxima para interpolação
                float currentSample = intPosition < samplesRead ? sourceBuffer[intPosition] : 0;
                float nextSample = intPosition + 1 < samplesRead ? sourceBuffer[intPosition + 1] : 0;
                
                // Interpolação linear
                buffer[offset + i] = currentSample * (1 - fraction) + nextSample * fraction;
                
                // Se passamos do final do buffer fonte, é necessário buscar mais amostras
                if (_position >= samplesRead - 1)
                {
                    // Reposiciona e mantém a continuidade
                    float remaining = _position - (samplesRead - 1);
                    _position = remaining;
                    _previousSample = sourceBuffer[samplesRead - 1];
                    
                    // Lê mais amostras se necessário
                    if (_position > 0)
                    {
                        int additionalSamples = _source.Read(sourceBuffer, 0, count);
                        if (additionalSamples == 0)
                        {
                            // Acabou o áudio fonte
                            return i + 1;
                        }
                        samplesRead = additionalSamples;
                    }
                }
            }
        }

        return samplesRead;
    }
}