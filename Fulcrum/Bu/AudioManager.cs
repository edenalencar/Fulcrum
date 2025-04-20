using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using Windows.Storage;

namespace Fulcrum.Bu;

/// <summary>
/// Representação dos estados possíveis de reprodução de áudio
/// </summary>
public enum PlaybackState
{
    /// <summary>
    /// O áudio está parado
    /// </summary>
    Stopped,
    
    /// <summary>
    /// O áudio está tocando
    /// </summary>
    Playing,
    
    /// <summary>
    /// O áudio está pausado
    /// </summary>
    Paused
}

/// <summary>
/// Gerenciador de áudio central do aplicativo
/// </summary>
public sealed class AudioManager
{
    private static readonly Lazy<AudioManager> _instance = new(() => new AudioManager());
    private readonly ConcurrentDictionary<string, Reprodutor> _audioPlayers = new();
    private readonly ConcurrentDictionary<string, float> _volumeStates = new();

    // Armazena configurações de equalizador para salvar/restaurar
    private readonly ConcurrentDictionary<string, float[]> _equalizerSettings = new();
    // Armazena configurações de efeitos para salvar/restaurar
    private readonly ConcurrentDictionary<string, EffectSettings> _effectSettings = new();

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
        try
        {
            System.Diagnostics.Debug.WriteLine($"Adicionando reprodutor com ID '{id}'");
            if (!_audioPlayers.ContainsKey(id))
            {
                _audioPlayers.TryAdd(id, player);
                System.Diagnostics.Debug.WriteLine($"Reprodutor com ID '{id}' adicionado com sucesso");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AVISO: Reprodutor com ID '{id}' já existe, substituindo");
                _audioPlayers[id] = player;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao adicionar reprodutor '{id}': {ex.Message}");
            throw;
        }
        
        // Restaura o volume salvo anteriormente, se existir
        if (_volumeStates.TryGetValue(id, out float savedVolume))
        {
            System.Diagnostics.Debug.WriteLine($"Restaurando volume salvo para '{id}': {savedVolume}");
            player.AlterarVolume(savedVolume);
        }
        
        // Restaura as configurações de equalização, se existirem
        if (_equalizerSettings.TryGetValue(id, out float[] eqSettings) && eqSettings.Length == 3)
        {
            System.Diagnostics.Debug.WriteLine($"Restaurando configurações de equalização para '{id}'");
            
            try
            {
                for (int i = 0; i < eqSettings.Length; i++)
                {
                    player.AjustarBandaEqualizador(i, eqSettings[i]);
                }
                player.EqualizerEnabled = true;
                System.Diagnostics.Debug.WriteLine($"Equalização restaurada para '{id}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao aplicar configurações de equalização para '{id}': {ex.Message}");
            }
        }
        
        // Restaura as configurações de efeitos, se existirem
        if (_effectSettings.TryGetValue(id, out EffectSettings effectConfig))
        {
            System.Diagnostics.Debug.WriteLine($"Restaurando configurações de efeitos para '{id}': Tipo={effectConfig.TipoEfeito}");
            
            try
            {
                player.DefinirTipoEfeito(effectConfig.TipoEfeito);
                
                switch (effectConfig.TipoEfeito)
                {
                    case TipoEfeito.Reverb:
                        player.AjustarReverbMix(effectConfig.ReverbMix);
                        player.AjustarReverbTime(effectConfig.ReverbTime);
                        break;
                    case TipoEfeito.Pitch:
                        player.AjustarPitchFactor(effectConfig.PitchFactor);
                        break;
                    case TipoEfeito.Echo:
                        player.AjustarEcho(effectConfig.EchoDelay, effectConfig.EchoMix);
                        break;
                    case TipoEfeito.Flanger:
                        player.AjustarFlanger(effectConfig.FlangerRate, effectConfig.FlangerDepth);
                        break;
                }
                
                player.EffectsEnabled = true;
                System.Diagnostics.Debug.WriteLine($"Efeitos restaurados para '{id}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao aplicar configurações de efeitos para '{id}': {ex.Message}");
            }
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
            // Só tenta reproduzir se o volume for maior que zero
            if (player.Reader.Volume > 0.001f)
            {
                player.Play();
            }
        }
    }

