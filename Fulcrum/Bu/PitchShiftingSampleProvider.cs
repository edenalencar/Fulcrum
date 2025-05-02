using NAudio.Wave;
using System;
using System.Diagnostics;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de efeito de alteração de tom (pitch)
/// </summary>
public class PitchShiftingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float _pitchFactor = 1.0f;
    private bool _isEnabled = false;
    private float _position = 0f;
    private float[] _lastBuffer;
    private int _lastBufferSize = 0;
    private float _maxSampleValue = 1.0f;
    private bool _enableClipping = true;
    private float _outputGain = 1.0f;
    private float[] _peakHistory = new float[5]; 
    private int _peakHistoryIndex = 0;
    private bool _autoNormalizeEnabled = true;
    
    // Constantes melhoradas
    private const float MIN_PITCH_CHANGE = 0.001f;
    private const int INITIAL_BUFFER_SIZE = 16384; // Aumentado para melhor desempenho
    private const int OVERLAP_SAMPLES = 512; // Amostras de sobreposição para transições suaves
    private const float BUFFER_REFILL_THRESHOLD = 0.7f; // Ponto em que buscamos mais amostras
    private const int WINDOW_SIZE = 8; // Tamanho da janela para interpolação

    /// <summary>
    /// Inicializa uma nova instância do provedor de alteração de tom
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public PitchShiftingSampleProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        
        // Buffer maior para processamento mais suave
        _lastBuffer = new float[INITIAL_BUFFER_SIZE]; 
        Debug.WriteLine($"[PitchShifting] Inicializado com tamanho de buffer {_lastBuffer.Length}");
        
        // Inicializa o histórico de picos com valores seguros
        for (int i = 0; i < _peakHistory.Length; i++)
            _peakHistory[i] = 0.8f;
            
        // Ajusta ganho inicial
        UpdateOutputGain();
        
        // Pré-carrega o buffer com amostras iniciais para processamento imediato
        PreencherBufferInicial();
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
            if (_isEnabled != value)
            {
                Debug.WriteLine($"[PitchShifting] Efeito {(value ? "ativado" : "desativado")}");
                _isEnabled = value;
                
                // Resetar estado ao ativar/desativar para evitar problemas de transição
                if (value)
                {
                    ResetState();
                }
            }
        }
    }

    /// <summary>
    /// Fator de alteração de tom (0.5 a 2.0, onde 1.0 é o tom original)
    /// </summary>
    public float PitchFactor
    {
        get => _pitchFactor;
        set 
        {
            float newValue = Math.Clamp(value, 0.5f, 2.0f);
            
            // Se a mudança for significativa, atualiza e reseta
            if (Math.Abs(_pitchFactor - newValue) > MIN_PITCH_CHANGE)
            {
                Debug.WriteLine($"[PitchShifting] Fator alterado: {newValue:F4}");
                
                // Para mudanças pequenas próximas a 1.0, garantimos transição suave
                if (Math.Abs(_pitchFactor - 1.0f) < 0.1f && Math.Abs(newValue - 1.0f) < 0.1f)
                {
                    // Transição suave para mudanças próximas ao original
                    _pitchFactor = newValue;
                    UpdateOutputGain();
                }
                else
                {
                    // Para mudanças maiores, realizamos um reset completo
                    _pitchFactor = newValue;
                    UpdateOutputGain();
                    ResetState();
                }
            }
        }
    }

    /// <summary>
    /// Define se a normalização automática de áudio está ativada
    /// </summary>
    public bool AutoNormalizeEnabled
    {
        get => _autoNormalizeEnabled;
        set => _autoNormalizeEnabled = value;
    }

    /// <summary>
    /// Reseta o estado interno do provedor para começar processamento do zero
    /// </summary>
    public void ResetState()
    {
        _position = 0;
        _lastBufferSize = 0;
        
        // Reinicia o histórico de picos
        for (int i = 0; i < _peakHistory.Length; i++)
            _peakHistory[i] = 0.8f;
            
        // Recalcula o ganho adequado
        UpdateOutputGain();
        
        // Pré-enche o buffer para iniciar com dados disponíveis
        PreencherBufferInicial();
            
        Debug.WriteLine("[PitchShifting] Estado reiniciado");
    }

    /// <summary>
    /// Pré-carrega o buffer com amostras iniciais para processamento imediato
    /// </summary>
    private void PreencherBufferInicial()
    {
        // Enche o buffer inicial com amostras suficientes
        int initialSamples = Math.Min(INITIAL_BUFFER_SIZE / 2, _lastBuffer.Length);
        int samplesRead = _source.Read(_lastBuffer, 0, initialSamples);
        _lastBufferSize = samplesRead;
        
        if (samplesRead > 0)
        {
            Debug.WriteLine($"[PitchShifting] Buffer inicial preenchido com {samplesRead} amostras");
        }
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito de alteração de tom
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // Se o efeito estiver desativado, apenas passa o áudio original
        if (!_isEnabled)
        {
            return _source.Read(buffer, offset, count);
        }

        // Para fatores muito próximos de 1.0, passamos diretamente sem processamento
        if (Math.Abs(_pitchFactor - 1.0f) < 0.005f)
        {
            return _source.Read(buffer, offset, count);
        }
        
        // Garante que o buffer é grande o suficiente
        EnsureBufferSize(count * 3); // Aumentamos a margem de segurança
        
        // Verifica se precisamos de mais amostras no buffer (ponto de recarga antecipado)
        // Se a posição atual já passou de 70% do buffer, buscamos mais amostras
        if (_position > _lastBufferSize * BUFFER_REFILL_THRESHOLD)
        {
            FillBuffer(count * 2); // Pedimos mais amostras do que precisamos para evitar recarga frequente
        }
        
        // Se ainda não temos amostras suficientes, retorna silêncio
        if (_lastBufferSize <= WINDOW_SIZE)
        {
            Debug.WriteLine("[PitchShifting] Buffer sem amostras suficientes");
            Array.Clear(buffer, offset, count);
            return count;
        }
        
        int outputSamples = 0;
        
        // Preenche o buffer de saída utilizando o fator de pitch
        while (outputSamples < count && _position < _lastBufferSize - WINDOW_SIZE)
        {
            // Calcula a amostra interpolada na posição atual
            float outputSample = ObterAmostraInterpolada(_position);
            
            // Aplica o ganho de saída para evitar saturação
            outputSample *= _outputGain;
            
            // Limita o valor para evitar clipping se habilitado
            if (_enableClipping)
            {
                outputSample = Math.Clamp(outputSample, -_maxSampleValue, _maxSampleValue);
            }
            
            buffer[offset + outputSamples] = outputSample;
            
            // Avança a posição de acordo com o fator de pitch, usando incremento mais preciso
            _position += _pitchFactor;
            outputSamples++;
        }
        
        // Se chegamos ao fim do buffer atual, mas ainda não preenchemos a saída,
        // buscamos mais amostras e continuamos o processamento
        if (outputSamples < count)
        {
            // Busca mais amostras
            FillBuffer(count * 2);
            
            // Continua preenchendo o buffer de saída
            while (outputSamples < count && _position < _lastBufferSize - WINDOW_SIZE)
            {
                float outputSample = ObterAmostraInterpolada(_position);
                outputSample *= _outputGain;
                
                if (_enableClipping)
                {
                    outputSample = Math.Clamp(outputSample, -_maxSampleValue, _maxSampleValue);
                }
                
                buffer[offset + outputSamples] = outputSample;
                _position += _pitchFactor;
                outputSamples++;
            }
        }
        
        // Monitora picos e ajusta ganho se necessário
        if (_autoNormalizeEnabled && outputSamples > 0)
        {
            MonitorAndNormalize(buffer, offset, outputSamples);
        }
        
        // Para casos extremos, onde não conseguimos gerar nenhuma amostra
        if (outputSamples == 0)
        {
            Debug.WriteLine("[PitchShifting] AVISO: Não foi possível gerar amostras!");
            int sourceSamples = _source.Read(buffer, offset, count);
            return sourceSamples > 0 ? sourceSamples : count;
        }
        
        return outputSamples;
    }
    
    /// <summary>
    /// Obtém uma amostra interpolada da posição especificada do buffer
    /// Usa interpolação cúbica para melhor qualidade
    /// </summary>
    private float ObterAmostraInterpolada(float pos)
    {
        int pos0 = (int)pos;
        float frac = pos - pos0;
        
        // Para interpolação de alta qualidade, usamos 4 pontos (interpolação cúbica)
        if (pos0 > 0 && pos0 < _lastBufferSize - 2)
        {
            float ym1 = _lastBuffer[pos0 - 1];
            float y0 = _lastBuffer[pos0];
            float y1 = _lastBuffer[pos0 + 1];
            float y2 = _lastBuffer[pos0 + 2];
            
            // Coeficientes para interpolação cúbica (algoritmo de Hermite)
            float c0 = y0;
            float c1 = 0.5f * (y1 - ym1);
            float c2 = ym1 - 2.5f * y0 + 2.0f * y1 - 0.5f * y2;
            float c3 = 0.5f * (y2 - ym1) + 1.5f * (y0 - y1);
            
            // Calcula o valor interpolado
            return ((c3 * frac + c2) * frac + c1) * frac + c0;
        }
        else
        {
            // Fallback para interpolação linear quando não temos pontos suficientes
            int pos1 = Math.Min(pos0 + 1, _lastBufferSize - 1);
            return _lastBuffer[pos0] * (1 - frac) + _lastBuffer[pos1] * frac;
        }
    }

    /// <summary>
    /// Processa amostras de áudio aplicando o efeito de alteração de tom diretamente no buffer
    /// </summary>
    /// <param name="buffer">Buffer de amostras</param>
    /// <param name="offset">Offset no buffer</param>
    /// <param name="count">Número de amostras a processar</param>
    public void ProcessInPlace(float[] buffer, int offset, int count)
    {
        // Otimização: Se o efeito estiver desativado ou o pitch for muito próximo de 1.0, não faz nada
        if (!_isEnabled || Math.Abs(_pitchFactor - 1.0f) < 0.005f)
        {
            return;
        }
        
        // Verifica a integridade do buffer de entrada
        if (buffer == null || offset < 0 || count <= 0 || buffer.Length < offset + count)
        {
            Debug.WriteLine("[PitchShifting] ERRO: Parâmetros inválidos em ProcessInPlace");
            return;
        }
        
        // Detecta valores extremos no buffer de entrada para normalização
        float maxInputValue = 0;
        for (int i = 0; i < count; i++)
        {
            float absValue = Math.Abs(buffer[offset + i]);
            if (absValue > maxInputValue)
                maxInputValue = absValue;
        }
        
        // Normaliza valores extremamente altos para evitar distorção
        if (maxInputValue > 10.0f)
        {
            float normFactor = 0.9f / maxInputValue;
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] *= normFactor;
            }
            Debug.WriteLine($"[PitchShifting] Entrada normalizada com fator {normFactor:F6}");
        }
        
        // Salvamos uma cópia do buffer original
        float[] tempBuffer = new float[count];
        Array.Copy(buffer, offset, tempBuffer, 0, count);
        
        // Garante que o buffer é grande o suficiente
        EnsureBufferSize(count * 3);
        
        // Adiciona as amostras do buffer ao buffer de processamento
        FillBufferFromTemp(tempBuffer, count);
        
        int outputSamples = 0;
        
        // Limpa o buffer de saída
        Array.Clear(buffer, offset, count);
        
        // Preenche o buffer de saída
        while (outputSamples < count && _position < _lastBufferSize - WINDOW_SIZE)
        {
            float outputSample = ObterAmostraInterpolada(_position);
            
            // Aplica processamento de ganho e limitação
            outputSample *= _outputGain;
            
            if (_enableClipping)
            {
                outputSample = Math.Clamp(outputSample, -_maxSampleValue, _maxSampleValue);
            }
            
            buffer[offset + outputSamples] = outputSample;
            _position += _pitchFactor;
            outputSamples++;
        }
        
        // Se não conseguimos gerar amostras suficientes, completamos o que falta
        if (outputSamples < count)
        {
            for (int i = outputSamples; i < count; i++)
            {
                buffer[offset + i] = 0;
            }
        }
        
        // Monitora e ajusta o ganho
        if (_autoNormalizeEnabled && outputSamples > 0)
        {
            MonitorAndNormalize(buffer, offset, outputSamples);
        }
    }
    
    /// <summary>
    /// Garante que o buffer interno tem tamanho suficiente
    /// </summary>
    private void EnsureBufferSize(int requiredSize)
    {
        if (_lastBuffer.Length < requiredSize)
        {
            int newSize = Math.Max(requiredSize, _lastBuffer.Length * 2);
            float[] newBuffer = new float[newSize];
            
            // Copia os dados existentes para o novo buffer
            if (_lastBufferSize > 0)
            {
                Array.Copy(_lastBuffer, 0, newBuffer, 0, _lastBufferSize);
            }
            
            _lastBuffer = newBuffer;
            Debug.WriteLine($"[PitchShifting] Buffer redimensionado para {newSize} amostras");
        }
    }
    
    /// <summary>
    /// Preenche o buffer interno com novas amostras da fonte
    /// </summary>
    private void FillBuffer(int count)
    {
        // Se a posição já passou de um certo ponto, movemos as amostras para o início
        if (_position > OVERLAP_SAMPLES)
        {
            // Calculamos quantas amostras queremos manter no início do buffer (overlap)
            int overlapStart = Math.Max(0, (int)_position - OVERLAP_SAMPLES);
            int overlapSize = Math.Min(_lastBufferSize - overlapStart, OVERLAP_SAMPLES);
            
            // Só fazemos a movimentação se tivermos amostras suficientes para o overlap
            if (overlapSize > 0)
            {
                // Move as amostras de sobreposição para o início do buffer
                Array.Copy(_lastBuffer, overlapStart, _lastBuffer, 0, overlapSize);
                
                // Ajustamos a posição considerando a nova posição das amostras no buffer
                _position -= overlapStart;
                _lastBufferSize = overlapSize;
            }
            else
            {
                // Se não temos amostras suficientes para o overlap, começamos do zero
                _position = 0;
                _lastBufferSize = 0;
            }
        }
        
        // Calculamos quantas amostras precisamos adicionar
        int spaceAvailable = _lastBuffer.Length - _lastBufferSize;
        int samplesToRead = Math.Min(spaceAvailable, count);
        
        // Lemos novas amostras na parte disponível do buffer
        if (samplesToRead > 0)
        {
            int samplesRead = _source.Read(_lastBuffer, _lastBufferSize, samplesToRead);
            
            if (samplesRead > 0)
            {
                // Atualizamos o tamanho do buffer
                _lastBufferSize += samplesRead;
                Debug.WriteLine($"[PitchShifting] Buffer atualizado: posição={_position:F2}, tamanho={_lastBufferSize}");
            }
            else
            {
                // Se não conseguimos ler mais amostras, repetimos as últimas para evitar silêncio súbito
                if (_lastBufferSize > 0 && _lastBufferSize < _lastBuffer.Length / 2)
                {
                    Debug.WriteLine("[PitchShifting] Repetindo últimas amostras para suavizar fim");
                    // Copia as últimas amostras para evitar silêncio abrupto
                    int repeatCount = Math.Min(_lastBufferSize, _lastBuffer.Length - _lastBufferSize);
                    Array.Copy(_lastBuffer, _lastBufferSize - repeatCount, _lastBuffer, _lastBufferSize, repeatCount);
                    _lastBufferSize += repeatCount;
                }
            }
        }
    }
    
    /// <summary>
    /// Preenche o buffer interno com amostras do buffer temporário
    /// </summary>
    private void FillBufferFromTemp(float[] tempBuffer, int count)
    {
        // Comportamento similar ao FillBuffer, mas usando o tempBuffer como fonte
        if (_position > OVERLAP_SAMPLES)
        {
            int overlapStart = Math.Max(0, (int)_position - OVERLAP_SAMPLES);
            int overlapSize = Math.Min(_lastBufferSize - overlapStart, OVERLAP_SAMPLES);
            
            if (overlapSize > 0)
            {
                Array.Copy(_lastBuffer, overlapStart, _lastBuffer, 0, overlapSize);
                _position -= overlapStart;
                _lastBufferSize = overlapSize;
            }
            else
            {
                _position = 0;
                _lastBufferSize = 0;
            }
        }
        
        // Adiciona amostras do buffer temporário
        int spaceAvailable = _lastBuffer.Length - _lastBufferSize;
        int samplesToAdd = Math.Min(spaceAvailable, count);
        
        if (samplesToAdd > 0)
        {
            Array.Copy(tempBuffer, 0, _lastBuffer, _lastBufferSize, samplesToAdd);
            _lastBufferSize += samplesToAdd;
        }
    }
    
    /// <summary>
    /// Monitora as amostras para detectar picos e ajusta o ganho se necessário
    /// </summary>
    private void MonitorAndNormalize(float[] buffer, int offset, int count)
    {
        // Encontra o valor máximo no buffer de saída
        float maxValue = 0;
        for (int i = 0; i < count; i++)
        {
            float absValue = Math.Abs(buffer[offset + i]);
            if (absValue > maxValue)
                maxValue = absValue;
        }
        
        // Registra valores de pico no histórico
        if (maxValue > 0.01f)
        {
            _peakHistory[_peakHistoryIndex] = maxValue;
            _peakHistoryIndex = (_peakHistoryIndex + 1) % _peakHistory.Length;
        }
        
        // Verifica se há valores acima do limite
        if (maxValue > _maxSampleValue)
        {
            // Valor extremamente alto - ajuste imediato do ganho e limitação
            if (maxValue > 10.0f)
            {
                _outputGain *= 0.5f; // Redução mais agressiva para valores extremos
                
                // Aplica limitador
                for (int i = 0; i < count; i++)
                {
                    if (Math.Abs(buffer[offset + i]) > _maxSampleValue)
                    {
                        buffer[offset + i] = Math.Sign(buffer[offset + i]) * _maxSampleValue * 0.99f;
                    }
                }
                
                Debug.WriteLine($"[PitchShifting] Limitação aplicada para valor extremo: {maxValue:F4}");
            }
            // Valor alto, mas não extremo - ajuste gradual
            else
            {
                _outputGain *= 0.95f; // Redução suave
            }
            
            _outputGain = Math.Max(_outputGain, 0.1f); // Limitação inferior
        }
        // Se estiver muito abaixo do limite, aumentamos gradualmente
        else if (maxValue < _maxSampleValue * 0.5f && _outputGain < 0.95f)
        {
            _outputGain *= 1.005f; // Aumento mais rápido
            _outputGain = Math.Min(_outputGain, 1.0f); // Limitação superior
        }
    }
    
    /// <summary>
    /// Atualiza o ganho de saída baseado no fator de pitch atual
    /// </summary>
    private void UpdateOutputGain()
    {
        // Ajuste de ganho baseado no fator de pitch
        if (Math.Abs(_pitchFactor - 1.0f) > 0.7f)
        {
            // Pitch muito alterado (valores extremos)
            _outputGain = 0.6f;
        }
        else if (Math.Abs(_pitchFactor - 1.0f) > 0.3f)
        {
            // Pitch moderadamente alterado
            _outputGain = 0.75f;
        }
        else if (Math.Abs(_pitchFactor - 1.0f) > 0.1f)
        {
            // Pequena alteração de pitch
            _outputGain = 0.85f;
        }
        else
        {
            // Pitch próximo ao normal
            _outputGain = 0.95f;
        }
        
        Debug.WriteLine($"[PitchShifting] Ganho inicial definido para {_outputGain:F2} com fator de pitch {_pitchFactor:F4}");
    }
}