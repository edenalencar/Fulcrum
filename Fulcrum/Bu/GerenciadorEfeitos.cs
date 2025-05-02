using NAudio.Wave;
using System;

namespace Fulcrum.Bu;

/// <summary>
/// Gerenciador de efeitos de áudio
/// </summary>
public class GerenciadorEfeitos : ISampleProvider, IDisposable
{
    private readonly ISampleProvider _source;
    private TipoEfeito _tipoEfeito = TipoEfeito.Nenhum;
    
    // Componentes de efeito
    private readonly ReverbSampleProvider _reverbProvider;
    private readonly PitchShiftingSampleProvider _pitchProvider;
    private readonly EchoSampleProvider _echoProvider;
    private readonly FlangerSampleProvider _flangerProvider;
    
    // Flag para controlar se cada efeito está ativo
    private bool _reverbEnabled = false;
    private bool _pitchEnabled = false;
    private bool _echoEnabled = false;
    private bool _flangerEnabled = false;
    
    // Configurações para os efeitos
    private float _reverbMix = 0.3f;
    private float _reverbTime = 1.0f;
    private float _pitchFactor = 1.0f;
    private float _echoDelay = 250f;
    private float _echoMix = 0.5f;
    private float _flangerRate = 0.5f;
    private float _flangerDepth = 0.005f;
    
    // Referência ao equalizador
    public EqualizadorAudio Equalizer { get; private set; }

    /// <summary>
    /// Inicializa uma nova instância do gerenciador de efeitos
    /// </summary>
    /// <param name="source">Fonte de áudio</param>
    public GerenciadorEfeitos(ISampleProvider source)
    {
        _source = source;
        
        // Define as bandas do equalizador
        var bands = new EqualizerBand[]
        {
            new EqualizerBand(100, 1.0f, 0, "Baixa"),
            new EqualizerBand(1000, 1.0f, 0, "Média"),
            new EqualizerBand(8000, 1.0f, 0, "Alta")
        };
        Equalizer = new EqualizadorAudio(_source, bands);
        
        // Inicializa os provedores de efeitos
        _reverbProvider = new ReverbSampleProvider(_source);
        _pitchProvider = new PitchShiftingSampleProvider(_source);
        _echoProvider = new EchoSampleProvider(_source);
        _flangerProvider = new FlangerSampleProvider(_source);
        
        // Aplica as configurações iniciais aos provedores
        _reverbProvider.ReverbMix = _reverbMix;
        _reverbProvider.ReverbTime = _reverbTime;
        _reverbProvider.IsEnabled = false; // Garante que o efeito começa desativado
        
        _pitchProvider.PitchFactor = _pitchFactor;
        _echoProvider.Delay = _echoDelay;
        _echoProvider.Mix = _echoMix;
        _flangerProvider.Rate = _flangerRate;
        _flangerProvider.Depth = _flangerDepth;
        
        // Log para diagnóstico
        System.Diagnostics.Debug.WriteLine("[EFFECTS] Gerenciador de efeitos inicializado com todos os efeitos desativados");
    }

    /// <summary>
    /// Obtém o formato de onda
    /// </summary>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Tipo de efeito selecionado
    /// </summary>
    public TipoEfeito TipoEfeito
    {
        get => _tipoEfeito;
        set
        {
            if (_tipoEfeito != value)
            {
                TipoEfeito oldValue = _tipoEfeito;
                _tipoEfeito = value;
                
                // Se estiver mudando para outro efeito, limpa qualquer resíduo do efeito anterior
                if (oldValue == TipoEfeito.Reverb && value != TipoEfeito.Reverb)
                {
                    _reverbProvider.IsEnabled = false;
                    _reverbProvider.LimparBuffer();
                }
                
                AtualizarConfiguracaoEfeitos();
                System.Diagnostics.Debug.WriteLine($"[EFFECTS] Tipo de efeito alterado: {oldValue} -> {value}");
            }
        }
    }

    // Propriedades para reverb
    public float ReverbMix
    {
        get => _reverbMix;
        set
        {
            float oldValue = _reverbMix;
            _reverbMix = Math.Clamp(value, 0f, 1f);
            
            if (_tipoEfeito == TipoEfeito.Reverb)
            {
                _reverbProvider.ReverbMix = _reverbMix;
                System.Diagnostics.Debug.WriteLine($"[REVERB] Mix alterado: {oldValue} -> {_reverbMix}");
            }
        }
    }
    
    public float ReverbTime
    {
        get => _reverbTime;
        set
        {
            float oldValue = _reverbTime;
            _reverbTime = Math.Clamp(value, 0.1f, 10f);
            
            if (_tipoEfeito == TipoEfeito.Reverb)
            {
                _reverbProvider.ReverbTime = _reverbTime;
                System.Diagnostics.Debug.WriteLine($"[REVERB] DecayTime alterado: {oldValue} -> {_reverbTime}");
            }
        }
    }
    
    // Propriedade para pitch
    public float PitchFactor
    {
        get => _pitchFactor;
        set
        {
            _pitchFactor = Math.Clamp(value, 0.5f, 2.0f);
            if (_tipoEfeito == TipoEfeito.Pitch)
            {
                _pitchProvider.PitchFactor = _pitchFactor;
            }
        }
    }
    
