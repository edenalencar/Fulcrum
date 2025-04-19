using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Fornecedor de amostras de áudio que implementa efeito de fade out com atraso
/// </summary>
public class DelayFadeOutSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private int _fadeSamplePosition;
    private int _fadeSampleCount;
    private int _delaySampleCount;
    private bool _fadingOut;
    private readonly object _lockObject = new();

    /// <summary>
    /// Inicializa uma nova instância da classe DelayFadeOutSampleProvider
    /// </summary>
    /// <param name="source">Fonte de amostras de áudio</param>
    public DelayFadeOutSampleProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
    }

    /// <summary>
    /// Obtém o formato de onda do áudio
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Inicia o processo de fade out após um atraso especificado
    /// </summary>
    /// <param name="delayMs">Atraso em milissegundos antes do fade out começar</param>
    /// <param name="fadeOutMs">Duração do fade out em milissegundos</param>
    public void BeginFadeOut(int delayMs, int fadeOutMs)
    {
        lock (_lockObject)
        {
            // Calcular quantas amostras duram o atraso
            _delaySampleCount = (int)(WaveFormat.SampleRate * delayMs / 1000.0);
            
            // Calcular quantas amostras duram o fade out
            _fadeSampleCount = (int)(WaveFormat.SampleRate * fadeOutMs / 1000.0);
            
            // Resetar a posição para indicar que estamos começando o atraso
            _fadeSamplePosition = -_delaySampleCount;
            
            // Indicar que estamos entrando no processo de fade out
            _fadingOut = true;
        }
    }

    /// <summary>
    /// Lê amostras do provedor de origem e aplica o efeito de fade out conforme configurado
    /// </summary>
    /// <param name="buffer">Buffer para receber as amostras</param>
    /// <param name="offset">Deslocamento inicial no buffer</param>
    /// <param name="count">Número de amostras a serem lidas</param>
    /// <returns>Número de amostras lidas</returns>
    public int Read(float[] buffer, int offset, int count)
    {
        // Ler amostras da fonte
        int sourceSamplesRead = _source.Read(buffer, offset, count);

        lock (_lockObject)
        {
            if (_fadingOut)
            {
                // Processar as amostras aplicando o efeito de fade out
                for (int n = 0; n < sourceSamplesRead; n++)
                {
                    // Incrementar a posição atual
                    _fadeSamplePosition++;
                    
                    // Se ainda estamos na fase de atraso, não modificar o som
                    if (_fadeSamplePosition < 0)
                        continue;
                        
                    // Se já passou o período de fade out, definir o volume para 0
                    if (_fadeSamplePosition >= _fadeSampleCount)
                    {
                        buffer[offset + n] = 0f;
                    }
                    // Durante o fade out, aplicar uma transição gradual do volume
                    else
                    {
                        // Calcular o fator de escala para o fade out
                        float fadeOutFactor = 1.0f - (_fadeSamplePosition / (float)_fadeSampleCount);
                        buffer[offset + n] *= fadeOutFactor;
                    }
                }
            }
        }

        return sourceSamplesRead;
    }

    /// <summary>
    /// Cancela o fade out em andamento
    /// </summary>
    public void CancelFadeOut()
    {
        lock (_lockObject)
        {
            _fadingOut = false;
        }
    }
}
