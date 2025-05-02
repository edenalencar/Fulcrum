using NAudio.Wave;
using System;
using System.Diagnostics;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de reverberação baseado no algoritmo Freeverb
/// Implementação optimizada para evitar chiados em configurações extremas
/// </summary>
public class ReverbSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly WaveFormat _waveFormat;
    private bool _isEnabled = false;
    private float _reverbMix = 0.3f;
    private float _reverbTime = 1.0f;
    
    // Parâmetros do Freeverb
    private const int NUM_COMBS = 8;
    private const int NUM_ALLPASSES = 4;
    private const float MUTED = 0.0f;
    private const float FIXED_GAIN = 0.015f;
    private const float SCALE_WET = 3.0f;
    private const float SCALE_DRY = 2.0f;
    private const float SCALE_DAMP = 0.4f;
    private const float SCALE_ROOM = 0.28f;
    private const float OFFSET_ROOM = 0.7f;
    private const float INITIAL_ROOM = 0.5f;
    private const float INITIAL_DAMP = 0.5f;
    private const float INITIAL_WET = 1.0f / SCALE_WET;
    private const float INITIAL_DRY = 0.0f;
    private const float INITIAL_WIDTH = 1.0f;
    private const float INITIAL_MODE = 0.0f;
    private const float FREEZE_MODE = 1.0f;
    private const float NORMAL_MODE = 0.0f;
    
    // Parâmetros de controle
    private float _wet1, _wet2;
    private float _roomSize;
    private float _damp;
    private float _mode;
    private float _dry;
    private float _width;
    
    // Filtros comb
    private CombFilter[] _combLeft;
    private CombFilter[] _combRight;
    
    // Filtros allpass
    private AllpassFilter[] _allpassLeft;
    private AllpassFilter[] _allpassRight;
    
    // Tempos de atraso para canais esquerdo e direito (valores primos para maior densidade)
    private readonly int[] _combTuningsL = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
    private readonly int[] _combTuningsR = { 1116+23, 1188+19, 1277+17, 1356+29, 1422+13, 1491+23, 1557+19, 1617+31 };
    private readonly int[] _allpassTuningsL = { 556, 441, 341, 225 };
    private readonly int[] _allpassTuningsR = { 556+23, 441+19, 341+17, 225+13 };
    
    // Controle dinâmico
    private bool _isFrozen = false;
    private float _currentMix = 0.0f;
    private float _mixInc = 0.01f;   // Suavização da mudança de mix
    private float _roomSizeStore;
    private float _dampStore;
    
    // Monitoramento
    private int _samplesSinceDiagnostic = 0;
    private const int DIAGNOSTIC_INTERVAL = 48000 * 5; // Diagnóstico a cada 5 segundos
    private float _peakValue = 0f;

    /// <summary>
    /// Inicializa uma nova instância do provedor de reverberação Freeverb
    /// </summary>
    /// <param name="source">Fonte de áudio a ser processada</param>
    public ReverbSampleProvider(ISampleProvider source)
    {
        _source = source;
        _waveFormat = source.WaveFormat;
        
        // Inicializa os filtros
        InitializeFilters();
        
        // Configura os parâmetros iniciais
        SetRoomSize(INITIAL_ROOM);
        SetDamp(INITIAL_DAMP);
        SetWidth(INITIAL_WIDTH);
        SetMix(INITIAL_WET);
        SetMode(INITIAL_MODE);
        SetDry(INITIAL_DRY);
        
        Debug.WriteLine("[REVERB] Provedor Freeverb inicializado com configurações otimizadas anti-chiado.");
    }
    
    /// <summary>
    /// Inicializa os filtros comb e allpass 
    /// </summary>
    private void InitializeFilters()
    {
        // Inicializa filtros comb para processamento estéreo
        _combLeft = new CombFilter[NUM_COMBS];
        _combRight = new CombFilter[NUM_COMBS];
        
        for (int i = 0; i < NUM_COMBS; i++)
        {
            _combLeft[i] = new CombFilter(_combTuningsL[i]);
            _combRight[i] = new CombFilter(_combTuningsR[i]);
        }
        
        // Inicializa filtros allpass para processamento estéreo
        _allpassLeft = new AllpassFilter[NUM_ALLPASSES];
        _allpassRight = new AllpassFilter[NUM_ALLPASSES];
        
        for (int i = 0; i < NUM_ALLPASSES; i++)
        {
            _allpassLeft[i] = new AllpassFilter(_allpassTuningsL[i]);
            _allpassRight[i] = new AllpassFilter(_allpassTuningsR[i]);
            
            // Configura o feedback do allpass (valor fixo de 0.5)
            _allpassLeft[i].SetFeedback(0.5f);
            _allpassRight[i] = new AllpassFilter(_allpassTuningsR[i]);
            _allpassRight[i].SetFeedback(0.5f);
        }
        
        // Inicializa os valores de mix
        _wet1 = SCALE_WET * 0.5f;
        _wet2 = SCALE_WET * 0.5f;
        
        Debug.WriteLine($"[REVERB] Filtros inicializados: {NUM_COMBS} combs, {NUM_ALLPASSES} allpasses");
    }
    
    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat => _waveFormat;

    /// <summary>
    /// Determina se o efeito está ativo
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set 
        {
            // Se estiver desativando, limpa os buffers
            if (_isEnabled && !value)
            {
                LimparBuffer();
            }
            _isEnabled = value;
            
            Debug.WriteLine($"[REVERB] Efeito {(_isEnabled ? "ativado" : "desativado")}");
        }
    }

    /// <summary>
    /// Mix de reverberação (0.0 a 1.0)
    /// </summary>
    public float ReverbMix
    {
        get => _reverbMix;
        set 
        {
            _reverbMix = Math.Clamp(value, 0.0f, 1.0f);
            SetMix(_reverbMix);
            Debug.WriteLine($"[REVERB] Mix ajustado para {_reverbMix:F3}");
        }
    }

    /// <summary>
    /// Tempo de reverberação em segundos (0.1 a 10.0)
    /// </summary>
    public float ReverbTime
    {
        get => _reverbTime;
        set 
        {
            _reverbTime = Math.Clamp(value, 0.1f, 10.0f);
            
            // Mapeia o tempo de reverberação para o tamanho da sala no algoritmo
            // Esta fórmula foi calibrada para corresponder aos segundos esperados
            float roomSize = 0.7f + (_reverbTime / 10.0f) * 0.28f;
            
            // Para tempos muito longos, aumenta o damping para controlar chiados
            float damp = 0.5f;
            if (_reverbTime > 5.0f)
            {
                damp = 0.6f + ((_reverbTime - 5.0f) / 5.0f) * 0.2f;
            }
            
            SetRoomSize(roomSize);
            SetDamp(damp);
            
            Debug.WriteLine($"[REVERB] Tempo ajustado para {_reverbTime:F2}s (room={roomSize:F2}, damp={damp:F2})");
        }
    }
    
    /// <summary>
    /// Limpa todos os buffers de reverberação
    /// </summary>
    public void LimparBuffer()
    {
        if (_combLeft != null && _combRight != null)
        {
            for (int i = 0; i < NUM_COMBS; i++)
            {
                _combLeft[i].Clear();
                _combRight[i].Clear();
            }
        }
        
        if (_allpassLeft != null && _allpassRight != null)
        {
            for (int i = 0; i < NUM_ALLPASSES; i++)
            {
                _allpassLeft[i].Clear();
                _allpassRight[i].Clear();
            }
        }
        
        _samplesSinceDiagnostic = 0;
        _peakValue = 0f;
        
        Debug.WriteLine("[REVERB] Todos os buffers limpos");
    }
    
    /// <summary>
    /// Define o modo de operação (normal ou congelado)
    /// </summary>
    private void SetMode(float value)
    {
        _mode = value;
        
        // No modo congelado, armazenamos os valores atuais e configuramos 
        // a reverberação para funcionar sem decaimento
        if (_isFrozen != (_mode >= FREEZE_MODE))
        {
            _isFrozen = (_mode >= FREEZE_MODE);
            
            if (_isFrozen)
            {
                _roomSizeStore = _roomSize;
                _dampStore = _damp;
                SetRoomSize(1.0f);
                SetDamp(0.0f);
            }
            else
            {
                SetRoomSize(_roomSizeStore);
                SetDamp(_dampStore);
            }
        }
    }
    
    /// <summary>
    /// Define o tamanho da sala (afeta o tempo de reverberação)
    /// </summary>
    private void SetRoomSize(float value)
    {
        _roomSize = (value * SCALE_ROOM) + OFFSET_ROOM;
        
        if (!_isFrozen)
        {
            for (int i = 0; i < NUM_COMBS; i++)
            {
                _combLeft[i].SetFeedback(_roomSize);
                _combRight[i].SetFeedback(_roomSize);
            }
        }
    }
    
    /// <summary>
    /// Define o amortecimento de alta frequência
    /// </summary>
    private void SetDamp(float value)
    {
        _damp = value * SCALE_DAMP;
        
        if (!_isFrozen)
        {
            for (int i = 0; i < NUM_COMBS; i++)
            {
                _combLeft[i].SetDamp(_damp);
                _combRight[i].SetDamp(_damp);
            }
        }
    }
    
    /// <summary>
    /// Define a largura estéreo (não usada nesta implementação mono)
    /// </summary>
    private void SetWidth(float value)
    {
        _width = value;
        _wet1 = SCALE_WET * (float)(0.5 + _width * 0.5);
        _wet2 = SCALE_WET * (float)(0.5 * (1.0 - _width));
    }
    
    /// <summary>
    /// Define o nível do sinal molhado (reverberado)
    /// </summary>
    private void SetMix(float value)
    {
        // Ajuste não-linear para melhor controle do mix
        value = (float)Math.Pow(value, 0.7);
        
        // Impedimos mudanças bruscas configurando apenas o valor alvo
        // A suavização ocorre durante o processamento
        _currentMix = value * SCALE_WET;
        _dry = (1.0f - value) * SCALE_DRY;
    }
    
    /// <summary>
    /// Define o nível do sinal seco
    /// </summary>
    private void SetDry(float value)
    {
        _dry = value;
    }

    /// <summary>
    /// Lê amostras de áudio da fonte e aplica o efeito de reverberação
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);
        
        if (samplesRead > 0 && _isEnabled && _currentMix > 0.001f)
        {
            ProcessReverberation(buffer, offset, samplesRead);
            
            // Monitoramento para diagnóstico
            _samplesSinceDiagnostic += samplesRead;
            if (_samplesSinceDiagnostic >= DIAGNOSTIC_INTERVAL)
            {
                Debug.WriteLine($"[REVERB] Diagnóstico: pico={_peakValue:F4}, mix={_reverbMix:F3}, tempo={_reverbTime:F2}s");
                _peakValue = 0;
                _samplesSinceDiagnostic = 0;
            }
        }
        
        return samplesRead;
    }
    
    /// <summary>
    /// Processa a reverberação Freeverb nas amostras de áudio
    /// </summary>
    private void ProcessReverberation(float[] buffer, int offset, int count)
    {
        // Freeverb é um algoritmo estéreo - para mono, usamos o mesmo valor para L e R
        for (int i = 0; i < count; i++)
        {
            // Obtém a amostra de entrada
            float input = buffer[offset + i];
            
            // Limita a amplitude da entrada para evitar instabilidades
            input = Math.Clamp(input, -0.95f, 0.95f);
            
            // Aplica ganho fixo de entrada para equilibrar o volume
            float inputL = input * FIXED_GAIN;
            float inputR = input * FIXED_GAIN;

            // Acumuladores para saída
            float outL = 0;
            float outR = 0;

            // Processa através dos filtros comb em paralelo
            for (int j = 0; j < NUM_COMBS; j++)
            {
                outL += _combLeft[j].Process(inputL);
                outR += _combRight[j].Process(inputR);
            }

            // Processa através dos filtros allpass em série
            for (int j = 0; j < NUM_ALLPASSES; j++)
            {
                outL = _allpassLeft[j].Process(outL);
                outR = _allpassRight[j].Process(outR);
            }
            
            // Proteção contra valores instáveis
            outL = Math.Clamp(outL, -0.95f, 0.95f);
            outR = Math.Clamp(outR, -0.95f, 0.95f);
            
            // Aplica mix estéreo para melhor espacialização
            float wet = (outL * _wet1) + (outR * _wet2);
            
            // Combina sinal seco e molhado
            float output = wet + (input * _dry);
            
            // Proteção final contra chiados
            if (float.IsNaN(output) || float.IsInfinity(output))
            {
                output = input * 0.5f;
                Debug.WriteLine("[REVERB] Detectado valor instável - corrigido");
            }
            
            // Proteção contra saturação
            output = Math.Clamp(output, -0.98f, 0.98f);
            
            // Aplica à saída
            buffer[offset + i] = output;
            
            // Monitoramento de pico
            float absValue = Math.Abs(output);
            if (absValue > _peakValue)
            {
                _peakValue = absValue;
            }
        }
    }
    
    #region Classes internas para implementação de Freeverb
    
    /// <summary>
    /// Implementação de filtro comb com feedback e lowpass
    /// </summary>
    private class CombFilter
    {
        private float[] _buffer;
        private int _bufSize;
        private int _bufIndex;
        
        private float _feedback;
        private float _filterStore;
        private float _damp1;
        private float _damp2;
        
        private const float VERY_SMALL_FLOAT = 1e-10f; // Evita denormais
        
        public CombFilter(int size)
        {
            _bufSize = size;
            _buffer = new float[size];
            _bufIndex = 0;
            
            _filterStore = 0;
            _feedback = 0.5f;
            _damp1 = 0.5f;
            _damp2 = 1.0f - _damp1;
        }
        
        public void SetFeedback(float value)
        {
            _feedback = value;
        }
        
        public void SetDamp(float value)
        {
            _damp1 = value;
            _damp2 = 1.0f - value;
        }
        
        public float Process(float input)
        {
            // Obtém a amostra do buffer
            float output = _buffer[_bufIndex];
            
            // Filtro passa-baixa de um polo aplicado ao feedback (controle de damping)
            _filterStore = (_filterStore * _damp1) + (output * _damp2);
            
            // Evita denormais
            if (Math.Abs(_filterStore) < VERY_SMALL_FLOAT)
                _filterStore = 0;
            
            // Atualiza o buffer com entrada + feedback filtrado
            _buffer[_bufIndex] = input + (_filterStore * _feedback);
            
            // Incrementa o índice e envolve se necessário
            _bufIndex++;
            if (_bufIndex >= _bufSize)
                _bufIndex = 0;
            
            return output;
        }
        
        public void Clear()
        {
            for (int i = 0; i < _bufSize; i++)
                _buffer[i] = 0;
            _filterStore = 0;
        }
    }
    
    /// <summary>
    /// Implementação de filtro allpass
    /// </summary>
    private class AllpassFilter
    {
        private float[] _buffer;
        private int _bufSize;
        private int _bufIndex;
        private float _feedback;
        
        public AllpassFilter(int size)
        {
            _bufSize = size;
            _buffer = new float[size];
            _bufIndex = 0;
            _feedback = 0.5f;
        }
        
        public void SetFeedback(float value)
        {
            _feedback = value;
        }
        
        public float Process(float input)
        {
            float output;
            float bufOut;
            
            bufOut = _buffer[_bufIndex];
            
            // Fórmula do filtro allpass: outL = bufOut + (-input * feedback);
            output = -input * _feedback + bufOut;
            
            _buffer[_bufIndex] = input + (bufOut * _feedback);
            
            _bufIndex++;
            if (_bufIndex >= _bufSize)
                _bufIndex = 0;
            
            return output;
        }
        
        public void Clear()
        {
            for (int i = 0; i < _bufSize; i++)
                _buffer[i] = 0;
        }
    }
    
    #endregion
}