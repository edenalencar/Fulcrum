using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de alteração de tom (pitch)
/// </summary>
public class PitchShiftingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float _pitchFactor = 1.0f;
    private bool _isEnabled = false;

    /// <summary>
    /// Inicializa uma nova instância do provedor de alteração de tom
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public PitchShiftingSampleProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
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
    /// Fator de alteração de tom (0.5 a 2.0, onde 1.0 é o tom original)
    /// </summary>
    public float PitchFactor
    {
        get => _pitchFactor;
        set => _pitchFactor = Math.Clamp(value, 0.5f, 2.0f);
    }

    /// <summary>
    /// Processa amostras de áudio aplicando a alteração de tom
    /// </summary>
    /// <param name="buffer">Buffer de amostras</param>
    /// <param name="offset">Offset no buffer</param>
    /// <param name="count">Número de amostras a processar</param>
    public void ProcessInPlace(float[] buffer, int offset, int count)
    {
        if (!_isEnabled || Math.Abs(_pitchFactor - 1.0f) < 0.01f)
        {
            // Se o efeito estiver desativado ou o pitch for muito próximo de 1.0, não faz nada
            return;
        }
        
        // Implementação simplificada - em uma aplicação real, seria melhor usar
        // um algoritmo mais sofisticado como o WSOLA (Waveform Similarity Overlap-Add)
        
        // Cria um buffer temporário para armazenar as amostras originais
        float[] tempBuffer = new float[count];
        Array.Copy(buffer, offset, tempBuffer, 0, count);
        
        // Aplica o pitch shift
        for (int i = 0; i < count; i++)
        {
            // Para simplificar, vamos usar interpolação linear
            float targetIndex = i / _pitchFactor;
            
            if (targetIndex >= 0 && targetIndex < count - 1)
            {
                int index = (int)targetIndex;
                float fraction = targetIndex - index;
                
                // Interpolação linear entre as duas amostras mais próximas
                float sample1 = tempBuffer[index];
                float sample2 = tempBuffer[Math.Min(index + 1, count - 1)];
                
                buffer[offset + i] = sample1 + fraction * (sample2 - sample1);
            }
            else if (targetIndex < 0)
            {
                buffer[offset + i] = 0; // Silêncio para índices negativos
            }
            else
            {
                buffer[offset + i] = 0; // Silêncio para índices além do buffer
            }
        }
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de alteração de tom
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
}