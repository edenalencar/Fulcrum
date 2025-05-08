using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Fulcrum.Util;
using Windows.Storage;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Fulcrum.Bu;

/// <summary>
/// Gerenciador de perfis de som que permite salvar, carregar e administrar perfis personalizados
/// </summary>
public class GerenciadorPerfis
{
    private static readonly Lazy<GerenciadorPerfis> _instance = new(() => new GerenciadorPerfis());
    
    private readonly List<PerfilSom> _perfis = new();
    private readonly string _perfilDiretorio;
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
    
    // Nome do perfil ativo
    private string _perfilAtivoNome = string.Empty;
    
    // Construtor privado para implementar o padrão Singleton
    private GerenciadorPerfis()
    {
        // Cria o diretório para armazenar os perfis
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _perfilDiretorio = Path.Combine(appDataPath, "Fulcrum", "Perfis");
        
        // Garante que o diretório exista
        if (!Directory.Exists(_perfilDiretorio))
        {
            Directory.CreateDirectory(_perfilDiretorio);
        }
        
        // Carrega o nome do perfil ativo das configurações
        if (_localSettings.Values.TryGetValue(Constantes.Perfis.PerfilAtivo, out var perfilAtivo) && perfilAtivo is string perfilAtivoNome)
        {
            _perfilAtivoNome = perfilAtivoNome;
        }
    }
    
    /// <summary>
    /// Instância única do GerenciadorPerfis (Singleton)
    /// </summary>
    public static GerenciadorPerfis Instance => _instance.Value;
    
    /// <summary>
    /// Lista de perfis de som disponíveis
    /// </summary>
    public IReadOnlyList<PerfilSom> Perfis => _perfis;
    
    /// <summary>
    /// Nome do perfil ativo atualmente
    /// </summary>
    public string PerfilAtivoNome => _perfilAtivoNome;
    
