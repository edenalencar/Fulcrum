using NAudio.Dsp;
using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Representa uma banda de equalização
/// </summary>
public class EqualizerBand
{
    /// <summary>
    /// Frequência central da banda em Hz
    /// </summary>
    public float Frequency { get; }
    
    /// <summary>
    /// Largura de banda (Q)
    /// </summary>
    public float Bandwidth { get; }
    
    /// <summary>
    /// Ganho em dB (-15 a +15, onde 0 é neutro)
    /// </summary>
    public float Gain { get; set; }
    
    /// <summary>
    /// Nome da banda (ex: "Baixa", "Média", "Alta")
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Inicializa uma nova banda de equalização
    /// </summary>
    /// <param name="frequency">Frequência central em Hz</param>
    /// <param name="bandwidth">Largura de banda (Q)</param>
    /// <param name="gain">Ganho inicial em dB</param>
    /// <param name="name">Nome descritivo da banda</param>
    public EqualizerBand(float frequency, float bandwidth, float gain, string name)
    {
        Frequency = frequency;
        Bandwidth = bandwidth;
        Gain = gain;
        Name = name;
    }
}

/// <summary>
/// Implementa um equalizador de áudio de 3 bandas
/// </summary>
public class EqualizadorAudio : ISampleProvider, IDisposable
{
    private readonly ISampleProvider _source;
    private readonly EqualizerBand[] _bands;
    
    // Filtros para cada banda
    private readonly BiQuadFilter[] _filters;
    
    // Buffer temporário para processamento
    private float[] _sampleBuffer;
    
    /// <summary>
    /// Inicializa um novo equalizador de áudio
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    /// <param name="bands">Bandas de equalização</param>
    public EqualizadorAudio(ISampleProvider source, EqualizerBand[] bands)
    {
        _source = source;
        _bands = bands;
        WaveFormat = source.WaveFormat;
        
        // Inicializa os filtros
        _filters = new BiQuadFilter[_bands.Length];
        for (int i = 0; i < _bands.Length; i++)
        {
            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _bands[i].Frequency, _bands[i].Bandwidth, GainToAmplitude(_bands[i].Gain));
        }
        
        // Inicializa o buffer de processamento
        _sampleBuffer = new float[4096];
    }
    
    /// <summary>
    /// Inicializa um novo equalizador de áudio com bandas padrão
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public EqualizadorAudio(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        
        // Cria bandas padrão (baixa, média e alta frequência)
        _bands = new EqualizerBand[]
        {
            new EqualizerBand(100, 1.0f, 0, "Baixa"),
            new EqualizerBand(1000, 1.0f, 0, "Média"),
            new EqualizerBand(8000, 1.0f, 0, "Alta")
        };
        
        // Inicializa os filtros
        _filters = new BiQuadFilter[_bands.Length];
        for (int i = 0; i < _bands.Length; i++)
        {
            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _bands[i].Frequency, _bands[i].Bandwidth, GainToAmplitude(_bands[i].Gain));
        }
        
        // Inicializa o buffer de processamento
        _sampleBuffer = new float[4096];
    }
    
    /// <summary>
    /// Inicializa um novo equalizador de áudio com bandas padrão
    /// </summary>
    public EqualizadorAudio()
    {
        // Construtor padrão para quando o GerenciadorEfeitos não tiver uma fonte externa
        WaveFormat = new WaveFormat(44100, 1);
        _bands = new EqualizerBand[]
        {
            new EqualizerBand(100, 1.0f, 0, "Baixa"),
            new EqualizerBand(1000, 1.0f, 0, "Média"),
            new EqualizerBand(8000, 1.0f, 0, "Alta")
        };
        
        _filters = new BiQuadFilter[_bands.Length];
        for (int i = 0; i < _bands.Length; i++)
        {
            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _bands[i].Frequency, _bands[i].Bandwidth, GainToAmplitude(_bands[i].Gain));
        }
        
        _sampleBuffer = new float[4096];
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }
    
    /// <summary>
    /// Obtém as bandas de equalização
    /// </summary>
    public EqualizerBand[] Bands => _bands;
    
    /// <summary>
    /// Atualiza os coeficientes do filtro para uma banda específica
    /// </summary>
    /// <param name="bandIndex">Índice da banda</param>
    public void UpdateBand(int bandIndex)
    {
        if (bandIndex >= 0 && bandIndex < _bands.Length)
        {
            var band = _bands[bandIndex];
            _filters[bandIndex] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, band.Frequency, band.Bandwidth, GainToAmplitude(band.Gain));
        }
    }
    
    /// <summary>
    /// Atualiza os coeficientes de todos os filtros de banda
    /// </summary>
    public void UpdateAllBands()
    {
        for (int i = 0; i < _bands.Length; i++)
        {
            UpdateBand(i);
        }
    }
    
    /// <summary>
    /// Converte um valor de ganho em dB para um fator de amplitude
    /// </summary>
    /// <param name="dB">Ganho em dB</param>
    /// <returns>Fator de amplitude</returns>
    private float GainToAmplitude(float dB)
    {
        return (float)Math.Pow(10, dB / 20);
    }
    
    /// <summary>
    /// Processa amostras de áudio aplicando o equalizador
    /// </summary>
    /// <param name="buffer">Buffer de amostras</param>
    /// <param name="offset">Offset no buffer</param>
    /// <param name="count">Número de amostras a processar</param>
    public void ProcessInPlace(float[] buffer, int offset, int count)
    {
        // Copia as amostras para o buffer temporário
        if (_sampleBuffer.Length < count)
        {
            _sampleBuffer = new float[count];
        }
        Array.Copy(buffer, offset, _sampleBuffer, 0, count);
        
        // Aplica os filtros em série
        for (int band = 0; band < _filters.Length; band++)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = _filters[band].Process(_sampleBuffer[i]);
            }
            
            // Se não for a última banda, copia para o buffer temporário para o próximo filtro
            if (band < _filters.Length - 1)
            {
                Array.Copy(buffer, offset, _sampleBuffer, 0, count);
            }
        }
    }
    
    /// <summary>
    /// Lê amostras de áudio e aplica o equalizador
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        if (_source == null) return 0;
        
        int samplesRead = _source.Read(buffer, offset, count);
        
        if (samplesRead > 0)
        {
            ProcessInPlace(buffer, offset, samplesRead);
        }
        
        return samplesRead;
    }
    
    /// <summary>
    /// Reseta todos os filtros
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < _bands.Length; i++)
        {
            _bands[i].Gain = 0;
            UpdateBand(i);
        }
    }
    
    /// <summary>
    /// Libera os recursos utilizados pelo equalizador
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Libera recursos do equalizador, se houver algum específico
            System.Diagnostics.Debug.WriteLine("Recursos do equalizador liberados com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao liberar recursos do equalizador: {ex.Message}");
        }
    }
}