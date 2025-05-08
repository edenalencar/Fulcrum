namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de lancha
/// </summary>
public class ReprodutorLancha : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de lancha
    /// </summary>
    public ReprodutorLancha()
    {
        // Garante que o nome do arquivo corresponda ao ID usado no sistema
        Initialize("Assets\\Sounds\\lancha.wav");
    }
}
