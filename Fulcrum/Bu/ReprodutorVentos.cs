namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de ventos
/// </summary>
public class ReprodutorVentos : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de ventos
    /// </summary>
    public ReprodutorVentos()
    {
        Initialize("Assets\\Sounds\\ventos.wav");
    }
}

