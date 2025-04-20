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
        // Garante que o nome do arquivo corresponda ao ID usado no sistema
        Initialize("Assets\\Sounds\\ondas.wav");
    }
}
