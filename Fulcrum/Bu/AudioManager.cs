using System.Collections.Concurrent;

namespace Fulcrum.Bu;

/// <summary>
/// Gerenciador de áudio central do aplicativo
/// </summary>
public sealed class AudioManager
{
    private static readonly Lazy<AudioManager> _instance = new(() => new AudioManager());
    private readonly ConcurrentDictionary<string, Reprodutor> _audioPlayers = new();
    private readonly ConcurrentDictionary<string, float> _volumeStates = new();

    // Construtor privado para implementar o padrão Singleton
    private AudioManager() { }

    /// <summary>
    /// Instância única do AudioManager (Singleton)
    /// </summary>
    public static AudioManager Instance => _instance.Value;

    /// <summary>
    /// Adiciona um reprodutor de áudio ao gerenciador
    /// </summary>
    /// <param name="id">Identificador único do reprodutor</param>
    /// <param name="player">Reprodutor a ser adicionado</param>
    public void AddAudioPlayer(string id, Reprodutor player)
    {
        _audioPlayers.TryAdd(id, player);
        
        // Restaura o volume salvo anteriormente, se existir
        if (_volumeStates.TryGetValue(id, out float savedVolume))
        {
            player.AlterarVolume(savedVolume);
        }
    }

    /// <summary>
    /// Obtém um reprodutor de áudio pelo seu identificador
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <returns>Reprodutor de áudio</returns>
    public Reprodutor GetReprodutorPorId(string id)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
            return player;

        throw new KeyNotFoundException($"Reprodutor de áudio com ID '{id}' não encontrado");
    }

    /// <summary>
    /// Pausa todos os reprodutores de áudio
    /// </summary>
    public void PauseAll()
    {
        foreach (var player in _audioPlayers.Values)
        {
            player.Pause();
        }
    }

    /// <summary>
    /// Inicia a reprodução de todos os áudios
    /// </summary>
    public void PlayAll()
    {
        foreach (var player in _audioPlayers.Values)
        {
            player.Play();
        }
    }

    /// <summary>
    /// Altera o volume de um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="volume">Novo volume (0.0 a 1.0)</param>
    public void AlterarVolume(string id, double volume)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.AlterarVolume(volume);
            // Salva o estado do volume para restauração posterior
            _volumeStates[id] = (float)volume;
        }
    }
    
    /// <summary>
    /// Salva o estado atual dos volumes de todos os reprodutores
    /// </summary>
    public void SalvarEstadoVolumes()
    {
        foreach (var entry in _audioPlayers)
        {
            _volumeStates[entry.Key] = entry.Value.Reader.Volume;
        }
    }
    
    /// <summary>
    /// Limpa todos os reprodutores registrados
    /// </summary>
    public void LimparReprodutores()
    {
        // Salva o estado atual dos volumes antes de limpar
        SalvarEstadoVolumes();
        
        // Pausa todos os reprodutores
        PauseAll();
        
        // Remove as referências, mas mantém os estados de volume
        _audioPlayers.Clear();
    }

    /// <summary>
    /// Obtém a quantidade de reprodutores registrados
    /// </summary>
    public int GetQuantidadeReprodutor => _audioPlayers.Count;

    /// <summary>
    /// Obtém todos os reprodutores registrados
    /// </summary>
    /// <returns>Dicionário com todos os reprodutores</returns>
    public IReadOnlyDictionary<string, Reprodutor> GetListReprodutores() => _audioPlayers;

    /// <summary>
    /// Libera todos os recursos de áudio
    /// </summary>
    public void DisposeAll()
    {
        foreach (var player in _audioPlayers.Values)
        {
            player.WaveOut.Dispose();
            player.Reader.Dispose();
        }
        
        _audioPlayers.Clear();
    }
}
