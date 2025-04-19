using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de lancha
/// </summary>
public class ReprodutorLancha : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de lancha
    /// </summary>
    public ReprodutorLancha()
    {
        Initialize("Assets\\Sounds\\lancha.wav");
    }
}
