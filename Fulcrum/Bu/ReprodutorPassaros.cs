namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de pássaros
/// </summary>
public class ReprodutorPassaros : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de pássaros
    /// </summary>
    public ReprodutorPassaros()
    {
        Initialize("Assets\\Sounds\\passaros.wav");
    }
}