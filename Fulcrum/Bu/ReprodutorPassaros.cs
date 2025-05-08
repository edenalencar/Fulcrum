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
        // Garante que o nome do arquivo corresponda ao ID usado no sistema
        Initialize("Assets\\Sounds\\passaros.wav");
    }
}