using Fulcrum.Bu;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;

namespace Fulcrum.View;

/// <summary>
/// Página para ajustar configurações de equalização e efeitos para um reprodutor específico
/// </summary>
public sealed partial class EqualizadorEfeitosPage : Page
{
    private string _currentSoundId = "";
    private Reprodutor? _currentPlayer;
    private bool _isInitializing = true;

    public EqualizadorEfeitosPage()
    {
        this.InitializeComponent();
        Debug.WriteLine("EqualizadorEfeitosPage: Inicialização");
    }

    /// <summary>
    /// Manipula o evento de navegação para esta página
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Recebe o ID do som como parâmetro
        if (e.Parameter is string soundId)
        {
            _currentSoundId = soundId;
            _currentPlayer = AudioManager.Instance.GetReprodutorPorId(soundId);

            if (_currentPlayer != null)
            {
                // Atualiza o título da página
                soundNameTextBlock.Text = ObterNomeLegivel(soundId);
                
                // Inicializa os controles
                InitializeControls();
            }
            else
            {
                // Reprodutor não encontrado, exibir mensagem de erro
                ShowErrorMessage($"Reprodutor '{soundId}' não encontrado.");
            }
        }
    }

    /// <summary>
    /// Manipula o evento de carregamento da página
    /// </summary>
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentSoundId != null)
            {
                // Atualiza o título da página com o nome legível do som
                soundNameTextBlock.Text = ObterNomeLegivel(_currentSoundId);
                
                // Verifica se o reprodutor existe antes de prosseguir
                if (AudioManager.Instance.GetListReprodutores().TryGetValue(_currentSoundId, out var reprodutor) && reprodutor != null)
                {
                    _currentPlayer = reprodutor;
                    
                    // Evita eventos de atualização durante a inicialização
                    _isInitializing = true;
                    
                    // Configura controles de equalização
                    if (reprodutor.Equalizer?.Bands != null && reprodutor.Equalizer.Bands.Length >= 3)
                    {
                        var bands = reprodutor.Equalizer.Bands;
                        sliderBaixa.Value = bands[0].Gain;
                        sliderMedia.Value = bands[1].Gain;
                        sliderAlta.Value = bands[2].Gain;
                        
                        // Exibe os valores atuais nos elementos de texto
                        txtBaixaValor.Text = $"{bands[0].Gain:F1} dB";
                        txtMediaValor.Text = $"{bands[1].Gain:F1} dB";
                        txtAltaValor.Text = $"{bands[2].Gain:F1} dB";
                    }
                    
                    // Configura switch de ativação do equalizador
                    equalizerSwitch.IsOn = reprodutor.EqualizerEnabled;
                    
                    // Atualiza controles de efeitos com base nas configurações atuais
                    UpdateEffectControls();
                    
                    _isInitializing = false;
                }
                else
                {
                    // Se não encontrou o reprodutor, exibe mensagem de erro
                    ShowErrorMessage($"Reprodutor para o som '{_currentSoundId}' não encontrado.");
                }
            }
            else
            {
                // Se não foi passado um ID de som válido, exibe mensagem de erro
                ShowErrorMessage("Nenhum som selecionado para ajustar.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar página: {ex.Message}");
            ShowErrorMessage($"Erro ao carregar configurações: {ex.Message}");
        }
    }

    /// <summary>
    /// Inicializa os controles com os valores atuais do reprodutor
    /// </summary>
    private void InitializeControls()
    {
        try
        {
            _isInitializing = true;

            // Configura as opções de tipo de efeito no ComboBox
            if (tipoEfeitoComboBox != null)
            {
                // Verifica se o ComboBox já tem itens
                if (tipoEfeitoComboBox.Items.Count == 0)
                {
                    tipoEfeitoComboBox.Items.Add("Nenhum");
                    tipoEfeitoComboBox.Items.Add("Reverberação");
                    tipoEfeitoComboBox.Items.Add("Eco");
                    tipoEfeitoComboBox.Items.Add("Pitch");
                    tipoEfeitoComboBox.Items.Add("Flanger");
                }
                
                if (_currentPlayer != null && _currentPlayer.EffectsManager != null)
                {
                    tipoEfeitoComboBox.SelectedIndex = (int)_currentPlayer.EffectsManager.TipoEfeito;
                }
            }

            // Configura os switches
            if (_currentPlayer != null)
            {
                equalizerSwitch.IsOn = _currentPlayer.EqualizerEnabled;
                effectsSwitch.IsOn = _currentPlayer.EffectsEnabled;

                // Configura os sliders de equalização
                if (_currentPlayer.Equalizer != null && _currentPlayer.Equalizer.Bands != null && _currentPlayer.Equalizer.Bands.Length >= 3)
                {
                    sliderBaixa.Value = _currentPlayer.Equalizer.Bands[0].Gain;
                    txtBaixaValor.Text = $"{sliderBaixa.Value:F1} dB";
                    
                    sliderMedia.Value = _currentPlayer.Equalizer.Bands[1].Gain;
                    txtMediaValor.Text = $"{sliderMedia.Value:F1} dB";
                    
                    sliderAlta.Value = _currentPlayer.Equalizer.Bands[2].Gain;
                    txtAltaValor.Text = $"{sliderAlta.Value:F1} dB";
                }
            }

            // Configura os paineis baseados nos estados dos switches
            equalizerPanel.Visibility = equalizerSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
            effectsPanel.Visibility = effectsSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;

            // Configura os controles de efeitos com base no tipo selecionado
            UpdateEffectControls();
            
            _isInitializing = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao inicializar controles: {ex.Message}");
            ShowErrorMessage("Não foi possível inicializar os controles de equalização e efeitos.");
        }
    }

    /// <summary>
    /// Atualiza os controles de efeitos com base no tipo de efeito atual
    /// </summary>
    private void UpdateEffectControls()
    {
        // Oculta todos os painéis de controle de efeitos
        reverbPanel.Visibility = Visibility.Collapsed;
        echoPanel.Visibility = Visibility.Collapsed;
        pitchPanel.Visibility = Visibility.Collapsed;
        flangerPanel.Visibility = Visibility.Collapsed;

        // Verifica se o reprodutor e seu gerenciador de efeitos existem
        if (_currentPlayer == null || _currentPlayer.EffectsManager == null)
        {
            return;
        }

        // Obtém o tipo de efeito atual
        var tipoEfeito = _currentPlayer.EffectsManager.TipoEfeito;

        // Exibe e configura os controles específicos do tipo de efeito
        switch (tipoEfeito)
        {
            case TipoEfeito.Reverb:
                reverbPanel.Visibility = Visibility.Visible;
                sliderReverbMix.Value = _currentPlayer.EffectsManager.ReverbMix;
                txtReverbMixValor.Text = $"{sliderReverbMix.Value:F2}";
                
                sliderReverbTime.Value = _currentPlayer.EffectsManager.ReverbTime;
                txtReverbTimeValor.Text = $"{sliderReverbTime.Value:F1} s";
                break;

            case TipoEfeito.Echo:
                echoPanel.Visibility = Visibility.Visible;
                sliderEchoDelay.Value = _currentPlayer.EffectsManager.EchoDelay;
                txtEchoDelayValor.Text = $"{sliderEchoDelay.Value:F0} ms";
                
                sliderEchoMix.Value = _currentPlayer.EffectsManager.EchoMix;
                txtEchoMixValor.Text = $"{sliderEchoMix.Value:F2}";
                break;

            case TipoEfeito.Pitch:
                pitchPanel.Visibility = Visibility.Visible;
                sliderPitchFactor.Value = _currentPlayer.EffectsManager.PitchFactor;
                txtPitchFactorValor.Text = $"{sliderPitchFactor.Value:F2}";
                break;

            case TipoEfeito.Flanger:
                flangerPanel.Visibility = Visibility.Visible;
                sliderFlangerRate.Value = _currentPlayer.EffectsManager.FlangerRate;
                txtFlangerRateValor.Text = $"{sliderFlangerRate.Value:F1} Hz";
                
                sliderFlangerDepth.Value = _currentPlayer.EffectsManager.FlangerDepth;
                txtFlangerDepthValor.Text = $"{sliderFlangerDepth.Value:F3}";
                break;
        }
    }

    /// <summary>
    /// Atualiza a interface com os valores do equalizador
    /// </summary>
    private void AtualizarValoresEqualizador()
    {
        if (string.IsNullOrEmpty(_currentSoundId))
        {
            System.Diagnostics.Debug.WriteLine("Não há som selecionado para atualizar valores do equalizador");
            return;
        }

        try
        {
            var reprodutor = AudioManager.Instance.GetReprodutorPorId(_currentSoundId);
            if (reprodutor?.Equalizer?.Bands == null || reprodutor.Equalizer.Bands.Length < 3)
            {
                System.Diagnostics.Debug.WriteLine("Equalizador ou bandas não disponíveis");
                return;
            }

            _isInitializing = true;
            
            // Atualiza os sliders com os valores atuais das bandas do equalizador
            sliderBaixa.Value = reprodutor.Equalizer.Bands[0].Gain;
            sliderMedia.Value = reprodutor.Equalizer.Bands[1].Gain;
            sliderAlta.Value = reprodutor.Equalizer.Bands[2].Gain;
            
            // Atualiza os textos
            txtBaixaValor.Text = $"{reprodutor.Equalizer.Bands[0].Gain:F1} dB";
            txtMediaValor.Text = $"{reprodutor.Equalizer.Bands[1].Gain:F1} dB";
            txtAltaValor.Text = $"{reprodutor.Equalizer.Bands[2].Gain:F1} dB";
            
            _isInitializing = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar valores do equalizador: {ex.Message}");
        }
    }

    /// <summary>
    /// Atualiza os valores de efeitos de acordo com o reprodutor atual
    /// </summary>
    private void AtualizarValoresEfeitos()
    {
        if (string.IsNullOrEmpty(_currentSoundId))
        {
            System.Diagnostics.Debug.WriteLine("Não há som selecionado para atualizar valores dos efeitos");
            return;
        }

        try
        {
            var reprodutor = AudioManager.Instance.GetReprodutorPorId(_currentSoundId);
            if (reprodutor?.EffectsManager == null)
            {
                System.Diagnostics.Debug.WriteLine("Gerenciador de efeitos não disponível");
                return;
            }

            _isInitializing = true;
            
            // Atualiza o tipo de efeito selecionado no ComboBox
            tipoEfeitoComboBox.SelectedIndex = (int)reprodutor.EffectsManager.TipoEfeito;
            
            // Atualiza controles específicos de acordo com o tipo de efeito
            UpdateEffectControls();
            
            _isInitializing = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar valores dos efeitos: {ex.Message}");
        }
    }

    /// <summary>
    /// Manipula o evento de alteração na seleção do tipo de efeito
    /// </summary>
    private void TipoEfeito_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        var tipoEfeito = (TipoEfeito)tipoEfeitoComboBox.SelectedIndex;
        
        // Define o tipo de efeito
        AudioManager.Instance.DefinirTipoEfeito(_currentSoundId, tipoEfeito);
        
        // Atualiza os controles visíveis
        UpdateEffectControls();
    }

    /// <summary>
    /// Manipula o evento de alteração no toggle do equalizador
    /// </summary>
    private void EqualizerSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        // Ativa/desativa o equalizador
        AudioManager.Instance.AtivarEqualizador(_currentSoundId, equalizerSwitch.IsOn);
        
        // Atualiza a interface
        equalizerPanel.Visibility = equalizerSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Manipula o evento de alteração no toggle de efeitos
    /// </summary>
    private void EffectsSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        // Ativa/desativa os efeitos
        AudioManager.Instance.AtivarEfeitos(_currentSoundId, effectsSwitch.IsOn);
        
        // Atualiza a interface
        effectsPanel.Visibility = effectsSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
        
        // Também atualiza os painéis específicos para manter consistência com o estado atual
        UpdateEffectControls();
    }

    #region Handlers de Equalização

    /// <summary>
    /// Manipula o evento de alteração no slider de banda baixa do equalizador
    /// </summary>
    private void SliderBaixa_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarEqualizador(_currentSoundId, 0, (float)sliderBaixa.Value);
        txtBaixaValor.Text = $"{sliderBaixa.Value:F1} dB";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de banda média do equalizador
    /// </summary>
    private void SliderMedia_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarEqualizador(_currentSoundId, 1, (float)sliderMedia.Value);
        txtMediaValor.Text = $"{sliderMedia.Value:F1} dB";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de banda alta do equalizador
    /// </summary>
    private void SliderAlta_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarEqualizador(_currentSoundId, 2, (float)sliderAlta.Value);
        txtAltaValor.Text = $"{sliderAlta.Value:F1} dB";
    }

    #endregion

    #region Handlers de Efeitos

    /// <summary>
    /// Manipula o evento de alteração no slider de mix de reverberação
    /// </summary>
    private void SliderReverbMix_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarReverb(_currentSoundId, (float)sliderReverbMix.Value, (float)sliderReverbTime.Value);
        txtReverbMixValor.Text = $"{sliderReverbMix.Value:F2}";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de tempo de reverberação
    /// </summary>
    private void SliderReverbTime_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarReverb(_currentSoundId, (float)sliderReverbMix.Value, (float)sliderReverbTime.Value);
        txtReverbTimeValor.Text = $"{sliderReverbTime.Value:F1} s";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de atraso de eco
    /// </summary>
    private void SliderEchoDelay_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarEcho(_currentSoundId, (float)sliderEchoDelay.Value, (float)sliderEchoMix.Value);
        txtEchoDelayValor.Text = $"{sliderEchoDelay.Value:F0} ms";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de mix de eco
    /// </summary>
    private void SliderEchoMix_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarEcho(_currentSoundId, (float)sliderEchoDelay.Value, (float)sliderEchoMix.Value);
        txtEchoMixValor.Text = $"{sliderEchoMix.Value:F2}";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de fator de pitch
    /// </summary>
    private void SliderPitchFactor_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarPitch(_currentSoundId, (float)sliderPitchFactor.Value);
        txtPitchFactorValor.Text = $"{sliderPitchFactor.Value:F2}";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de taxa de flanger
    /// </summary>
    private void SliderFlangerRate_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarFlanger(_currentSoundId, (float)sliderFlangerRate.Value, (float)sliderFlangerDepth.Value);
        txtFlangerRateValor.Text = $"{sliderFlangerRate.Value:F1} Hz";
    }

    /// <summary>
    /// Manipula o evento de alteração no slider de profundidade de flanger
    /// </summary>
    private void SliderFlangerDepth_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;
        AudioManager.Instance.AjustarFlanger(_currentSoundId, (float)sliderFlangerRate.Value, (float)sliderFlangerDepth.Value);
        txtFlangerDepthValor.Text = $"{sliderFlangerDepth.Value:F3}";
    }

    #endregion

    #region Manipulação de Botões

    /// <summary>
    /// Manipula o evento de clique no botão de voltar
    /// </summary>
    private void BtnVoltar_Click(object sender, RoutedEventArgs e)
    {
        // Salva o estado atual antes de navegar de volta
        AudioManager.Instance.SalvarEstadoEfeitos();
        AudioManager.Instance.SalvarEstadoVolumes();
        
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    /// <summary>
    /// Manipula o evento de clique no botão de salvar configurações
    /// </summary>
    private void BtnSalvarConfiguracoes_Click(object sender, RoutedEventArgs e)
    {
        // Salva explicitamente as configurações de efeitos e equalização
        AudioManager.Instance.SalvarEstadoEfeitos();
        
        // Exibe feedback
        MostrarFeedbackSucesso("Configurações salvas com sucesso.");
    }

    /// <summary>
    /// Manipula o evento de clique no botão de resetar equalizador
    /// </summary>
    private void BtnResetEqualizer_Click(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        
        // Verifica se há um ID de som atual válido
        if (string.IsNullOrEmpty(_currentSoundId))
        {
            MostrarFeedbackErro("Nenhum som selecionado para redefinir equalização");
            return;
        }
        
        // Obtém o reprodutor para o som atual
        Reprodutor? reprodutor = null;
        try
        {
            reprodutor = AudioManager.Instance.GetReprodutorPorId(_currentSoundId);
        }
        catch (KeyNotFoundException)
        {
            MostrarFeedbackErro($"Reprodutor para o som '{_currentSoundId}' não encontrado");
            return;
        }

        // Só prossegue se o reprodutor for válido
        if (reprodutor != null && reprodutor.Equalizer != null)
        {
            reprodutor.Equalizer.Reset();
            
            // Atualiza a UI
            _isInitializing = true;
            sliderBaixa.Value = 0;
            sliderMedia.Value = 0;
            sliderAlta.Value = 0;
            
            txtBaixaValor.Text = "0.0 dB";
            txtMediaValor.Text = "0.0 dB";
            txtAltaValor.Text = "0.0 dB";
            _isInitializing = false;
            
            MostrarFeedbackSucesso("Equalização redefinida com sucesso");
        }
        else
        {
            MostrarFeedbackErro("Não foi possível acessar o equalizador");
        }
    }

    /// <summary>
    /// Manipula o evento de clique no botão de resetar efeitos
    /// </summary>
    private void BtnResetEffects_Click(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        // Verifica se _currentPlayer e _currentPlayer.EffectsManager não são nulos
        if (_currentPlayer == null || _currentPlayer.EffectsManager == null) 
        {
            ShowErrorMessage("Reprodutor não disponível para resetar efeitos.");
            return;
        }

        var tipoEfeito = _currentPlayer.EffectsManager.TipoEfeito;
        
        // Reseta os efeitos para os valores padrão manualmente baseado no tipo atual
        switch (tipoEfeito)
        {
            case TipoEfeito.Reverb:
                AudioManager.Instance.AjustarReverb(_currentSoundId, 0.3f, 1.0f);
                break;
            case TipoEfeito.Echo:
                AudioManager.Instance.AjustarEcho(_currentSoundId, 250f, 0.5f);
                break;
            case TipoEfeito.Pitch:
                AudioManager.Instance.AjustarPitch(_currentSoundId, 1.0f);
                break;
            case TipoEfeito.Flanger:
                AudioManager.Instance.AjustarFlanger(_currentSoundId, 0.5f, 0.005f);
                break;
        }

        // Atualiza os controles
        UpdateEffectControls();
        
        // Exibe feedback
        MostrarFeedbackSucesso("Efeitos resetados para os valores padrão.");
    }

    #endregion

    #region Utilitários

    /// <summary>
    /// Exibe uma mensagem de erro
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        infoBar.Title = "Erro";
        infoBar.Message = message;
        infoBar.Severity = InfoBarSeverity.Error;
        infoBar.IsOpen = true;
    }

    /// <summary>
    /// Exibe uma mensagem de erro com estilo apropriado
    /// </summary>
    private void MostrarFeedbackErro(string mensagem)
    {
        infoBar.Title = "Erro";
        infoBar.Message = mensagem;
        infoBar.Severity = InfoBarSeverity.Error;
        infoBar.IsOpen = true;

        // Configura um temporizador para fechar a infoBar após alguns segundos
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (s, args) =>
        {
            infoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }

    /// <summary>
    /// Exibe uma mensagem de sucesso temporária
    /// </summary>
    private void MostrarFeedbackSucesso(string mensagem)
    {
        infoBar.Title = "Sucesso";
        infoBar.Message = mensagem;
        infoBar.Severity = InfoBarSeverity.Success;
        infoBar.IsOpen = true;

        // Configura um temporizador para fechar a infoBar após alguns segundos
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, args) =>
        {
            infoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }

    /// <summary>
    /// Converte a chave técnica do som para um nome mais amigável para exibição
    /// </summary>
    private string ObterNomeLegivel(string chaveSom)
    {
        // Mapear chaves técnicas para nomes amigáveis
        switch (chaveSom)
        {
            case "chuva": return "Chuva";
            case "fogueira": return "Fogueira";
            case "ondas": return "Ondas do Mar";
            case "passaros": return "Pássaros";
            case "praia": return "Praia";
            case "trem": return "Trem";
            case "ventos": return "Ventos";
            case "cafeteria": return "Cafeteria";
            case "lancha": return "Lancha";
            default: return chaveSom;
        }
    }

    #endregion

    /// <summary>
    /// Manipula o evento de clique no mostrador de valor do equalizador 
    /// para redefinir para o valor padrão
    /// </summary>
    private void TxtEqualizerValue_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_isInitializing) return;
        
        if (sender is TextBlock txtBlock)
        {
            int bandIndex = -1;
            float defaultValue = 0f;
            Slider? targetSlider = null;

            // Identifica qual slider deve ser ajustado com base no textblock clicado
            if (txtBlock == txtBaixaValor)
            {
                bandIndex = 0;
                targetSlider = sliderBaixa;
            }
            else if (txtBlock == txtMediaValor)
            {
                bandIndex = 1;
                targetSlider = sliderMedia;
            }
            else if (txtBlock == txtAltaValor)
            {
                bandIndex = 2;
                targetSlider = sliderAlta;
            }

            // Se identificou o slider e ele não é nulo, redefine para o valor padrão
            if (bandIndex >= 0 && targetSlider != null)
            {
                targetSlider.Value = defaultValue;
                AudioManager.Instance.AjustarEqualizador(_currentSoundId, bandIndex, defaultValue);
                MostrarFeedbackSucesso($"Banda de equalização redefinida para {defaultValue:F1} dB");
            }
        }
    }

    /// <summary>
    /// Manipula o evento de seleção alterada na lista de sons
    /// </summary>
    private void SoundsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is Reprodutor selectedReprod)
        {
            // Procura o ID do reprodutor selecionado
            string? id = null;
            foreach (var pair in AudioManager.Instance.GetListReprodutores())
            {
                if (pair.Value == selectedReprod)
                {
                    id = pair.Key;
                    break;
                }
            }
            
            if (!string.IsNullOrEmpty(id))
            {
                _currentSoundId = id;
                soundNameTextBlock.Text = ObterNomeLegivel(id);
                
                // Atualiza a interface com os valores atuais
                AtualizarValoresEqualizador();
                AtualizarValoresEfeitos();
                
                // Atualiza os toggles de ativação
                try {
                    var reprodutor = AudioManager.Instance.GetReprodutorPorId(id);
                    if (reprodutor != null)
                    {
                        equalizerSwitch.IsOn = reprodutor.EqualizerEnabled;
                        effectsSwitch.IsOn = reprodutor.EffectsEnabled;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao atualizar toggles: {ex.Message}");
                }
            }
        }
    }
}