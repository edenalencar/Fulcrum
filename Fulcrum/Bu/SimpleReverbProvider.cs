using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de reverberação simples baseado em linhas de atraso (Schroeder Reverberator)
/// com redução de chiado
/// </summary>
public class SimpleReverbProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float _decayTime = 1.0f;
    private float _mix = 0.3f;
    private bool _isEnabled = false;
    private readonly int _sampleRate;
    
    // Parâmetros de reverberação
    private const int NUM_COMBS = 4;      // Número de filtros comb
    private const int NUM_ALLPASSES = 2;  // Número de filtros allpass
    
    // Linhas de atraso para filtros comb
    private float[][] _combBuffers;
    private int[] _combLengths;
    private int[] _combPositions;
    private float[] _combFeedback;
    
    // Filtros passa-baixa para cada filtro comb
    private float[] _lpfValues;          // Estado atual do filtro
    private float[] _lpfCoeffs;          // Coeficientes por filtro
    private const float LP_BASE = 0.5f;   // Coeficiente base do filtro passa-baixa
    
    // Linhas de atraso para filtros allpass
    private float[][] _allpassBuffers;
    private int[] _allpassLengths;
    private int[] _allpassPositions;
    private float _allpassFeedback = 0.5f;
    
    // Filtro de engenharia para redução de ruído
    private const float NOISE_THRESHOLD = 0.0001f; // -80dB
    private const float DENORMAL_FIX = 1e-10f;    // Valor pequeno para evitar denormais
    
    // Pré-atraso para criar sensação de espaço
    private int _preDelayLength;
    private float[] _preDelayBuffer;
    private int _preDelayPos;
    
    // Coeficientes de damping (amortecimento) para controle de altas frequências
    private float _dampingCoeff = 0.2f;
    
    // Filtro de saída para suavização final
    private float _outputLpfValue = 0.0f;
    private float _outputLpfCoeff = 0.7f;
    
    // Para monitoramento de qualidade
    private float _peakValue = 0f;
    private int _monitorCounter = 0;

    /// <summary>
    /// Inicializa uma nova instância do provedor de reverberação sem chiado
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public SimpleReverbProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        _sampleRate = WaveFormat.SampleRate;
        
        // Inicializa os componentes de reverberação
        InitializeReverb();
        
        // Imprime informações de diagnóstico
        System.Diagnostics.Debug.WriteLine($"[REVERB] SimpleReverbProvider v2 (anti-chiado) inicializado com {NUM_COMBS} filtros comb, {NUM_ALLPASSES} filtros allpass");
    }
    
    /// <summary>
    /// Inicializa os componentes de reverberação
    /// </summary>
    private void InitializeReverb()
    {
        // Tempos de atraso (em ms) para filtros comb
        // Valores primos para evitar ressonâncias comuns
        int[] combDelaysMs = { 29, 37, 43, 51 };
        
        // Inicializa filtros comb
        _combLengths = new int[NUM_COMBS];
        _combBuffers = new float[NUM_COMBS][];
        _combPositions = new int[NUM_COMBS];
        _combFeedback = new float[NUM_COMBS];
        
        // Inicializa filtros passa-baixa para cada comb
        _lpfValues = new float[NUM_COMBS];
        _lpfCoeffs = new float[NUM_COMBS];
        
        for (int i = 0; i < NUM_COMBS; i++)
        {
            // Converte ms para amostras
            _combLengths[i] = (_sampleRate * combDelaysMs[i]) / 1000;
            _combBuffers[i] = new float[_combLengths[i]];
            _combPositions[i] = 0;
            
            // Calcula o feedback baseado no tempo de reverberação
            // RT60 = -3 * delay * log10(feedback)
            _combFeedback[i] = CalculateFeedback(_combLengths[i], _decayTime);
            
            // Configura o filtro passa-baixa para este comb com valor levemente diferente
            // para criar dispersão espectral (reduz ressonâncias)
            _lpfCoeffs[i] = LP_BASE + (0.05f * i);
        }
        
        // Tempos de atraso (em ms) para filtros allpass
        int[] allpassDelaysMs = { 11, 17 }; // Valores primos menores
        
        // Inicializa filtros allpass
        _allpassLengths = new int[NUM_ALLPASSES];
        _allpassBuffers = new float[NUM_ALLPASSES][];
        _allpassPositions = new int[NUM_ALLPASSES];
        
        for (int i = 0; i < NUM_ALLPASSES; i++)
        {
            // Converte ms para amostras
            _allpassLengths[i] = (_sampleRate * allpassDelaysMs[i]) / 1000;
            _allpassBuffers[i] = new float[_allpassLengths[i]];
            _allpassPositions[i] = 0;
        }
        
        // Configura o pré-atraso
        _preDelayLength = _sampleRate / 25; // 40ms
        _preDelayBuffer = new float[_preDelayLength];
        _preDelayPos = 0;
        
        // Ajusta parâmetros de damping baseado na taxa de amostragem
        AjustarDamping();
    }
    
    /// <summary>
    /// Calcula o coeficiente de feedback seguro para evitar instabilidades
    /// </summary>
    private float CalculateFeedback(int delaySamples, float reverbTime)
    {
        // Fórmula baseada no tempo de reverberação (RT60)
        float rawFeedback = (float)Math.Pow(10.0, (-3.0 * delaySamples) / (-reverbTime * _sampleRate));
        
        // Aplicar limite de segurança para evitar instabilidade
        return Math.Min(rawFeedback, 0.7f);
    }
    
    /// <summary>
    /// Ajusta os parâmetros de damping baseado na taxa de amostragem
    /// </summary>
    private void AjustarDamping()
    {
        // Ajuste de damping baseado na taxa de amostragem
        // Taxas mais altas precisam de mais damping para evitar chiados
        if (_sampleRate >= 96000)
        {
            _dampingCoeff = 0.25f;
            _outputLpfCoeff = 0.75f;
        }
        else if (_sampleRate >= 48000)
        {
            _dampingCoeff = 0.2f;
            _outputLpfCoeff = 0.7f;
        }
        else
        {
            _dampingCoeff = 0.15f;
            _outputLpfCoeff = 0.65f;
        }
    }
    
    /// <summary>
    /// Aplica um filtro passa-baixa a uma amostra
    /// </summary>
    private float ApplyLowpass(float input, ref float state, float coeff)
    {
        // Filtro IIR simples: y[n] = coeff * y[n-1] + (1-coeff) * x[n]
        state = (coeff * state) + ((1.0f - coeff) * input);
        
        // Evita valores denormais que causam chiados
        if (Math.Abs(state) < NOISE_THRESHOLD)
        {
            state = 0;
        }
        
        return state;
    }
    
    /// <summary>
    /// Atualiza os coeficientes de feedback com base no tempo de decaimento
    /// </summary>
    private void UpdateDecayTime()
    {
        for (int i = 0; i < NUM_COMBS; i++)
        {
            // Recalcula o feedback para o novo tempo de decaimento
            _combFeedback[i] = CalculateFeedback(_combLengths[i], _decayTime);
            
            // Ajusta o coeficiente passa-baixa
            // Reverberações mais longas precisam de maior atenuação nas altas frequências
            _lpfCoeffs[i] = Math.Clamp(LP_BASE + (_decayTime * 0.05f), 0.3f, 0.9f) + (0.03f * i);
        }
        
        // Ajusta o coeficiente de amortecimento com base no tempo de decaimento
        _dampingCoeff = Math.Clamp(0.15f + (_decayTime * 0.05f), 0.15f, 0.4f);
        
        // Ajusta o filtro passa-baixa de saída
        _outputLpfCoeff = Math.Clamp(0.5f + (_decayTime * 0.07f), 0.5f, 0.8f);
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
        set
        {
            // Se estiver desativando, limpa o buffer para evitar resíduos
            if (_isEnabled && !value)
            {
                LimparBuffers();
            }
            _isEnabled = value;
            System.Diagnostics.Debug.WriteLine($"[REVERB] Efeito {(value ? "ativado" : "desativado")}");
        }
    }

    /// <summary>
    /// Tempo de decaimento da reverberação em segundos (0.1 a 5.0)
    /// </summary>
    public float DecayTime
    {
        get => _decayTime;
        set
        {
            // Limita o tempo de decaimento para evitar instabilidades
            float newValue = Math.Clamp(value, 0.1f, 5.0f);
            if (Math.Abs(newValue - _decayTime) > 0.01f)
            {
                _decayTime = newValue;
                UpdateDecayTime();
                System.Diagnostics.Debug.WriteLine($"[REVERB] Tempo de decaimento ajustado para {_decayTime}s");
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
    /// Limpa todos os buffers de reverberação
    /// </summary>
    public void LimparBuffers()
    {
        // Limpa os buffers comb
        if (_combBuffers != null)
        {
            for (int i = 0; i < NUM_COMBS; i++)
            {
                if (_combBuffers[i] != null)
                {
                    Array.Clear(_combBuffers[i], 0, _combBuffers[i].Length);
                }
            }
        }
        
        // Limpa os buffers allpass
        if (_allpassBuffers != null)
        {
            for (int i = 0; i < NUM_ALLPASSES; i++)
            {
                if (_allpassBuffers[i] != null)
                {
                    Array.Clear(_allpassBuffers[i], 0, _allpassBuffers[i].Length);
                }
            }
        }
        
        // Limpa o buffer de pré-atraso
        if (_preDelayBuffer != null)
        {
            Array.Clear(_preDelayBuffer, 0, _preDelayBuffer.Length);
        }
        
        // Reseta os estados dos filtros passa-baixa
        if (_lpfValues != null)
        {
            Array.Clear(_lpfValues, 0, _lpfValues.Length);
        }
        
        _outputLpfValue = 0.0f;
        
        // Reseta o monitoramento
        _peakValue = 0f;
        _monitorCounter = 0;
        
        System.Diagnostics.Debug.WriteLine("[REVERB] Buffers limpos");
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de reverberação
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // Lê amostras da fonte original primeiro
        int samplesRead = _source.Read(buffer, offset, count);
        
        // Se não há amostras ou o efeito está desativado ou mix é zero, retorna o áudio original
        if (samplesRead <= 0 || !_isEnabled || _mix <= 0.001f)
        {
            return samplesRead;
        }

        // Cria uma cópia do buffer original para preservar as amostras originais
        float[] originalSamples = new float[samplesRead];
        Array.Copy(buffer, offset, originalSamples, 0, samplesRead);
        
        // Calcula os fatores de mix
        float dryFactor = 1.0f - (_mix * 0.5f); // Preserva pelo menos 50% do sinal original
        float wetFactor = _mix * 0.5f;          // Sinal molhado é 50% para não sobrepor
        
        // Processamento de cada amostra
        for (int i = 0; i < samplesRead; i++)
        {
            float originalSample = originalSamples[i];
            
            // Limita a amplitude da entrada
            originalSample = Math.Clamp(originalSample, -1.0f, 1.0f);
            
            // Primeiro passa pelo pré-atraso (simula a distância fonte-ouvinte)
            float preDelaySample = _preDelayBuffer[_preDelayPos];
            _preDelayBuffer[_preDelayPos] = originalSample;
            _preDelayPos = (_preDelayPos + 1) % _preDelayLength;
            
            // Aplicar um leve filtro passa-baixa ao sinal de entrada para reduzir agudos
            preDelaySample = preDelaySample * (1.0f - _dampingCoeff);
            
            // Processa através dos filtros comb em paralelo
            float combOutput = 0.0f;
            for (int j = 0; j < NUM_COMBS; j++)
            {
                // Lê a amostra atrasada
                float delaySample = _combBuffers[j][_combPositions[j]];
                
                // Aplica atenuação para evitar ruídos em valores muito baixos
                if (Math.Abs(delaySample) < NOISE_THRESHOLD)
                {
                    delaySample = 0f;
                }
                
                // Calcula o feedback
                float feedback = delaySample * _combFeedback[j];
                
                // Aplica filtro passa-baixa ao feedback para reduzir chiados
                feedback = ApplyLowpass(feedback, ref _lpfValues[j], _lpfCoeffs[j]);
                
                // Calcula a saída do filtro comb (entrada + feedback com atenuação)
                float output = preDelaySample + feedback;
                
                // Armazena no buffer
                _combBuffers[j][_combPositions[j]] = output;
                
                // Avança a posição
                _combPositions[j] = (_combPositions[j] + 1) % _combLengths[j];
                
                // Acumula a saída
                combOutput += delaySample;
            }
            
            // Normaliza e escala a saída dos filtros comb
            combOutput = combOutput / NUM_COMBS * 0.8f;
            
            // Passa pela cadeia de filtros allpass em série
            float allpassOutput = combOutput;
            for (int j = 0; j < NUM_ALLPASSES; j++)
            {
                // Lê a amostra atrasada
                float delaySample = _allpassBuffers[j][_allpassPositions[j]];
                
                // Aplica atenuação para evitar ruídos em valores muito baixos
                if (Math.Abs(delaySample) < NOISE_THRESHOLD)
                {
                    delaySample = 0f;
                }
                
                // Calcula a saída do filtro allpass (algoritmo de Schroeder)
                float feedbackSample = delaySample * _allpassFeedback;
                float output = -allpassOutput + feedbackSample;
                
                // Armazena no buffer (entrada + feedback)
                _allpassBuffers[j][_allpassPositions[j]] = allpassOutput + (delaySample * _allpassFeedback);
                
                // Avança a posição
                _allpassPositions[j] = (_allpassPositions[j] + 1) % _allpassLengths[j];
                
                // Atualiza para o próximo filtro
                allpassOutput = output;
            }
            
            // Aplicar filtro passa-baixa final para suavizar ainda mais o sinal
            allpassOutput = ApplyLowpass(allpassOutput, ref _outputLpfValue, _outputLpfCoeff);
            
            // Limita a amplitude da saída
            float reverbSample = Math.Clamp(allpassOutput * 0.6f, -0.95f, 0.95f);
            
            // Combina sinal original (dry) com o reverberado (wet)
            buffer[offset + i] = (originalSample * dryFactor) + (reverbSample * wetFactor);
            
            // Proteção final contra valores extremos (fallback para o sinal original)
            if (float.IsNaN(buffer[offset + i]) || float.IsInfinity(buffer[offset + i]))
            {
                buffer[offset + i] = originalSample * 0.5f;
            }
            
            // Monitora o nível de saída para diagnóstico
            float absValue = Math.Abs(buffer[offset + i]);
            if (absValue > _peakValue)
            {
                _peakValue = absValue;
            }
        }
        
        // Loga periodicamente os níveis de pico para monitoramento
        _monitorCounter += samplesRead;
        if (_monitorCounter > _sampleRate * 5) // a cada 5 segundos
        {
            System.Diagnostics.Debug.WriteLine($"[REVERB] Monitoramento: Valor de pico={_peakValue:F3}, MIX={_mix:F2}, Decay={_decayTime:F1}s");
            _peakValue = 0;
            _monitorCounter = 0;
        }

        return samplesRead;
    }
}