namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de ondas
/// </summary>
public class ReprodutorOndas : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de ondas
    /// </summary>
    public ReprodutorOndas()
    {
        Initialize("Assets\\Sounds\\ondas.wav");
    }
}
