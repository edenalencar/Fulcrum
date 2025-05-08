using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Filtro de limpeza de áudio que remove artefatos e ruídos
/// </summary>
public class AudioCleanerFilter : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _threshold;
    private readonly float _attackTime;
    private readonly float _releaseTime;
    private float _envelope;

    /// <summary>
    /// Inicializa uma nova instância do filtro de limpeza de áudio
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    /// <param name="threshold">Limiar para detecção de ruído (0.0 a 1.0)</param>
    /// <param name="attackTime">Tempo de ataque em milissegundos</param>
    /// <param name="releaseTime">Tempo de liberação em milissegundos</param>
    public AudioCleanerFilter(ISampleProvider source, float threshold = 0.1f, float attackTime = 10f, float releaseTime = 100f)
    {
        _source = source;
        _threshold = Math.Clamp(threshold, 0.0f, 1.0f);
        _attackTime = attackTime;
        _releaseTime = releaseTime;
        _envelope = 0.0f;
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Lê amostras de áudio e aplica a limpeza
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0)
        {
            // Calcula as constantes de tempo baseadas na taxa de amostragem
            float attackCoeff = (float)Math.Exp(-1.0 / (_attackTime * 0.001f * WaveFormat.SampleRate));
            float releaseCoeff = (float)Math.Exp(-1.0 / (_releaseTime * 0.001f * WaveFormat.SampleRate));

            // Processa cada amostra
            for (int i = 0; i < samplesRead; i++)
            {
                float input = buffer[offset + i];
                float inputAbs = Math.Abs(input);

                // Atualiza o envelope seguidor
                if (inputAbs > _envelope)
                {
                    _envelope = attackCoeff * _envelope + (1 - attackCoeff) * inputAbs;
                }
                else
                {
                    _envelope = releaseCoeff * _envelope + (1 - releaseCoeff) * inputAbs;
                }

                // Aplica o gate baseado no envelope e threshold
                if (_envelope < _threshold)
                {
                    buffer[offset + i] = 0.0f; // Silencia amostras abaixo do threshold
                }
            }
        }

        return samplesRead;
    }
}