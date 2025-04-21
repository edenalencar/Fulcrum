using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
/// Tipo de efeito aplicável a um reprodutor de áudio
/// </summary>
public enum TipoEfeito
{
    /// <summary>
    /// Sem efeito
    /// </summary>
    Nenhum = 0,
    
    /// <summary>
    /// Efeito de reverberação
    /// </summary>
    Reverb = 1,
    
    /// <summary>
    /// Efeito de eco
    /// </summary>
    Echo = 2,
    
    /// <summary>
    /// Efeito de alteração de tom
    /// </summary>
    Pitch = 3,
    
    /// <summary>
    /// Efeito de flanger
    /// </summary>
    Flanger = 4
}

/// <summary>
/// Gerenciador de áudio central do aplicativo
/// </summary>
public sealed class AudioManager
{
    private static readonly Lazy<AudioManager> _instance = new(() => new AudioManager());
    private readonly ConcurrentDictionary<string, Reprodutor> _audioPlayers = new();
    private readonly ConcurrentDictionary<string, float> _volumeStates = new();
    // Dicionário para rastrear o estado de reprodução individual de cada reprodutor
    private readonly ConcurrentDictionary<string, PlaybackState> _playerStates = new();
    private const string VOLUME_SETTINGS_KEY = "VolumeSettings";
    private const string EFFECTS_SETTINGS_KEY = "EffectsSettings";

    // Armazena configurações de equalizador para salvar/restaurar
    private ConcurrentDictionary<string, float[]> _equalizerSettings = new();
    // Armazena configurações de efeitos para salvar/restaurar
    private ConcurrentDictionary<string, EffectSettings> _effectSettings = new();
    
    // Flag para garantir que todos os reprodutores iniciem com volume zero
    private bool _forceInitialVolumeZero = true;
    
    // Estado global de reprodução
    private PlaybackState _globalPlaybackState = PlaybackState.Stopped;
    
    // Flag para controle de mudo global
    private bool _isMuted = false;
    private readonly ConcurrentDictionary<string, float> _preMuteVolumes = new();

    // Armazena estados de reprodução individuais
    private readonly ConcurrentDictionary<string, bool> _playbackStates = new();

    // Construtor privado para implementar o padrão Singleton
    private AudioManager() 
    {
        // Inicializar todos os volumes para zero antes de carregar configurações salvas
        _volumeStates.Clear();
        
        // Flag para forçar volume zero na primeira execução é ativada por padrão
        _forceInitialVolumeZero = true;
        
        // Carregar configurações salvas de volume na inicialização
        CarregarVolumes();
        
        // Carregar configurações salvas de efeitos na inicialização
        CarregarEstadoEfeitos();
        
        System.Diagnostics.Debug.WriteLine("AudioManager: Inicializado com regra de volume zero");
    }

    /// <summary>
    /// Instância única do AudioManager (Singleton)
    /// </summary>
    public static AudioManager Instance => _instance.Value;
    
    /// <summary>
    /// Indica se pelo menos um som está em reprodução
    /// </summary>
    public bool IsPlaying => _globalPlaybackState == PlaybackState.Playing && 
                           _audioPlayers.Values.Any(p => p.WaveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing);
    
    /// <summary>
    /// Indica se o áudio está em mudo
    /// </summary>
    public bool IsMuted => _isMuted;
    
    /// <summary>
    /// Retorna a quantidade de reprodutores registrados
    /// </summary>
    public int GetQuantidadeReprodutor => _audioPlayers.Count;

