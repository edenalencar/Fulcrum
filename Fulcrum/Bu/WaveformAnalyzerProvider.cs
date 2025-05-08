using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Provedor de amostragem com análise para visualização da forma de onda
/// </summary>
public class WaveformAnalyzerProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _fftLength;
    private readonly List<float> _sampleHistory;
    private readonly int _maxHistoryLength;
    private readonly object _lockObject = new();

    // Buffers para análise FFT (para futura implementação espectral)
    private float[] _fftBuffer;
    private Complex[] _complexBuffer;

    /// <summary>
    /// Inicializa uma nova instância do analisador de forma de onda
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    /// <param name="historyLength">Número de amostras a manter no histórico</param>
    /// <param name="fftLength">Tamanho da FFT para análise espectral</param>
    public WaveformAnalyzerProvider(ISampleProvider source, int historyLength = 4096, int fftLength = 1024)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        _maxHistoryLength = historyLength;
        _sampleHistory = new List<float>(historyLength);
        _fftLength = fftLength;

        // Inicializa buffers para análise FFT
        _fftBuffer = new float[fftLength];
        _complexBuffer = new Complex[fftLength];
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Obtém uma cópia do histórico de amostras para visualização
    /// </summary>
    /// <returns>Array com amostras recentes</returns>
    public float[] GetWaveformData()
    {
        lock (_lockObject)
        {
            return _sampleHistory.ToArray();
        }
    }

    /// <summary>
    /// Calcula o valor RMS (Root Mean Square) atual do áudio
    /// </summary>
    /// <returns>Valor RMS entre 0.0 e 1.0</returns>
    public float GetRmsLevel()
    {
        lock (_lockObject)
        {
            if (_sampleHistory.Count == 0)
                return 0f;

            float sumOfSquares = 0;
            foreach (float sample in _sampleHistory)
            {
                sumOfSquares += sample * sample;
            }

            return (float)Math.Sqrt(sumOfSquares / _sampleHistory.Count);
        }
    }

    /// <summary>
    /// Obtém o valor de pico das amostras recentes
    /// </summary>
    /// <returns>Valor de pico entre 0.0 e 1.0</returns>
    public float GetPeakLevel()
    {
        lock (_lockObject)
        {
            if (_sampleHistory.Count == 0)
                return 0f;

            float max = 0;
            foreach (float sample in _sampleHistory)
            {
                float abs = Math.Abs(sample);
                if (abs > max)
                    max = abs;
            }

            return max;
        }
    }

    /// <summary>
    /// Limpa o histórico de amostras
    /// </summary>
    public void ClearHistory()
    {
        lock (_lockObject)
        {
            _sampleHistory.Clear();
        }
    }

    /// <summary>
    /// Lê amostras de áudio e armazena para análise
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0)
        {
            lock (_lockObject)
            {
                // Adiciona novas amostras ao histórico
                for (int i = 0; i < samplesRead; i += WaveFormat.Channels)
                {
                    // Usado para mono ou para o canal esquerdo em estéreo
                    float sample = buffer[offset + i];
                    _sampleHistory.Add(sample);

                    // Limita o tamanho do histórico
                    if (_sampleHistory.Count > _maxHistoryLength)
                    {
                        _sampleHistory.RemoveAt(0);
                    }
                }
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// Representação de um número complexo para análise FFT
    /// </summary>
    private struct Complex
    {
        public float Real;
        public float Imaginary;

        public Complex(float real, float imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public float Magnitude => (float)Math.Sqrt(Real * Real + Imaginary * Imaginary);
    }
}