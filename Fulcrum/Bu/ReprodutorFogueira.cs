namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de fogueira
/// </summary>
public class ReprodutorFogueira : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de fogueira
    /// </summary>
    public ReprodutorFogueira()
    {
        Initialize("Assets\\Sounds\\fogueira.wav");
    }
}