    /// <summary>
    /// Adiciona um reprodutor de áudio ao gerenciador
    /// </summary>
    /// <param name="id">Identificador único do reprodutor</param>
    /// <param name="player">Reprodutor a ser adicionado</param>
    public void AddAudioPlayer(string id, Reprodutor player)
    {
        if (_audioPlayers.TryGetValue(id, out var existingPlayer))
        {
            existingPlayer.Dispose();
        }

        _audioPlayers[id] = player;
        
        // Inicializa o estado de reprodução como parado
        _playerStates[id] = PlaybackState.Stopped;
        
        // Define o volume inicial como zero para todos os reprodutores
        player.Reader.Volume = 0f;
        
        // Garante que o player não inicie automaticamente
        if (player.WaveOut != null && player.WaveOut.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
        {
            player.Stop();
        }
        
        System.Diagnostics.Debug.WriteLine($"Reprodutor {id} adicionado com sucesso com volume inicial zero e playback parado");
        
        // Se o dicionário de volumes ainda não foi inicializado ou não tem entradas,
        // forçamos a inicialização com volume zero
        if (_volumeStates.Count == 0)
        {
            _volumeStates[id] = 0f;
            SalvarVolumes();
        }
        // Caso contrário, recuperamos o valor salvo
        else if (_volumeStates.TryGetValue(id, out var savedVolume))
        {
            // Aplicamos o volume salvo apenas se não for a primeira execução
            if (!_forceInitialVolumeZero)
            {
                player.AlterarVolume(savedVolume);
                System.Diagnostics.Debug.WriteLine($"Volume restaurado para {id}: {savedVolume}");
            }
            else
            {
                // Forçamos o volume para zero na primeira execução
                _volumeStates[id] = 0f;
                player.AlterarVolume(0f);
                System.Diagnostics.Debug.WriteLine($"Forçando volume inicial para zero em {id}");
            }
        }
        else
        {
            // Garantimos que o volume inicial seja sempre zero
            _volumeStates.TryAdd(id, 0f);
        }
        
        // Restaura as configurações de efeitos salvas anteriormente, se existirem
        if (_effectSettings.TryGetValue(id, out var effectSettings))
        {
            if (effectSettings.EffectsEnabled)
            {
                AtivarEfeitos(id, true);
                DefinirTipoEfeito(id, effectSettings.TipoEfeito);
                
                switch (effectSettings.TipoEfeito)
                {
                    case TipoEfeito.Reverb:
                        AjustarReverb(id, effectSettings.ReverbMix, effectSettings.ReverbTime);
                        break;
                    case TipoEfeito.Echo:
                        AjustarEcho(id, effectSettings.EchoDelay, effectSettings.EchoMix);
                        break;
                    case TipoEfeito.Pitch:
                        AjustarPitch(id, effectSettings.PitchFactor);
                        break;
                    case TipoEfeito.Flanger:
                        AjustarFlanger(id, effectSettings.FlangerRate, effectSettings.FlangerDepth);
                        break;
                }
            }
        }
        
        // Restaura as configurações de equalizador salvas anteriormente, se existirem
        if (_equalizerSettings.TryGetValue(id, out var eqSettings) && eqSettings.Length >= 3)
        {
            AjustarEqualizador(id, 0, eqSettings[0]);
            AjustarEqualizador(id, 1, eqSettings[1]);
            AjustarEqualizador(id, 2, eqSettings[2]);
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
        {
            return player;
        }
        throw new KeyNotFoundException($"Reprodutor com ID '{id}' não encontrado");
    }
    
    /// <summary>
    /// Obtém a lista de todos os reprodutores registrados
    /// </summary>
    /// <returns>Dicionário com todos os reprodutores</returns>
    public IReadOnlyDictionary<string, Reprodutor> GetListReprodutores()
    {
        return _audioPlayers;
    }
    
    /// <summary>
    /// Altera o volume de um reprodutor específico
    /// </summary>
    /// <param name="id">Identificador do reprodutor</param>
    /// <param name="volume">Novo volume (0.0 a 1.0)</param>
    public void AlterarVolume(string id, float volume)
    {
        try
        {
            if (_audioPlayers.TryGetValue(id, out var player))
            {
                // Limita o volume entre 0 e 1
                volume = Math.Clamp(volume, 0f, 1f);
                player.AlterarVolume(volume);
                
                // Salva o novo estado do volume
                _volumeStates[id] = volume;
                SalvarVolumes();
                
                // Inicia a reprodução automaticamente se o volume for maior que zero
                if (volume > 0.001f && player.WaveOut?.PlaybackState != NAudio.Wave.PlaybackState.Playing)
                {
                    player.Play();
                    _playerStates[id] = PlaybackState.Playing;
                    _globalPlaybackState = PlaybackState.Playing;
                    System.Diagnostics.Debug.WriteLine($"Reprodução iniciada automaticamente para '{id}' ao ajustar volume para {volume}");
                }
                // Para a reprodução automaticamente se o volume for zero
                else if (volume <= 0.001f && player.WaveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                {
                    player.Pause();
                    _playerStates[id] = PlaybackState.Paused;
                    
                    // Verifica se ainda há algum player tocando
                    if (!_audioPlayers.Values.Any(p => p.WaveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing))
                    {
                        _globalPlaybackState = PlaybackState.Paused;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Volume do reprodutor '{id}' alterado para {volume}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AVISO: Tentativa de alterar volume de reprodutor inexistente '{id}'");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao alterar volume do reprodutor '{id}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Aumenta o volume de todos os reprodutores pela quantidade especificada
    /// </summary>
    /// <param name="increment">Quantidade a aumentar (0.0 a 1.0)</param>
    public void IncreaseMainVolume(float increment)
    {
        foreach (var pair in _audioPlayers)
        {
            var currentVolume = pair.Value.Reader.Volume;
            var newVolume = Math.Clamp(currentVolume + increment, 0f, 1f);
            AlterarVolume(pair.Key, newVolume);
        }
        
        System.Diagnostics.Debug.WriteLine($"Volume global aumentado em {increment}");
    }
    
    /// <summary>
    /// Diminui o volume de todos os reprodutores pela quantidade especificada
    /// </summary>
    /// <param name="decrement">Quantidade a diminuir (0.0 a 1.0)</param>
    public void DecreaseMainVolume(float decrement)
    {
        foreach (var pair in _audioPlayers)
        {
            var currentVolume = pair.Value.Reader.Volume;
            var newVolume = Math.Clamp(currentVolume - decrement, 0f, 1f);
            AlterarVolume(pair.Key, newVolume);
        }
        
        System.Diagnostics.Debug.WriteLine($"Volume global diminuído em {decrement}");
    }
    
    /// <summary>
    /// Alterna entre mudo e com som para todos os reprodutores
    /// </summary>
    public void ToggleMute()
    {
        if (_isMuted)
        {
            // Restaura volumes anteriores
            foreach (var pair in _preMuteVolumes)
            {
                if (_audioPlayers.ContainsKey(pair.Key))
                {
                    AlterarVolume(pair.Key, pair.Value);
                }
            }
            _preMuteVolumes.Clear();
        }
        else
        {
            // Salva volumes atuais e muta tudo
            _preMuteVolumes.Clear();
            foreach (var pair in _audioPlayers)
            {
                _preMuteVolumes[pair.Key] = pair.Value.Reader.Volume;
                AlterarVolume(pair.Key, 0f);
            }
        }
        
        _isMuted = !_isMuted;
        System.Diagnostics.Debug.WriteLine($"Áudio global {(_isMuted ? "mutado" : "desmutado")}");
    }
    
    /// <summary>
    /// Inicia a reprodução de todos os sons
    /// </summary>
    public void PlayAll()
    {
        // Reproduzir apenas sons que estavam em estado de reprodução ou que têm volume acima de zero
        foreach (var playerEntry in _audioPlayers)
        {
            string soundId = playerEntry.Key;
            Reprodutor player = playerEntry.Value;
            
            // Verifica se o reprodutor estava em estado de reprodução ou se tem volume suficiente para tocar
            if (_playerStates.TryGetValue(soundId, out var state) && 
                (state == PlaybackState.Playing || 
                (state == PlaybackState.Stopped && player.Reader.Volume > 0.001f)))
            {
                player.Play();
                _playerStates[soundId] = PlaybackState.Playing;
            }
        }
        
        // Atualiza o estado global somente se pelo menos um reprodutor estiver tocando
        if (_audioPlayers.Any(p => p.Value.WaveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing))
        {
            _globalPlaybackState = PlaybackState.Playing;
            System.Diagnostics.Debug.WriteLine("Iniciada reprodução respeitando estados anteriores dos reprodutores");
        }
    }
    
    /// <summary>
    /// Pausa a reprodução de todos os sons
    /// </summary>
    public void PauseAll()
    {
        foreach (var playerEntry in _audioPlayers)
        {
            playerEntry.Value.Pause();
            _playerStates[playerEntry.Key] = PlaybackState.Paused;
        }
        
        _globalPlaybackState = PlaybackState.Paused;
        System.Diagnostics.Debug.WriteLine("Pausada reprodução de todos os sons");
    }
    
    /// <summary>
    /// Reproduz um som específico
    /// </summary>
    public void Play(string soundId)
    {
        if (_audioPlayers.TryGetValue(soundId, out var player))
        {
            player.Play();
            _playerStates[soundId] = PlaybackState.Playing;
            
            // Atualiza o estado global se pelo menos um player estiver tocando
            _globalPlaybackState = PlaybackState.Playing;
        }
    }
    
    /// <summary>
    /// Pausa um som específico
    /// </summary>
    public void Pause(string soundId)
    {
        if (_audioPlayers.TryGetValue(soundId, out var player))
        {
            player.Pause();
            _playerStates[soundId] = PlaybackState.Paused;
            
            // Verifica se ainda há algum player tocando
            if (!_audioPlayers.Values.Any(p => p.WaveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing))
            {
                _globalPlaybackState = PlaybackState.Paused;
            }
        }
    }
    
    /// <summary>
    /// Alterna entre reprodução e pausa para todos os sons
    /// </summary>
    public void TogglePlayback()
    {
        if (IsPlaying)
        {
            PauseAll();
        }
        else
        {
            PlayAll();
        }
    }
    
    /// <summary>
    /// Remove um reprodutor de áudio do gerenciador
    /// </summary>
    /// <param name="id">Identificador do reprodutor a ser removido</param>
    public void RemoveAudioPlayer(string id)
    {
        try
        {
            if (_audioPlayers.TryRemove(id, out var player))
            {
                player.Dispose();
                System.Diagnostics.Debug.WriteLine($"Reprodutor '{id}' removido com sucesso");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AVISO: Tentativa de remover reprodutor inexistente '{id}'");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao remover reprodutor '{id}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Salva o estado atual dos volumes para persistência
    /// </summary>
    public void SalvarEstadoVolumes()
    {
        SalvarVolumes();
    }
    
    /// <summary>
    /// Salva os estados de volume atuais para persistência
    /// </summary>
    private void SalvarVolumes()
    {
        try
        {
            var json = JsonSerializer.Serialize(_volumeStates);
            ApplicationData.Current.LocalSettings.Values[VOLUME_SETTINGS_KEY] = json;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações de volume: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Carrega os estados de volume salvos anteriormente
    /// </summary>
    private void CarregarVolumes()
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(VOLUME_SETTINGS_KEY, out var value) && 
                value is string json)
            {
                var volumes = JsonSerializer.Deserialize<Dictionary<string, float>>(json);
                if (volumes != null)
                {
                    // Limpa o dicionário existente
                    _volumeStates.Clear();
                    
                    // Adiciona os itens um a um
                    foreach (var item in volumes)
                    {
                        _volumeStates.TryAdd(item.Key, item.Value);
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Configurações de volume carregadas com sucesso");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações de volume: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Inicializa todos os reprodutores com um volume específico
    /// </summary>
    /// <param name="initialVolume">Volume inicial para todos os reprodutores (0.0 a 1.0)</param>
    public void InitializePlayers(double initialVolume)
    {
        // Força volume zero na primeira execução
        float volume = _forceInitialVolumeZero ? 0.0f : (float)Math.Clamp(initialVolume, 0.0, 1.0);
        
        // Se os volumes já foram carregados das configurações, mas queremos forçar volume zero
        if (_volumeStates.Count > 0 && _forceInitialVolumeZero)
        {
            System.Diagnostics.Debug.WriteLine("Forçando volume inicial zero para todos os reprodutores...");
            
            // Reseta todos os volumes para zero
            foreach (var pair in _audioPlayers)
            {
                // Define o volume
                pair.Value.AlterarVolume(0.0f);
                
                // Garante que o player esteja parado
                if (pair.Value.WaveOut != null && pair.Value.WaveOut.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
                {
                    pair.Value.Stop();
                }
                
                // Atualiza o dicionário de estados de volume
                _volumeStates[pair.Key] = 0.0f;
                
                // Marca o estado de reprodução como parado
                _playerStates[pair.Key] = PlaybackState.Stopped;
            }
            
            // Atualiza o estado global
            _globalPlaybackState = PlaybackState.Stopped;
            
            // Salva os novos estados de volume
            SalvarVolumes();
            
            // Desativa o flag para futuras execuções
            _forceInitialVolumeZero = false;
            
            System.Diagnostics.Debug.WriteLine("Todos os reprodutores foram inicializados com volume zero");
            return;
        }
        
        // Se os volumes já foram carregados das configurações e não queremos forçar volume zero
        if (_volumeStates.Count > 0 && !_forceInitialVolumeZero)
        {
            System.Diagnostics.Debug.WriteLine("Volumes já carregados de configurações anteriores, preservando estado...");
            return;
        }
        
        // Aplica o volume inicial a todos os reprodutores e interrompe qualquer reprodução
        foreach (var pair in _audioPlayers)
        {
            // Define o volume
            pair.Value.AlterarVolume(volume);
            
            // Garante que o player esteja parado
            if (pair.Value.WaveOut != null && pair.Value.WaveOut.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
            {
                pair.Value.Stop();
            }
            
            // Atualiza o dicionário de estados de volume
            _volumeStates[pair.Key] = volume;
            
            // Marca o estado de reprodução como parado
            _playerStates[pair.Key] = PlaybackState.Stopped;
        }
        
        // Atualiza o estado global
        _globalPlaybackState = PlaybackState.Stopped;
        
        // Salva os novos estados de volume
        SalvarVolumes();
        
        // Após a inicialização, desativa o flag para futuras execuções
        _forceInitialVolumeZero = false;
        
        System.Diagnostics.Debug.WriteLine($"Todos os reprodutores inicializados com volume {volume} e reprodução interrompida");
    }
    
    #region Métodos de Equalizador e Efeitos
    
    /// <summary>
    /// Ativa ou desativa o equalizador para um reprodutor específico
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="enabled">True para ativar, false para desativar</param>
    public void AtivarEqualizador(string soundId, bool enabled)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.EqualizerEnabled = enabled;
            
            // Salva o estado atual
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Equalizador {(enabled ? "ativado" : "desativado")} para {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao {(enabled ? "ativar" : "desativar")} equalizador: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ajusta uma banda específica do equalizador para um reprodutor
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="bandIndex">Índice da banda (0=baixos, 1=médios, 2=agudos)</param>
    /// <param name="gain">Ganho em dB (-15 a +15)</param>
    public void AjustarEqualizador(string soundId, int bandIndex, float gain)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.AjustarBandaEqualizador(bandIndex, gain);
            
            // Salva o estado atual do equalizador
            if (!_equalizerSettings.ContainsKey(soundId))
            {
                _equalizerSettings[soundId] = new float[3] { 0f, 0f, 0f };
            }
            
            _equalizerSettings[soundId][bandIndex] = gain;
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Banda {bandIndex} ajustada para {gain}dB em {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar equalizador: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ativa ou desativa os efeitos para um reprodutor específico
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="enabled">True para ativar, false para desativar</param>
    public void AtivarEfeitos(string soundId, bool enabled)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.EffectsEnabled = enabled;
            
            // Inicializa ou atualiza as configurações de efeitos
            if (!_effectSettings.ContainsKey(soundId))
            {
                _effectSettings[soundId] = new EffectSettings();
            }
            
            _effectSettings[soundId].EffectsEnabled = enabled;
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Efeitos {(enabled ? "ativados" : "desativados")} para {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao {(enabled ? "ativar" : "desativar")} efeitos: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Define o tipo de efeito a ser aplicado a um reprodutor
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="tipoEfeito">Tipo de efeito</param>
    public void DefinirTipoEfeito(string soundId, TipoEfeito tipoEfeito)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.DefinirTipoEfeito(tipoEfeito);
            
            // Salva o tipo de efeito atual
            if (!_effectSettings.ContainsKey(soundId))
            {
                _effectSettings[soundId] = new EffectSettings();
            }
            
            _effectSettings[soundId].TipoEfeito = tipoEfeito;
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Tipo de efeito definido para {tipoEfeito} em {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao definir tipo de efeito: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ajusta os parâmetros de reverberação para um reprodutor
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="mix">Mix de reverberação (0.0 a 1.0)</param>
    /// <param name="time">Tempo de reverberação (0.1 a 10.0 segundos)</param>
    public void AjustarReverb(string soundId, float mix, float time)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.AjustarReverbMix(mix);
            reprodutor.AjustarReverbTime(time);
            
            // Salva os parâmetros atuais
            if (!_effectSettings.ContainsKey(soundId))
            {
                _effectSettings[soundId] = new EffectSettings();
            }
            
            _effectSettings[soundId].ReverbMix = mix;
            _effectSettings[soundId].ReverbTime = time;
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Reverb ajustado para mix={mix}, time={time}s em {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar reverb: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ajusta os parâmetros de eco para um reprodutor
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="delay">Atraso em ms (10 a 1000)</param>
    /// <param name="mix">Mix de eco (0.0 a 1.0)</param>
    public void AjustarEcho(string soundId, float delay, float mix)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.AjustarEcho(delay, mix);
            
            // Salva os parâmetros atuais
            if (!_effectSettings.ContainsKey(soundId))
            {
                _effectSettings[soundId] = new EffectSettings();
            }
            
            _effectSettings[soundId].EchoDelay = delay;
            _effectSettings[soundId].EchoMix = mix;
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Echo ajustado para delay={delay}ms, mix={mix} em {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar echo: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ajusta o fator de pitch para um reprodutor
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="factor">Fator de pitch (0.5 a 2.0)</param>
    public void AjustarPitch(string soundId, float factor)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.AjustarPitchFactor(factor);
            
            // Salva o parâmetro atual
            if (!_effectSettings.ContainsKey(soundId))
            {
                _effectSettings[soundId] = new EffectSettings();
            }
            
            _effectSettings[soundId].PitchFactor = factor;
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Pitch ajustado para factor={factor} em {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar pitch: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ajusta os parâmetros de flanger para um reprodutor
    /// </summary>
    /// <param name="soundId">ID do reprodutor</param>
    /// <param name="rate">Taxa em Hz (0.1 a 5.0)</param>
    /// <param name="depth">Profundidade (0.001 a 0.01)</param>
    public void AjustarFlanger(string soundId, float rate, float depth)
    {
        try
        {
            var reprodutor = GetReprodutorPorId(soundId);
            reprodutor.AjustarFlanger(rate, depth);
            
            // Salva os parâmetros atuais
            if (!_effectSettings.ContainsKey(soundId))
            {
                _effectSettings[soundId] = new EffectSettings();
            }
            
            _effectSettings[soundId].FlangerRate = rate;
            _effectSettings[soundId].FlangerDepth = depth;
            SalvarEstadoEfeitos();
            
            System.Diagnostics.Debug.WriteLine($"Flanger ajustado para rate={rate}Hz, depth={depth} em {soundId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao ajustar flanger: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Salva o estado atual dos efeitos e equalizadores
    /// </summary>
    public void SalvarEstadoEfeitos()
    {
        try
        {
            // Salva as configurações de efeitos
            var effectsJson = JsonSerializer.Serialize(_effectSettings);
            ApplicationData.Current.LocalSettings.Values[EFFECTS_SETTINGS_KEY + "_Effects"] = effectsJson;
            
            // Salva as configurações de equalizador
            var eqJson = JsonSerializer.Serialize(_equalizerSettings);
            ApplicationData.Current.LocalSettings.Values[EFFECTS_SETTINGS_KEY + "_EQ"] = eqJson;
            
            System.Diagnostics.Debug.WriteLine("Estado de efeitos e equalizador salvo com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações de efeitos: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Carrega o estado salvo dos efeitos e equalizadores
    /// </summary>
    private void CarregarEstadoEfeitos()
    {
        try
        {
            // Carrega as configurações de efeitos
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(EFFECTS_SETTINGS_KEY + "_Effects", out var effectsValue) && 
                effectsValue is string effectsJson)
            {
                var effects = JsonSerializer.Deserialize<ConcurrentDictionary<string, EffectSettings>>(effectsJson);
                if (effects != null)
                {
                    _effectSettings.Clear();
                    foreach (var item in effects)
                    {
                        _effectSettings.TryAdd(item.Key, item.Value);
                    }
                }
            }
            
            // Carrega as configurações de equalizador
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(EFFECTS_SETTINGS_KEY + "_EQ", out var eqValue) && 
                eqValue is string eqJson)
            {
                var eq = JsonSerializer.Deserialize<ConcurrentDictionary<string, float[]>>(eqJson);
                if (eq != null)
                {
                    _equalizerSettings.Clear();
                    foreach (var item in eq)
                    {
                        _equalizerSettings.TryAdd(item.Key, item.Value);
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine("Configurações de efeitos carregadas com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações de efeitos: {ex.Message}");
        }
    }
    
    #endregion
    
    /// <summary>
    /// Libera todos os recursos utilizados
    /// </summary>
    public void Dispose()
    {
        foreach (var player in _audioPlayers.Values)
        {
            player.Dispose();
        }
        _audioPlayers.Clear();
        System.Diagnostics.Debug.WriteLine("AudioManager: todos os recursos liberados");
    }
    
    /// <summary>
    /// Obtém o estado de reprodução específico por player
    /// </summary>
    public bool IsPlayerPlaying(string soundId)
    {
        if (_audioPlayers.TryGetValue(soundId, out var player))
        {
            return player.WaveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing;
        }
        return false;
    }
}

/// <summary>
/// Armazena configurações de efeitos para um reprodutor
/// </summary>
public class EffectSettings
{
    /// <summary>
    /// Tipo de efeito ativo
    /// </summary>
    public TipoEfeito TipoEfeito { get; set; } = TipoEfeito.Nenhum;
    
    /// <summary>
    /// Nível de reverberação (0.0 a 1.0)
    /// </summary>
    public float ReverbMix { get; set; } = 0.3f;
    
    /// <summary>
    /// Tempo de reverberação (0.1 a 10.0 segundos)
    /// </summary>
    public float ReverbTime { get; set; } = 1.0f;
    
    /// <summary>
    /// Tempo de atraso do eco (10 a 1000 ms)
    /// </summary>
    public float EchoDelay { get; set; } = 250f;
    
    /// <summary>
    /// Mix do eco (0.0 a 1.0)
    /// </summary>
    public float EchoMix { get; set; } = 0.5f;
    
    /// <summary>
    /// Fator de alteração de tom (0.5 a 2.0)
    /// </summary>
    public float PitchFactor { get; set; } = 1.0f;
    
    /// <summary>
    /// Taxa de flanger em Hz (0.1 a 5.0)
    /// </summary>
    public float FlangerRate { get; set; } = 0.5f;
    
    /// <summary>
    /// Profundidade de flanger (0.001 a 0.01)
    /// </summary>
    public float FlangerDepth { get; set; } = 0.005f;
    
    /// <summary>
    /// Indica se os efeitos estão habilitados
    /// </summary>
    public bool EffectsEnabled { get; set; } = false;
}

/// <summary>
/// Armazena configurações de equalização para um reprodutor de áudio
/// </summary>
public class EqualizerSettings
{
    /// <summary>
    /// Valores de ganho para cada banda (dB)
    /// </summary>
    public float[] BandGains { get; set; } = new float[3] { 0f, 0f, 0f };
    
    /// <summary>
    /// Indica se o equalizador está habilitado
    /// </summary>
    public bool EqualizerEnabled { get; set; } = false;
}