    // Propriedades para echo
    public float EchoDelay
    {
        get => _echoDelay;
        set
        {
            _echoDelay = Math.Clamp(value, 10f, 1000f);
            if (_tipoEfeito == TipoEfeito.Echo)
            {
                _echoProvider.Delay = _echoDelay;
            }
        }
    }
    
    public float EchoMix
    {
        get => _echoMix;
        set
        {
            _echoMix = Math.Clamp(value, 0f, 1f);
            if (_tipoEfeito == TipoEfeito.Echo)
            {
                _echoProvider.Mix = _echoMix;
            }
        }
    }
    
    // Propriedades para flanger
    public float FlangerRate
    {
        get => _flangerRate;
        set
        {
            _flangerRate = Math.Clamp(value, 0.1f, 5.0f);
            if (_tipoEfeito == TipoEfeito.Flanger)
            {
                _flangerProvider.Rate = _flangerRate;
            }
        }
    }
    
    public float FlangerDepth
    {
        get => _flangerDepth;
        set
        {
            _flangerDepth = Math.Clamp(value, 0.001f, 0.01f);
            if (_tipoEfeito == TipoEfeito.Flanger)
            {
                _flangerProvider.Depth = _flangerDepth;
            }
        }
    }

    /// <summary>
    /// Aplica as configurações ao efeito atualmente selecionado
    /// </summary>
    private void AtualizarConfiguracaoEfeitos()
    {
        // Desativa todos os efeitos primeiro
        _reverbEnabled = false;
        _pitchEnabled = false;
        _echoEnabled = false;
        _flangerEnabled = false;
        
        // Desativa explicitamente todos os provedores de efeitos
        _reverbProvider.IsEnabled = false;
        _pitchProvider.IsEnabled = false;
        _echoProvider.IsEnabled = false;
        _flangerProvider.IsEnabled = false;
        
        // Ativa o efeito selecionado e aplica suas configurações
        switch (_tipoEfeito)
        {
            case TipoEfeito.Reverb:
                _reverbEnabled = true;
                _reverbProvider.ReverbMix = _reverbMix;
                _reverbProvider.ReverbTime = _reverbTime;
                _reverbProvider.IsEnabled = true; // Ativa explicitamente o efeito
                
                // Log para diagnóstico
                System.Diagnostics.Debug.WriteLine($"[REVERB] Efeito ativado com Mix={_reverbMix}, Time={_reverbTime}");
                break;
                
            case TipoEfeito.Pitch:
                _pitchEnabled = true;
                _pitchProvider.IsEnabled = true;
                _pitchProvider.PitchFactor = _pitchFactor;
                break;
                
            case TipoEfeito.Echo:
                _echoEnabled = true;
                _echoProvider.IsEnabled = true;
                _echoProvider.Delay = _echoDelay;
                _echoProvider.Mix = _echoMix;
                break;
                
            case TipoEfeito.Flanger:
                _flangerEnabled = true;
                _flangerProvider.IsEnabled = true;
                _flangerProvider.Rate = _flangerRate;
                _flangerProvider.Depth = _flangerDepth;
                break;
            
            case TipoEfeito.Nenhum:
                // Garante que nenhum efeito está aplicado
                System.Diagnostics.Debug.WriteLine("[EFFECTS] Todos os efeitos desativados");
                break;
        }
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito selecionado
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead;
        
        // Em vez de ler e processar, passa diretamente para o provedor de efeito ativo
        switch (_tipoEfeito)
        {
            case TipoEfeito.Reverb:
                if (_reverbEnabled)
                {
                    samplesRead = _reverbProvider.Read(buffer, offset, count);
                    return samplesRead;
                }
                break;
                
            case TipoEfeito.Pitch:
                if (_pitchEnabled)
                {
                    samplesRead = _pitchProvider.Read(buffer, offset, count);
                    return samplesRead;
                }
                break;
                
            case TipoEfeito.Echo:
                if (_echoEnabled)
                {
                    samplesRead = _echoProvider.Read(buffer, offset, count);
                    return samplesRead;
                }
                break;
                
            case TipoEfeito.Flanger:
                if (_flangerEnabled)
                {
                    samplesRead = _flangerProvider.Read(buffer, offset, count);
                    return samplesRead;
                }
                break;
        }
        
        // Se nenhum efeito estiver ativo, lê diretamente da fonte
        samplesRead = _source.Read(buffer, offset, count);
        return samplesRead;
    }

    /// <summary>
    /// Libera os recursos utilizados pelo gerenciador de efeitos
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Desativa todos os efeitos antes de liberar recursos
            _reverbProvider.IsEnabled = false;
            _pitchProvider.IsEnabled = false;
            _echoProvider.IsEnabled = false;
            _flangerProvider.IsEnabled = false;
            
            // Garante que nenhum resíduo permaneça no buffer de reverberação
            _reverbProvider.LimparBuffer();
            
            System.Diagnostics.Debug.WriteLine("Recursos do gerenciador de efeitos liberados com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao liberar recursos do gerenciador de efeitos: {ex.Message}");
        }
    }
}