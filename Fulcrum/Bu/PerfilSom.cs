using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Fulcrum.Bu;

/// <summary>
/// Representa um perfil personalizado de configurações de som
/// </summary>
public class PerfilSom : INotifyPropertyChanged
{
    private string _nome = string.Empty;
    private string _descricao = string.Empty;
    private DateTime _dataCriacao = DateTime.Now;
    private Dictionary<string, float> _configuracoesSom = new Dictionary<string, float>();

    /// <summary>
    /// Evento que é disparado quando uma propriedade é alterada
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Nome do perfil
    /// </summary>
    public string Nome
    {
        get => _nome;
        set => SetProperty(ref _nome, value);
    }
    
    /// <summary>
    /// Descrição do perfil
    /// </summary>
    public string Descricao
    {
        get => _descricao;
        set => SetProperty(ref _descricao, value);
    }
    
    /// <summary>
    /// Data de criação do perfil
    /// </summary>
    public DateTime DataCriacao
    {
        get => _dataCriacao;
        set => SetProperty(ref _dataCriacao, value);
    }
    
    /// <summary>
    /// Configurações de som para este perfil (ID do som -> volume)
    /// </summary>
    public Dictionary<string, float> ConfiguracoesSom
    {
        get => _configuracoesSom;
        set => SetProperty(ref _configuracoesSom, value);
    }
    
    /// <summary>
    /// Cria uma nova instância de PerfilSom
    /// </summary>
    [JsonConstructor]
    public PerfilSom()
    {
        Nome = string.Empty;
        Descricao = string.Empty;
        DataCriacao = DateTime.Now;
        ConfiguracoesSom = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Cria uma nova instância de PerfilSom com nome e descrição
    /// </summary>
    /// <param name="nome">Nome do perfil</param>
    /// <param name="descricao">Descrição do perfil</param>
    public PerfilSom(string nome, string descricao)
    {
        Nome = nome;
        Descricao = descricao;
        DataCriacao = DateTime.Now;
        ConfiguracoesSom = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Define o volume para um som específico
    /// </summary>
    /// <param name="idSom">ID do som</param>
    /// <param name="volume">Volume (0.0 a 1.0)</param>
    public void DefinirVolumeSom(string idSom, float volume)
    {
        ConfiguracoesSom[idSom] = volume;
        OnPropertyChanged(nameof(ConfiguracoesSom));
    }
    
    /// <summary>
    /// Obtém o volume configurado para um som específico
    /// </summary>
    /// <param name="idSom">ID do som</param>
    /// <returns>Volume configurado, ou 0 se o som não estiver configurado</returns>
    public float ObterVolumeSom(string idSom)
    {
        return ConfiguracoesSom.TryGetValue(idSom, out var volume) ? volume : 0.0f;
    }
    
    /// <summary>
    /// Método auxiliar para definir uma propriedade e notificar a mudança
    /// </summary>
    /// <typeparam name="T">Tipo da propriedade</typeparam>
    /// <param name="field">Campo de referência</param>
    /// <param name="value">Novo valor</param>
    /// <param name="propertyName">Nome da propriedade (obtido automaticamente)</param>
    /// <returns>True se o valor foi alterado, false caso contrário</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    /// <summary>
    /// Método para disparar o evento PropertyChanged
    /// </summary>
    /// <param name="propertyName">Nome da propriedade que mudou</param>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    /// <summary>
    /// Aplica este perfil de som, definindo os volumes configurados para cada reprodutor
    /// </summary>
    public void Aplicar()
    {
        var audioManager = AudioManager.Instance;
        
        // Aplica as configurações de volume para cada som no perfil
        foreach (var configuracao in ConfiguracoesSom)
        {
            try
            {
                // Obtém o reprodutor pelo ID e define o volume
                var reprodutor = audioManager.GetReprodutorPorId(configuracao.Key);
                reprodutor.AlterarVolume(configuracao.Value);
            }
            catch (KeyNotFoundException)
            {
                // Ignora sons que não existem (podem ter sido removidos ou renomeados)
                System.Diagnostics.Debug.WriteLine($"Reprodutor não encontrado para o som: {configuracao.Key}");
            }
            catch (Exception ex)
            {
                // Log de outros erros
                System.Diagnostics.Debug.WriteLine($"Erro ao aplicar configuração para {configuracao.Key}: {ex.Message}");
            }
        }
    }
}