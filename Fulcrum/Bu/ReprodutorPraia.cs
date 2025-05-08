namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de praia
/// </summary>
public class ReprodutorPraia : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de praia
    /// </summary>
    public ReprodutorPraia()
    {
        // Alterado para usar o arquivo correto que existe na pasta de ativos
        Initialize("Assets\\Sounds\\manhã-praia.wav");

        // Configurações específicas para o som de praia
        ConfigurarEqualizadorPraia();
    }

    /// <summary>
    /// Configura o equalizador com valores otimizados para sons de praia
    /// </summary>
    private void ConfigurarEqualizadorPraia()
    {
        // Configurações recomendadas para sons de praia
        // Aumenta levemente os graves para as ondas e reduz os agudos para o som mais suave
        if (Equalizer != null && Equalizer.Bands.Length >= 3)
        {
            // Banda baixa (ondas) - ligeiramente aumentada
            AjustarBandaEqualizador(0, 2.0f);

            // Banda média (sons gerais) - neutra
            AjustarBandaEqualizador(1, 0.0f);

            // Banda alta (sons agudos como pássaros ao fundo) - redução leve
            AjustarBandaEqualizador(2, -1.5f);
        }
    }

    /// <summary>
    /// Personaliza os efeitos para som de praia
    /// </summary>
    /// <param name="tipoEfeito">Tipo de efeito a ser aplicado</param>
    public override void DefinirTipoEfeito(TipoEfeito tipoEfeito)
    {
        base.DefinirTipoEfeito(tipoEfeito);

        // Configurações adicionais para sons de praia
        if (tipoEfeito == TipoEfeito.Reverb)
        {
            // Reverberação leve para simular ambiente aberto
            AjustarReverbMix(0.2f);
            AjustarReverbTime(1.5f);
        }
        else if (tipoEfeito == TipoEfeito.Echo)
        {
            // Eco muito sutil para ambiente de praia
            AjustarEcho(300f, 0.15f);
        }
    }
}
