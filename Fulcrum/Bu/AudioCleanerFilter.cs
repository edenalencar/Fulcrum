using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace Fulcrum.Bu;

/// <summary>
/// Implementa um filtro de limpeza para remover anomalias e valores extremos no áudio
/// </summary>
public class AudioCleanerFilter : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _thresholdLevel;
    private readonly int _medianFilterSize;
    private readonly Queue<float> _medianFilterBuffer;
    private readonly float[] _sortBuffer;
    private float _lastValidSample = 0.0f;
    private bool _useSmoothTransition = true;
    private float _recoveryFactor = 0.1f;
    private int _consecutiveExtremeValues = 0;
    private readonly int _maxConsecutiveExtreme = 100;
    private bool _isEnabled = true;
    
    /// <summary>
    /// Inicializa uma nova instância do filtro de limpeza de áudio
    /// </summary>
    /// <param name="source">Fonte de áudio a ser filtrada</param>
    /// <param name="thresholdLevel">Limite para considerar uma amostra como anômala (padrão 10.0)</param>
    /// <param name="medianFilterSize">Tamanho do filtro de mediana para suavização (padrão 5)</param>
    public AudioCleanerFilter(ISampleProvider source, float thresholdLevel = 20.0f, int medianFilterSize = 5)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _thresholdLevel = thresholdLevel;
        _medianFilterSize = medianFilterSize;
        _medianFilterBuffer = new Queue<float>(_medianFilterSize);
        _sortBuffer = new float[_medianFilterSize];
        
        // Preenche o buffer inicial com zeros
        for (int i = 0; i < _medianFilterSize; i++)
        {
            _medianFilterBuffer.Enqueue(0.0f);
        }
        
        System.Diagnostics.Debug.WriteLine($"AudioCleanerFilter inicializado: Threshold={_thresholdLevel}, MedianFilterSize={_medianFilterSize}");
    }

    /// <summary>
    /// Obtém ou define se o filtro está ativo
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// Obtém o formato de onda da fonte
    /// </summary>
    public WaveFormat WaveFormat => _source.WaveFormat;
    
    /// <summary>
    /// Calcula a mediana dos valores no buffer
    /// </summary>
    private float CalculateMedian()
    {
        _medianFilterBuffer.CopyTo(_sortBuffer, 0);
        Array.Sort(_sortBuffer);
        return _sortBuffer[_medianFilterSize / 2];
    }

    /// <summary>
    /// Lê amostras da fonte e aplica o filtro de limpeza
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // Lê amostras da fonte
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead <= 0 || !_isEnabled)
            return samplesRead;

        // Processa cada amostra
        for (int i = 0; i < samplesRead; i++)
        {
            float sample = buffer[offset + i];
            float absSample = Math.Abs(sample);
            
            // Verifica se a amostra é anômala (muito acima do limiar)
            if (absSample > _thresholdLevel)
            {
                // Incrementa contador de valores extremos consecutivos
                _consecutiveExtremeValues++;
                
                if (_consecutiveExtremeValues > _maxConsecutiveExtreme)
                {
                    // Se houver muitos valores extremos consecutivos, é uma corrução prolongada
                    // Reduzimos a amostra mas não silenciamos completamente
                    float sign = Math.Sign(sample);
                    buffer[offset + i] = sign * (_thresholdLevel * 0.9f);
                    
                    // Registra a ocorrência apenas periodicamente para não sobrecarregar o log
                    if (_consecutiveExtremeValues % 1000 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Corrução prolongada detectada: {_consecutiveExtremeValues} amostras consecutivas acima do limite.");
                    }
                }
                else
                {
                    if (_useSmoothTransition)
                    {
                        // Não substituímos completamente, apenas limitamos a um valor máximo
                        // preservando o sinal original para manter características do áudio
                        float sign = Math.Sign(sample);
                        buffer[offset + i] = sign * Math.Min(absSample, _thresholdLevel * 0.9f);
                    }
                    else
                    {
                        // Alternativa: substitui pela mediana dos valores recentes
                        buffer[offset + i] = CalculateMedian();
                    }
                    
                    // Registra a substituição (limitado para não sobrecarregar o log)
                    if (_consecutiveExtremeValues == 1 || _consecutiveExtremeValues % 1000 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Amostra anômala corrigida: {sample} limitada para {buffer[offset + i]}");
                    }
                }
            }
            else
            {
                // Amostra dentro dos limites normais
                _lastValidSample = sample;
                _consecutiveExtremeValues = 0;
            }
            
            // Atualiza o buffer do filtro de mediana
            _medianFilterBuffer.Dequeue();
            _medianFilterBuffer.Enqueue(buffer[offset + i]);
        }

        return samplesRead;
    }
}