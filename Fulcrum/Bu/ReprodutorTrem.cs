namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de trem
/// </summary>
public class ReprodutorTrem : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de trem
    /// </summary>
    public ReprodutorTrem()
    {
        // Garante que o nome do arquivo corresponda ao ID usado no sistema
        Initialize("Assets\\Sounds\\trem.wav");
    }
}