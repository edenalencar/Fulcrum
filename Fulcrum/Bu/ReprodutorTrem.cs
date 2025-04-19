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
        Initialize("Assets\\Sounds\\trem.wav");
    }
}