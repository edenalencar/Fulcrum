namespace Fulcrum.Bu;

/// <summary>
/// Implementa um filtro BiQuad para processamento de áudio
/// </summary>
public class BiQuadFilter
{
    // Coeficientes do filtro
    private float a0, a1, a2, b1, b2;

    // Estado do filtro
    private float z1, z2;

    /// <summary>
    /// Inicializa um novo filtro BiQuad com os coeficientes especificados
    /// </summary>
    /// <param name="a0">Coeficiente a0</param>
    /// <param name="a1">Coeficiente a1</param>
    /// <param name="a2">Coeficiente a2</param>
    /// <param name="b1">Coeficiente b1</param>
    /// <param name="b2">Coeficiente b2</param>
    public BiQuadFilter(float a0, float a1, float a2, float b1, float b2)
    {
        this.a0 = a0;
        this.a1 = a1;
        this.a2 = a2;
        this.b1 = b1;
        this.b2 = b2;

        Reset();
    }

    /// <summary>
    /// Cria um filtro de equalização paramétrica (Peaking EQ)
    /// </summary>
    /// <param name="sampleRate">Taxa de amostragem em Hz</param>
    /// <param name="frequency">Frequência central em Hz</param>
    /// <param name="q">Fator Q (largura de banda)</param>
    /// <param name="gainFactor">Fator de ganho (amplitude, não em dB)</param>
    /// <returns>Um novo filtro BiQuad configurado como equalizador paramétrico</returns>
    public static BiQuadFilter PeakingEQ(float sampleRate, float frequency, float q, float gainFactor)
    {
        float omega = 2 * (float)Math.PI * frequency / sampleRate;
        float alpha = (float)Math.Sin(omega) / (2 * q);

        float a0 = 1 + alpha / gainFactor;
        float a1 = -2 * (float)Math.Cos(omega);
        float a2 = 1 - alpha / gainFactor;
        float b0 = 1 + alpha * gainFactor;
        float b1 = -2 * (float)Math.Cos(omega);
        float b2 = 1 - alpha * gainFactor;

        return new BiQuadFilter(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
    }

    /// <summary>
    /// Cria um filtro de shelving para baixas frequências
    /// </summary>
    /// <param name="sampleRate">Taxa de amostragem em Hz</param>
    /// <param name="frequency">Frequência de corte em Hz</param>
    /// <param name="q">Fator Q</param>
    /// <param name="gainFactor">Fator de ganho (amplitude, não em dB)</param>
    /// <returns>Um novo filtro BiQuad configurado como shelving de graves</returns>
    public static BiQuadFilter LowShelf(float sampleRate, float frequency, float q, float gainFactor)
    {
        float omega = 2 * (float)Math.PI * frequency / sampleRate;
        float alpha = (float)Math.Sin(omega) / (2 * q);
        float A = (float)Math.Sqrt(gainFactor);

        float a0 = (A + 1) + (A - 1) * (float)Math.Cos(omega) + 2 * (float)Math.Sqrt(A) * alpha;
        float a1 = -2 * ((A - 1) + (A + 1) * (float)Math.Cos(omega));
        float a2 = (A + 1) + (A - 1) * (float)Math.Cos(omega) - 2 * (float)Math.Sqrt(A) * alpha;
        float b0 = A * ((A + 1) - (A - 1) * (float)Math.Cos(omega) + 2 * (float)Math.Sqrt(A) * alpha);
        float b1 = 2 * A * ((A - 1) - (A + 1) * (float)Math.Cos(omega));
        float b2 = A * ((A + 1) - (A - 1) * (float)Math.Cos(omega) - 2 * (float)Math.Sqrt(A) * alpha);

        return new BiQuadFilter(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
    }

    /// <summary>
    /// Cria um filtro de shelving para altas frequências
    /// </summary>
    /// <param name="sampleRate">Taxa de amostragem em Hz</param>
    /// <param name="frequency">Frequência de corte em Hz</param>
    /// <param name="q">Fator Q</param>
    /// <param name="gainFactor">Fator de ganho (amplitude, não em dB)</param>
    /// <returns>Um novo filtro BiQuad configurado como shelving de agudos</returns>
    public static BiQuadFilter HighShelf(float sampleRate, float frequency, float q, float gainFactor)
    {
        float omega = 2 * (float)Math.PI * frequency / sampleRate;
        float alpha = (float)Math.Sin(omega) / (2 * q);
        float A = (float)Math.Sqrt(gainFactor);

        float a0 = (A + 1) - (A - 1) * (float)Math.Cos(omega) + 2 * (float)Math.Sqrt(A) * alpha;
        float a1 = 2 * ((A - 1) - (A + 1) * (float)Math.Cos(omega));
        float a2 = (A + 1) - (A - 1) * (float)Math.Cos(omega) - 2 * (float)Math.Sqrt(A) * alpha;
        float b0 = A * ((A + 1) + (A - 1) * (float)Math.Cos(omega) + 2 * (float)Math.Sqrt(A) * alpha);
        float b1 = -2 * A * ((A - 1) + (A + 1) * (float)Math.Cos(omega));
        float b2 = A * ((A + 1) + (A - 1) * (float)Math.Cos(omega) - 2 * (float)Math.Sqrt(A) * alpha);

        return new BiQuadFilter(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
    }

    /// <summary>
    /// Processa uma amostra de áudio através do filtro
    /// </summary>
    /// <param name="input">Amostra de entrada</param>
    /// <returns>Amostra filtrada</returns>
    public float Process(float input)
    {
        float output = input * a0 + z1;
        z1 = input * a1 + z2 - b1 * output;
        z2 = input * a2 - b2 * output;
        return output;
    }

    /// <summary>
    /// Reseta o estado do filtro
    /// </summary>
    public void Reset()
    {
        z1 = 0;
        z2 = 0;
    }
}