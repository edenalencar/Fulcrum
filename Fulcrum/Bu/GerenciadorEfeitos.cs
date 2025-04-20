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
                _tipoEfeito = value;
                AtualizarConfiguracaoEfeitos();
            }
        }
    }

    // Propriedades para reverb
    public float ReverbMix
    {
        get => _reverbMix;
        set
        {
            _reverbMix = Math.Clamp(value, 0f, 1f);
            if (_tipoEfeito == TipoEfeito.Reverb)
            {
                _reverbProvider.ReverbMix = _reverbMix;
            }
        }
    }
    
    public float ReverbTime
    {
        get => _reverbTime;
        set
        {
            _reverbTime = Math.Clamp(value, 0.1f, 10f);
            if (_tipoEfeito == TipoEfeito.Reverb)
            {
                _reverbProvider.ReverbTime = _reverbTime;
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
        _reverbProvider.IsEnabled = false;
        _pitchProvider.IsEnabled = false;
        _echoProvider.IsEnabled = false;
        _flangerProvider.IsEnabled = false;
        
        // Ativa o efeito selecionado e aplica suas configurações
        switch (_tipoEfeito)
        {
            case TipoEfeito.Reverb:
                _reverbProvider.IsEnabled = true;
                _reverbProvider.ReverbMix = _reverbMix;
                _reverbProvider.ReverbTime = _reverbTime;
                break;
                
            case TipoEfeito.Pitch:
                _pitchProvider.IsEnabled = true;
                _pitchProvider.PitchFactor = _pitchFactor;
                break;
                
            case TipoEfeito.Echo:
                _echoProvider.IsEnabled = true;
                _echoProvider.Delay = _echoDelay;
                _echoProvider.Mix = _echoMix;
                break;
                
            case TipoEfeito.Flanger:
                _flangerProvider.IsEnabled = true;
                _flangerProvider.Rate = _flangerRate;
                _flangerProvider.Depth = _flangerDepth;
                break;
        }
    }

    /// <summary>
    /// Lê amostras de áudio e aplica o efeito selecionado
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);
        
        if (samplesRead > 0)
        {
            // Aplica o efeito correspondente
            switch (_tipoEfeito)
            {
                case TipoEfeito.Reverb:
                    _reverbProvider.ProcessInPlace(buffer, offset, samplesRead);
                    break;
                    
                case TipoEfeito.Pitch:
                    _pitchProvider.ProcessInPlace(buffer, offset, samplesRead);
                    break;
                    
                case TipoEfeito.Echo:
                    _echoProvider.ProcessInPlace(buffer, offset, samplesRead);
                    break;
                    
                case TipoEfeito.Flanger:
                    _flangerProvider.ProcessSamples(buffer, offset, samplesRead);
                    break;
            }
        }
        
        return samplesRead;
    }

    /// <summary>
    /// Libera os recursos utilizados pelo gerenciador de efeitos
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Libera recursos específicos dos efeitos, se houver
            System.Diagnostics.Debug.WriteLine("Recursos do gerenciador de efeitos liberados com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao liberar recursos do gerenciador de efeitos: {ex.Message}");
        }
    }
}