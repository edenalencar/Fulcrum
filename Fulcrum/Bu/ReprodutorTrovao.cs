namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de trovão
/// </summary>
public class ReprodutorTrovao : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de trovão
    /// </summary>
    public ReprodutorTrovao()
    {
        Initialize("Assets\\Sounds\\trovao.wav");
    }
}