using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fulcrum.Bu;

/// <summary>
/// Representa um perfil personalizado de configurações de som
/// </summary>
public class PerfilSom
{
    /// <summary>
    /// Nome do perfil
    /// </summary>
    public string Nome { get; set; }
    
    /// <summary>
    /// Descrição do perfil
    /// </summary>
    public string Descricao { get; set; }
    
    /// <summary>
    /// Data de criação do perfil
    /// </summary>
    public DateTime DataCriacao { get; set; }
    
    /// <summary>
    /// Configurações de som para este perfil (ID do som -> volume)
    /// </summary>
    public Dictionary<string, float> ConfiguracoesSom { get; set; }
    
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