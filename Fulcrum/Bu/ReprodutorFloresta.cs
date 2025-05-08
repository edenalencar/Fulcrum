namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som ambiente de floresta
/// </summary>
public class ReprodutorFloresta : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de floresta
    /// </summary>
    public ReprodutorFloresta()
    {
        Initialize("Assets\\Sounds\\floresta.wav");
    }
}