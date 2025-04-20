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
        // Garante que o nome do arquivo corresponda ao ID usado no sistema
        Initialize("Assets\\Sounds\\ventos.wav");
    }
}