    /// <summary>
    /// Obtém o perfil ativo atual ou null se nenhum perfil estiver ativo
    /// </summary>
    public PerfilSom? PerfilAtivo
    {
        get
        {
            if (string.IsNullOrEmpty(_perfilAtivoNome))
                return null;
            
            // Encontra o perfil pelo nome
            foreach (var perfil in _perfis)
            {
                if (perfil.Nome == _perfilAtivoNome)
                    return perfil;
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Define o perfil ativo atual pelo nome
    /// </summary>
    /// <param name="nomePerfil">Nome do perfil a ser definido como ativo</param>
    public void DefinirPerfilAtivo(string nomePerfil)
    {
        _perfilAtivoNome = nomePerfil;
        _localSettings.Values[Constantes.Perfis.PerfilAtivo] = nomePerfil;
    }
    
    /// <summary>
    /// Define o perfil ativo atual
    /// </summary>
    /// <param name="perfil">Perfil a ser definido como ativo</param>
    public void DefinirPerfilAtivo(PerfilSom? perfil)
    {
        if (perfil != null)
        {
            DefinirPerfilAtivo(perfil.Nome);
        }
        else
        {
            // Remove o perfil ativo se null for passado
            _perfilAtivoNome = string.Empty;
            _localSettings.Values[Constantes.Perfis.PerfilAtivo] = string.Empty;
        }
    }
    
    /// <summary>
    /// Verifica se um perfil é o perfil ativo atualmente
    /// </summary>
    /// <param name="perfil">Perfil a ser verificado</param>
    /// <returns>True se o perfil for o ativo atualmente</returns>
    public bool EhPerfilAtivo(PerfilSom perfil)
    {
        if (perfil == null || string.IsNullOrEmpty(_perfilAtivoNome))
            return false;
        
        return perfil.Nome == _perfilAtivoNome;
    }
    
    /// <summary>
    /// Carrega todos os perfis de som disponíveis do armazenamento
    /// </summary>
    public async Task CarregarPerfisAsync()
    {
        _perfis.Clear();
        
        try
        {
            foreach (var arquivo in Directory.GetFiles(_perfilDiretorio, "*.json"))
            {
                try
                {
                    using var stream = new FileStream(arquivo, FileMode.Open, FileAccess.Read);
                    var perfil = await JsonSerializer.DeserializeAsync<PerfilSom>(stream);
                    
                    if (perfil != null)
                    {
                        _perfis.Add(perfil);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao carregar perfil {arquivo}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar perfis: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Salva um perfil de som no armazenamento
    /// </summary>
    /// <param name="perfil">Perfil de som a ser salvo</param>
    public async Task SalvarPerfilAsync(PerfilSom perfil)
    {
        try
        {
            // Adiciona o perfil à lista se ainda não estiver presente
            if (!_perfis.Contains(perfil))
            {
                _perfis.Add(perfil);
            }
            
            // Salva o perfil em um arquivo JSON
            var nomeArquivo = SanitizarNomeArquivo(perfil.Nome) + ".json";
            var caminhoArquivo = Path.Combine(_perfilDiretorio, nomeArquivo);
            
            using var stream = new FileStream(caminhoArquivo, FileMode.Create, FileAccess.Write);
            var options = new JsonSerializerOptions { WriteIndented = true };
            await JsonSerializer.SerializeAsync(stream, perfil, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar perfil {perfil.Nome}: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Remove um perfil de som
    /// </summary>
    /// <param name="perfil">Perfil de som a ser removido</param>
    public void RemoverPerfil(PerfilSom perfil)
    {
        try
        {
            if (_perfis.Contains(perfil))
            {
                _perfis.Remove(perfil);
                
                // Remove o arquivo do perfil
                var nomeArquivo = SanitizarNomeArquivo(perfil.Nome) + ".json";
                var caminhoArquivo = Path.Combine(_perfilDiretorio, nomeArquivo);
                
                if (File.Exists(caminhoArquivo))
                {
                    File.Delete(caminhoArquivo);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao remover perfil {perfil.Nome}: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Cria um novo perfil baseado nas configurações atuais do AudioManager
    /// </summary>
    /// <param name="nome">Nome do perfil</param>
    /// <param name="descricao">Descrição do perfil</param>
    /// <returns>Novo perfil de som</returns>
    public PerfilSom CriarPerfilDaConfigAtual(string nome, string descricao)
    {
        var perfil = new PerfilSom(nome, descricao);
        var audioManager = AudioManager.Instance;
        
        // Captura as configurações atuais de todos os reprodutores
        foreach (var reprodutor in audioManager.GetListReprodutores())
        {
            perfil.DefinirVolumeSom(reprodutor.Key, reprodutor.Value.Reader.Volume);
        }
        
        return perfil;
    }
    
    /// <summary>
    /// Cria um perfil com todas as configurações padrão
    /// </summary>
    /// <param name="nome">Nome opcional do perfil (usa valor localizado se não especificado)</param>
    /// <returns>Perfil de som com valores padrão</returns>
    public PerfilSom CriarPerfilPadrao(string? nome = null)
    {
        // Se o nome não for fornecido, usa o valor do recurso localizado
        if (string.IsNullOrEmpty(nome))
        {
            nome = LocalizationHelper.GetString("DefaultProfileName", "Perfil Padrão");
        }
        
        // Obtém a descrição localizada
        string descricao = LocalizationHelper.GetString("DefaultProfileDescription", "Configurações padrão para todos os sons");
        
        var perfil = new PerfilSom(nome, descricao);
        
        // Configura valores iniciais para todos os sons conhecidos
        perfil.DefinirVolumeSom(Constantes.Sons.Chuva, 0.2f);
        perfil.DefinirVolumeSom(Constantes.Sons.Fogueira, 0.3f);
        perfil.DefinirVolumeSom(Constantes.Sons.Lancha, 0.1f);
        perfil.DefinirVolumeSom(Constantes.Sons.Ondas, 0.4f);
        perfil.DefinirVolumeSom(Constantes.Sons.Passaros, 0.3f);
        perfil.DefinirVolumeSom(Constantes.Sons.Praia, 0.4f);
        perfil.DefinirVolumeSom(Constantes.Sons.Trem, 0.2f);
        perfil.DefinirVolumeSom(Constantes.Sons.Ventos, 0.2f);
        perfil.DefinirVolumeSom(Constantes.Sons.Cafeteria, 0.25f);
        
        // Define configurações de efeitos padrão
        ConfigurarEfeitosPadrao(perfil);
        
        // Adiciona o perfil à lista interna
        _perfis.Add(perfil);
        
        return perfil;
    }
    
    /// <summary>
    /// Configura efeitos padrão para um perfil de som
    /// </summary>
    /// <param name="perfil">Perfil a receber as configurações padrão de efeitos</param>
    private void ConfigurarEfeitosPadrao(PerfilSom perfil)
    {
        // Esta é uma implementação básica que pode ser expandida no futuro
        // para incluir configurações específicas de efeitos para cada som
        
        // Atualmente, apenas garantimos que o perfil existe e está registrado
        // As configurações específicas de efeitos serão gerenciadas pelo AudioManager
        if (perfil == null)
        {
            System.Diagnostics.Debug.WriteLine("Erro: Tentativa de configurar efeitos para um perfil nulo");
            return;
        }
        
        // Pode ser expandido para incluir configurações iniciais específicas
        // como valores de equalização padrão, configurações de reverb, etc.
        System.Diagnostics.Debug.WriteLine($"Configurações de efeitos padrão aplicadas ao perfil: {perfil.Nome}");
    }
    
    /// <summary>
    /// Sanitiza um nome para que seja válido como nome de arquivo
    /// </summary>
    /// <param name="nome">Nome original</param>
    /// <returns>Nome sanitizado</returns>
    private string SanitizarNomeArquivo(string nome)
    {
        // Remove caracteres inválidos em nomes de arquivo
        var caracteresInvalidos = Path.GetInvalidFileNameChars();
        return string.Join("_", nome.Split(caracteresInvalidos));
    }
}