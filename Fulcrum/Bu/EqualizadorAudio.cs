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
    // Alterando para usar inicialização em construtor em vez de 'required'
    private readonly ISampleProvider _source;
    private readonly EqualizerBand[] _bands;
    
    // Filtros para cada banda
    private readonly BiQuadFilter[] _filters;
    
    // Buffer temporário para processamento
    private float[] _sampleBuffer;
    
    // Flag para depuração
    private bool _debugMode = true;
    
    // Contador de amostras processadas
    private long _totalSamplesProcessed = 0;
    private readonly long _sampleReportInterval = 44100 * 5; // Reportar a cada 5 segundos de áudio
    
    /// <summary>
    /// Inicializa um novo equalizador de áudio
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    /// <param name="bands">Bandas de equalização</param>
    public EqualizadorAudio(ISampleProvider source, EqualizerBand[] bands)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _bands = bands ?? throw new ArgumentNullException(nameof(bands));
        WaveFormat = source.WaveFormat;
        
        // Inicializa os filtros
        _filters = new BiQuadFilter[_bands.Length];
        for (int i = 0; i < _bands.Length; i++)
        {
            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _bands[i].Frequency, _bands[i].Bandwidth, GainToAmplitude(_bands[i].Gain));
            System.Diagnostics.Debug.WriteLine($"[EQ] Inicializado filtro para banda {i}: Freq={_bands[i].Frequency}Hz, Gain={_bands[i].Gain}dB -> Amplitude={GainToAmplitude(_bands[i].Gain):F3}");
        }
        
        // Inicializa o buffer de processamento
        _sampleBuffer = new float[4096];
        
        System.Diagnostics.Debug.WriteLine($"[EQ] Equalizador inicializado com {_bands.Length} bandas, formato: {WaveFormat}");
    }
    
    /// <summary>
    /// Inicializa um novo equalizador de áudio com bandas padrão
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public EqualizadorAudio(ISampleProvider source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        WaveFormat = source.WaveFormat;
        
        // Cria bandas padrão (baixa, média e alta frequência)
        // Utilizando valores de Q (bandwidth) mais precisos para cada faixa
        _bands = new EqualizerBand[]
        {
            new EqualizerBand(100, 0.8f, 0, "Baixa"),    // Banda mais estreita para graves
            new EqualizerBand(1000, 1.2f, 0, "Média"),   // Banda média para frequências médias
            new EqualizerBand(8000, 1.5f, 0, "Alta")     // Banda mais larga para agudos
        };
        
        // Inicializa os filtros
        _filters = new BiQuadFilter[_bands.Length];
        for (int i = 0; i < _bands.Length; i++)
        {
            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _bands[i].Frequency, _bands[i].Bandwidth, GainToAmplitude(_bands[i].Gain));
            System.Diagnostics.Debug.WriteLine($"[EQ] Inicializado filtro para banda {i}: Freq={_bands[i].Frequency}Hz, Q={_bands[i].Bandwidth}, Gain={_bands[i].Gain}dB -> Amplitude={GainToAmplitude(_bands[i].Gain):F3}");
        }
        
        // Inicializa o buffer de processamento
        _sampleBuffer = new float[4096];
        
        System.Diagnostics.Debug.WriteLine($"[EQ] Equalizador inicializado com bandas padrão, formato: {WaveFormat}");
    }
    
    /// <summary>
    /// Inicializa um novo equalizador de áudio com bandas padrão
    /// </summary>
    public EqualizadorAudio()
    {
        // Construtor padrão para quando o GerenciadorEfeitos não tiver uma fonte externa
        _source = null!;
        WaveFormat = new WaveFormat(44100, 1);
        _bands = new EqualizerBand[]
        {
            new EqualizerBand(100, 0.8f, 0, "Baixa"),    // Banda mais estreita para graves
            new EqualizerBand(1000, 1.2f, 0, "Média"),   // Banda média para frequências médias
            new EqualizerBand(8000, 1.5f, 0, "Alta")     // Banda mais larga para agudos
        };
        
        _filters = new BiQuadFilter[_bands.Length];
        for (int i = 0; i < _bands.Length; i++)
        {
            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _bands[i].Frequency, _bands[i].Bandwidth, GainToAmplitude(_bands[i].Gain));
        }
        
        _sampleBuffer = new float[4096];
        
        System.Diagnostics.Debug.WriteLine("[EQ] Equalizador inicializado sem fonte de áudio (construtor padrão)");
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
            float amplitude = GainToAmplitude(band.Gain);
            _filters[bandIndex] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, band.Frequency, band.Bandwidth, amplitude);
            
            System.Diagnostics.Debug.WriteLine($"[EQ] Banda {bandIndex} ({band.Name}) atualizada: Freq={band.Frequency}Hz, Gain={band.Gain}dB -> Amplitude={amplitude:F3}");
            
            // Verifica se o ganho é significativo para logs diagnósticos
            if (Math.Abs(band.Gain) > 10)
            {
                System.Diagnostics.Debug.WriteLine($"[EQ] ATENÇÃO: Banda {bandIndex} configurada com ganho extremo ({band.Gain}dB)");
            }
        }
    }
    
    /// <summary>
    /// Atualiza os coeficientes de todos os filtros de banda
    /// </summary>
    public void UpdateAllBands()
    {
        System.Diagnostics.Debug.WriteLine("[EQ] Atualizando todas as bandas do equalizador");
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
        try
        {
            // Verifica se há valores extremos no buffer para diagnóstico
            if (_debugMode)
            {
                float maxInput = 0;
                for (int i = 0; i < count; i++)
                {
                    maxInput = Math.Max(maxInput, Math.Abs(buffer[offset + i]));
                }
                
                if (_totalSamplesProcessed % _sampleReportInterval == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[EQ] Processando {count} amostras - Valor máximo na entrada: {maxInput:F3}");
                }
            }
            
            // Redimensiona o buffer temporário se necessário
            if (_sampleBuffer.Length < count)
            {
                _sampleBuffer = new float[count];
                System.Diagnostics.Debug.WriteLine($"[EQ] Buffer temporário redimensionado para {count} amostras");
            }
            
            // Preserva o áudio original
            Array.Copy(buffer, offset, _sampleBuffer, 0, count);
            
            // Conta quantas bandas estão ativas para ajustar o processamento
            int activeBands = 0;
            for (int i = 0; i < _bands.Length; i++)
            {
                if (Math.Abs(_bands[i].Gain) >= 0.1f)
                {
                    activeBands++;
                }
            }
            
            // Se nenhuma banda está ativa, mantenha o áudio original
            if (activeBands == 0)
            {
                return; // Mantém o buffer original intacto
            }
            
            // ABORDAGEM COMPLETAMENTE NOVA: Filtro em série para evitar interferências
            // Cada filtro processa o resultado do anterior, começando com o áudio original
            
            // Aplicando os filtros em sequência (processamento em série)
            for (int band = 0; band < _filters.Length; band++)
            {
                // Pula bandas com ganho zero ou muito próximo de zero
                if (Math.Abs(_bands[band].Gain) < 0.1f)
                    continue;
                
                // Processamento de banda para cada amostra
                float gain = _bands[band].Gain;
                BiQuadFilter filter = _filters[band];
                
                // Processa as amostras uma a uma
                for (int i = 0; i < count; i++)
                {
                    // Aplica o filtro ao sinal atual, já potencialmente modificado por outros filtros
                    buffer[offset + i] = filter.Process(buffer[offset + i]);
                }
                
                if (_debugMode && _totalSamplesProcessed % _sampleReportInterval == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[EQ] Banda {band} ({_bands[band].Name}) aplicada com ganho {gain}dB");
                }
            }
            
            // Logs de diagnóstico para valores extremos na saída
            if (_debugMode)
            {
                float maxOutput = 0;
                for (int i = 0; i < count; i++)
                {
                    maxOutput = Math.Max(maxOutput, Math.Abs(buffer[offset + i]));
                }
                
                if (_totalSamplesProcessed % _sampleReportInterval == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[EQ] Valor máximo na saída: {maxOutput:F3}");
                    
                    // Imprime o estado atual das bandas
                    for (int i = 0; i < _bands.Length; i++)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EQ] Banda {i} ({_bands[i].Name}): Gain={_bands[i].Gain:F1}dB");
                    }
                }
                
                _totalSamplesProcessed += count;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EQ] ERRO durante processamento: {ex.Message}");
            // Em caso de erro, preserva o áudio original
            Array.Copy(_sampleBuffer, 0, buffer, offset, count);
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
            // Verifica se há bandas com ganho diferente de zero
            bool hasActiveEQ = false;
            for (int i = 0; i < _bands.Length; i++)
            {
                if (Math.Abs(_bands[i].Gain) >= 0.1f)
                {
                    hasActiveEQ = true;
                    break;
                }
            }
            
            // Só processa se alguma banda estiver com ganho
            if (hasActiveEQ)
            {
                try
                {
                    ProcessInPlace(buffer, offset, samplesRead);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EQ] ERRO ao processar áudio: {ex.Message}");
                }
            }
            else if (_totalSamplesProcessed % _sampleReportInterval == 0 && _debugMode)
            {
                System.Diagnostics.Debug.WriteLine("[EQ] Equalizador está ativo mas todas as bandas têm ganho próximo de zero - sem processamento");
            }
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
        System.Diagnostics.Debug.WriteLine("[EQ] Todas as bandas do equalizador resetadas para 0dB");
    }
    
    /// <summary>
    /// Define se o modo de depuração está ativo
    /// </summary>
    public bool DebugMode
    {
        get => _debugMode;
        set => _debugMode = value;
    }
    
    /// <summary>
    /// Aplica uma configuração de teste para diagnosticar se o equalizador está funcionando
    /// </summary>
    public void ApplyTestConfiguration()
    {
        // Define ganhos extremos para facilitar a verificação auditiva
        _bands[0].Gain = -12.0f; // Reduz graves drasticamente
        _bands[1].Gain = 0.0f;   // Mantém médios neutros
        _bands[2].Gain = 12.0f;  // Aumenta agudos drasticamente
        
        UpdateAllBands();
        
        System.Diagnostics.Debug.WriteLine("[EQ] Configuração de teste aplicada: Graves=-12dB, Médios=0dB, Agudos=+12dB");
    }
    
    /// <summary>
    /// Libera os recursos utilizados pelo equalizador
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Libera recursos do equalizador, se houver algum específico
            System.Diagnostics.Debug.WriteLine("[EQ] Recursos do equalizador liberados com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EQ] Erro ao liberar recursos do equalizador: {ex.Message}");
        }
    }
}