namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som ambiente de cafeteria
/// </summary>
public class ReprodutorCafeteria : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de cafeteria
    /// </summary>
    public ReprodutorCafeteria()
    {
        Initialize("Assets\\Sounds\\cafeteria.wav");
    }
}