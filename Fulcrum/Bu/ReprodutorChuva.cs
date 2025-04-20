using NAudio.Wave;

namespace Fulcrum.Bu;

/// <summary>
/// Reprodutor especializado para som de chuva
/// </summary>
public class ReprodutorChuva : Reprodutor
{
    /// <summary>
    /// Inicializa um novo reprodutor de som de chuva
    /// </summary>
    public ReprodutorChuva()
    {
        // Alterado para usar o nome completo do arquivo conforme registrado no csproj
        Initialize("Assets\\Sounds\\chuva forte.wav");
    }
}