    /// <summary>
    /// Para completamente todos os reprodutores (não apenas pausa)
    /// </summary>
    public void StopAll()
    {
        foreach (var player in _audioPlayers.Values)
        {
            player.Stop();
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
            // Limita o volume entre 0.0 e 1.0
            double adjustedVolume = Math.Clamp(volume, 0.0, 1.0);
            
            // Aplica o volume ao reprodutor
            player.AlterarVolume(adjustedVolume);
            
            // Salva o estado do volume para restauração posterior
            _volumeStates[id] = (float)adjustedVolume;
            
            // Verifica se o volume é maior que zero para iniciar a reprodução
            if (adjustedVolume > 0 && player.WaveOut.PlaybackState.Equals(PlaybackState.Stopped))
            {
                player.Play();
            }
            else if (adjustedVolume <= 0 && player.WaveOut.PlaybackState.Equals(PlaybackState.Playing))
            {
                // Pausa a reprodução se o volume for zero
                player.Pause();
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Reprodutor com ID '{id}' não encontrado ao ajustar volume");
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
    /// Salva o estado atual dos equalizadores e efeitos
    /// </summary>
    public void SalvarEstadoEfeitos()
    {
        foreach (var entry in _audioPlayers)
        {
            // Salva configurações do equalizador
            var equalizer = entry.Value.Equalizer;
            if (equalizer != null)
            {
                var bands = equalizer.Bands;
                var gains = new float[bands.Length];
                for (int i = 0; i < bands.Length; i++)
                {
                    gains[i] = bands[i].Gain;
                }
                _equalizerSettings[entry.Key] = gains;
            }
            
            // Salva configurações de efeitos
            var effects = entry.Value.EffectsManager;
            if (effects != null)
            {
                _effectSettings[entry.Key] = new EffectSettings
                {
                    TipoEfeito = effects.TipoEfeito,
                    ReverbMix = effects.ReverbMix,
                    ReverbTime = effects.ReverbTime,
                    PitchFactor = effects.PitchFactor,
                    EchoDelay = effects.EchoDelay,
                    EchoMix = effects.EchoMix,
                    FlangerRate = effects.FlangerRate,
                    FlangerDepth = effects.FlangerDepth,
                    Equalizer = new EqualizerSettings
                    {
                        // Obtém os valores de ganho diretamente das bandas do equalizador
                        LowBand = equalizer?.Bands[0].Gain ?? 0,
                        MidBand = equalizer?.Bands[1].Gain ?? 0,
                        HighBand = equalizer?.Bands[2].Gain ?? 0,
                        // Não usar CustomBands já que EqualizadorAudio não parece ter essa propriedade
                        CustomBands = new Dictionary<int, float>()
                    }
                };
            }
        }
        
        // Salva as configurações no armazenamento
        ConfiguracoesApp.SalvarConfiguracoesEqualizer(_equalizerSettings);
        ConfiguracoesApp.SalvarConfiguracoesEfeitos(_effectSettings);
    }
    
    /// <summary>
    /// Limpa todos os reprodutores registrados
    /// </summary>
    public void LimparReprodutores()
    {
        // Salva o estado atual antes de limpar
        SalvarEstadoVolumes();
        SalvarEstadoEfeitos();
        
        // Pausa todos os reprodutores
        PauseAll();
        
        // Remove as referências, mas mantém os estados
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
    
    /// <summary>
    /// Ativa ou desativa o equalizador para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="enabled">True para ativar, false para desativar</param>
    public void AtivarEqualizador(string id, bool enabled)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.EqualizerEnabled = enabled;
        }
    }
    
    /// <summary>
    /// Ajusta uma banda de equalização para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="bandIndex">Índice da banda (0=baixos, 1=médios, 2=agudos)</param>
    /// <param name="gain">Ganho em dB (-15 a +15)</param>
    public void AjustarEqualizador(string id, int bandIndex, float gain)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.AjustarBandaEqualizador(bandIndex, gain);
        }
    }
    
