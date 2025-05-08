using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Implementa um normalizador de áudio para garantir que as amostras fiquem
/// dentro da faixa aceitável (-1.0 a 1.0) e evitar distorções.
/// </summary>
public class NormalizadorAudio : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _targetLevel;
    private readonly float _maxSampleValue;
    private bool _scanCompleted = false;
    private float[] _scanBuffer;
    private readonly int _scanBufferSize = 4096;
    private float _peakLevel = 0.0f;
    private bool _enableHardLimiter = true;
    private bool _isEnabled = true;

    // Cache para armazenar valores históricos e aplicar suavização
    private readonly float[] _historicalPeaks;
    private int _historicalPeaksIndex = 0;
    private readonly int _historicalPeaksSize = 5;
    private readonly bool _useAveragePeak = true;

    // Atenuação para valores extremos
    private float _lastCompressionFactor = 1.0f;
    private readonly float _compressionSmoothingFactor = 0.8f; // Menos suavização para resposta mais rápida
    private readonly float _minCompressionFactor = 0.1f; // Aumentado para evitar atenuação excessiva

    /// <summary>
    /// Inicializa uma nova instância do normalizador de áudio
    /// </summary>
    /// <param name="source">Fonte de áudio a ser normalizada</param>
    /// <param name="targetLevel">Nível alvo para normalização (0.0 a 1.0), padrão 0.95</param>
    /// <param name="scanSource">Define se o áudio deve ser escaneado para determinar o nível de pico</param>
    /// <param name="enableHardLimiter">Habilita o limitador que impede que qualquer amostra exceda 1.0</param>
    public NormalizadorAudio(ISampleProvider source, float targetLevel = 0.95f, bool scanSource = false, bool enableHardLimiter = true)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _targetLevel = Math.Clamp(targetLevel, 0.1f, 1.0f);
        _enableHardLimiter = enableHardLimiter;
        _scanBuffer = new float[_scanBufferSize];
        _maxSampleValue = 1.0f;
        _historicalPeaks = new float[_historicalPeaksSize];

        // Inicializa o histórico com valores padrão (1.0)
        for (int i = 0; i < _historicalPeaksSize; i++)
        {
            _historicalPeaks[i] = 1.0f;
        }

        // Se solicitado, faz uma varredura para determinar o pico
        if (scanSource)
        {
            ScanSource();
        }

        System.Diagnostics.Debug.WriteLine($"NormalizadorAudio inicializado: TargetLevel={_targetLevel}, PeakLevel={_peakLevel}, HardLimiter={_enableHardLimiter}");
    }

    /// <summary>
    /// Obtém ou define se o normalizador está ativo
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
    /// Escaneia a fonte completa para determinar o nível de pico
    /// </summary>
    private void ScanSource()
    {
        if (_scanCompleted)
            return;

        System.Diagnostics.Debug.WriteLine("Escaneando áudio para determinar nível de pico...");

        int totalSamplesRead = 0;
        int samplesRead;
        float currentPeak = 0.0f;

        // Armazena a posição atual se a fonte for um AudioFileReader
        long originalPosition = 0;
        if (_source is AudioFileReader reader)
        {
            originalPosition = reader.Position;
            reader.Position = 0;
        }

        try
        {
            // Lê todas as amostras da fonte para encontrar o valor máximo
            while ((samplesRead = _source.Read(_scanBuffer, 0, _scanBuffer.Length)) > 0)
            {
                for (int i = 0; i < samplesRead; i++)
                {
                    float absoluteSample = Math.Abs(_scanBuffer[i]);
                    if (absoluteSample > currentPeak)
                    {
                        currentPeak = absoluteSample;
                    }
                }

                totalSamplesRead += samplesRead;
            }

            _peakLevel = currentPeak;
            _scanCompleted = true;

            System.Diagnostics.Debug.WriteLine($"Escaneamento concluído: {totalSamplesRead} amostras processadas, pico encontrado: {_peakLevel}");
        }
        finally
        {
            // Restaura a posição original
            if (_source is AudioFileReader readerRestore)
            {
                readerRestore.Position = originalPosition;
            }
        }
    }

    /// <summary>
    /// Calcula uma média suavizada dos picos históricos para evitar mudanças bruscas
    /// </summary>
    private float CalculateAveragePeak()
    {
        float sum = 0f;
        for (int i = 0; i < _historicalPeaksSize; i++)
        {
            sum += _historicalPeaks[i];
        }
        return sum / _historicalPeaksSize;
    }

    /// <summary>
    /// Adiciona um novo valor de pico ao histórico
    /// </summary>
    private void AddPeakToHistory(float peak)
    {
        _historicalPeaks[_historicalPeaksIndex] = peak;
        _historicalPeaksIndex = (_historicalPeaksIndex + 1) % _historicalPeaksSize;
    }

    /// <summary>
    /// Lê amostras da fonte e aplica normalização
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // Lê amostras da fonte
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead <= 0 || !_isEnabled)
            return samplesRead;

        // Verifica se há amostras com valor acima de 1.0
        float maxSample = 0.0f;
        bool hasExtremeValues = false;

        // Primeiro passo: encontrar o valor máximo neste frame
        for (int i = 0; i < samplesRead; i++)
        {
            float absoluteSample = Math.Abs(buffer[offset + i]);
            if (absoluteSample > maxSample)
            {
                maxSample = absoluteSample;
            }

            // Detecta valores realmente extremos (acima de 10.0) para tratamento especial
            if (absoluteSample > 10.0f)
            {
                hasExtremeValues = true;
            }
        }

        // Adiciona este pico ao histórico
        AddPeakToHistory(maxSample);

        // Calcula o fator de compressão suavizado
        float compressionFactor = 1.0f;
        float peakForCalculation = _useAveragePeak ? CalculateAveragePeak() : maxSample;

        // Se o valor máximo exceder 1.0, calcula o fator de compressão
        if (peakForCalculation > _maxSampleValue)
        {
            // Para valores extremamente altos, usamos uma abordagem mais gradual
            if (hasExtremeValues && peakForCalculation > 50.0f)
            {
                // Abordagem logarítmica mais suave para valores extremos
                compressionFactor = _targetLevel / ((float)Math.Log10(peakForCalculation) + 1.0f);
            }
            // Para valores moderadamente altos, usamos uma abordagem linear suave
            else if (peakForCalculation > 5.0f && peakForCalculation <= 50.0f)
            {
                // Suavização para valores moderadamente altos
                compressionFactor = _targetLevel / ((peakForCalculation / 5.0f) + 0.8f);
            }
            // Para valores um pouco altos, usamos a abordagem normal
            else
            {
                compressionFactor = _targetLevel / peakForCalculation;
            }

            // Garante um fator mínimo para evitar silenciamento excessivo
            compressionFactor = Math.Max(compressionFactor, _minCompressionFactor);

            // Suaviza a transição entre fatores de compressão para evitar artefatos audíveis
            compressionFactor = (_lastCompressionFactor * _compressionSmoothingFactor) +
                              (compressionFactor * (1.0f - _compressionSmoothingFactor));

            if (hasExtremeValues || maxSample > 2.0f)
            {
                System.Diagnostics.Debug.WriteLine($"Normalizando áudio: valor máximo={maxSample:F2}, fator={compressionFactor:F3}");
            }
        }

        // Armazena o fator para a próxima iteração
        _lastCompressionFactor = compressionFactor;

        // Segundo passo: aplicar o fator de compressão às amostras
        for (int i = 0; i < samplesRead; i++)
        {
            // Se o pico foi escaneado e é maior que 1.0, normaliza com base nele
            if (_scanCompleted && _peakLevel > _maxSampleValue)
            {
                buffer[offset + i] *= (_targetLevel / _peakLevel);
            }
            // Se há compressão a ser aplicada (amostras > 1.0 no buffer atual)
            else if (compressionFactor < 1.0f)
            {
                // Para valores extremos, aplicamos uma correção mais suave
                if (Math.Abs(buffer[offset + i]) > 10.0f)
                {
                    float absValue = Math.Abs(buffer[offset + i]);
                    float sign = Math.Sign(buffer[offset + i]);

                    // Usamos uma curva logarítmica para comprimir valores extremos
                    // Esta abordagem preserva mais as características do áudio
                    float logValue = (float)Math.Log10(absValue) + 1.0f;
                    buffer[offset + i] = sign * logValue * compressionFactor * 0.8f;
                }
                // Para valores normais, aplicamos o fator de compressão diretamente
                else
                {
                    buffer[offset + i] *= compressionFactor;
                }
            }

            // Aplica limitador rígido como última proteção, apenas se estiver habilitado
            if (_enableHardLimiter)
            {
                if (buffer[offset + i] > _maxSampleValue)
                {
                    buffer[offset + i] = _maxSampleValue * 0.99f; // Pequena margem para evitar clipping exato
                }
                else if (buffer[offset + i] < -_maxSampleValue)
                {
                    buffer[offset + i] = -_maxSampleValue * 0.99f;
                }
            }
        }

        return samplesRead;
    }
}