    /// <summary>
    /// Ativa ou desativa efeitos para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="enabled">True para ativar, false para desativar</param>
    public void AtivarEfeitos(string id, bool enabled)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.EffectsEnabled = enabled;
        }
    }
    
    /// <summary>
    /// Define o tipo de efeito para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="tipoEfeito">Tipo de efeito a ser aplicado</param>
    public void DefinirTipoEfeito(string id, TipoEfeito tipoEfeito)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.DefinirTipoEfeito(tipoEfeito);
        }
    }
    
    /// <summary>
    /// Ajusta parâmetros de reverberação para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="mix">Mix de reverberação (0.0 a 1.0)</param>
    /// <param name="time">Tempo de reverberação (0.1 a 10.0 segundos)</param>
    public void AjustarReverb(string id, float mix, float time)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.AjustarReverbMix(mix);
            player.AjustarReverbTime(time);
        }
    }
    
    /// <summary>
    /// Ajusta o fator de alteração de tom (pitch) para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="factor">Fator de pitch (0.5 a 2.0)</param>
    public void AjustarPitch(string id, float factor)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.AjustarPitchFactor(factor);
        }
    }
    
    /// <summary>
    /// Ajusta parâmetros de eco para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="delay">Tempo de atraso em ms (10 a 1000)</param>
    /// <param name="mix">Mix do eco (0.0 a 1.0)</param>
    public void AjustarEcho(string id, float delay, float mix)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.AjustarEcho(delay, mix);
        }
    }
    
    /// <summary>
    /// Ajusta parâmetros de flanger para um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="rate">Taxa em Hz (0.1 a 5.0)</param>
    /// <param name="depth">Profundidade (0.001 a 0.01)</param>
    public void AjustarFlanger(string id, float rate, float depth)
    {
        if (_audioPlayers.TryGetValue(id, out var player))
        {
            player.AjustarFlanger(rate, depth);
        }
    }
}

/// <summary>
/// Armazena configurações de efeitos para um reprodutor
/// </summary>
public class EffectSettings
{
    public TipoEfeito TipoEfeito { get; set; } = TipoEfeito.Nenhum;
    public float ReverbMix { get; set; } = 0.3f;
    public float ReverbTime { get; set; } = 1.0f;
    public float PitchFactor { get; set; } = 1.0f;
    public float EchoDelay { get; set; } = 250f;
    public float EchoMix { get; set; } = 0.5f;
    public float FlangerRate { get; set; } = 0.5f;
    public float FlangerDepth { get; set; } = 0.005f;
    
    // Adicionando referência às configurações de equalização
    public EqualizerSettings Equalizer { get; set; } = new EqualizerSettings();
}

/// <summary>
/// Armazena configurações de equalização para um reprodutor de áudio
/// </summary>
public class EqualizerSettings
{
    // Bandas de equalização padrão
    public float LowBand { get; set; } = 1.0f;     // Frequências baixas (bass)
    public float MidBand { get; set; } = 1.0f;     // Frequências médias
    public float HighBand { get; set; } = 1.0f;    // Frequências altas (treble)
    
    // Configurações de equalização avançada com mais bandas
    public Dictionary<int, float> CustomBands { get; set; } = new Dictionary<int, float>();
    
    /// <summary>
    /// Define um valor de ganho para uma banda de frequência específica
    /// </summary>
    /// <param name="frequencyHz">Frequência central da banda em Hz</param>
    /// <param name="gain">Valor de ganho (normalmente entre 0.0 e 2.0, onde 1.0 é neutro)</param>
    public void SetBandGain(int frequencyHz, float gain)
    {
        if (CustomBands.ContainsKey(frequencyHz))
            CustomBands[frequencyHz] = gain;
        else
            CustomBands.Add(frequencyHz, gain);
    }
    
    /// <summary>
    /// Reseta todas as configurações de equalização para valores neutros
    /// </summary>
    public void Reset()
    {
        LowBand = 1.0f;
        MidBand = 1.0f;
        HighBand = 1.0f;
        CustomBands.Clear();
    }
}
