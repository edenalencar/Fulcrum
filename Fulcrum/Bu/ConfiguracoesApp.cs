using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Fulcrum.Bu;

/// <summary>
/// Classe responsável por gerenciar as configurações persistentes da aplicação
/// </summary>
public static class ConfiguracoesApp
{
    // Instância do armazenamento local
    private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
    
    // Chaves para armazenamento das configurações
    private const string EqualizerSettingsKey = "EqualizerSettings";
    private const string EffectSettingsKey = "EffectSettings";
    
    /// <summary>
    /// Salva as configurações de equalização no armazenamento local
    /// </summary>
    /// <param name="settings">Dicionário com configurações de equalização</param>
    public static void SalvarConfiguracoesEqualizer(IDictionary<string, float[]> settings)
    {
        try
        {
            string jsonData = JsonSerializer.Serialize(settings);
            _localSettings.Values[EqualizerSettingsKey] = jsonData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações de equalização: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Salva as configurações de efeitos no armazenamento local
    /// </summary>
    /// <param name="settings">Dicionário com configurações de efeitos</param>
    public static void SalvarConfiguracoesEfeitos(IDictionary<string, EffectSettings> settings)
    {
        try
        {
            string jsonData = JsonSerializer.Serialize(settings);
            _localSettings.Values[EffectSettingsKey] = jsonData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações de efeitos: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Carrega as configurações de equalização do armazenamento local
    /// </summary>
    /// <returns>Dicionário com configurações de equalização ou null se não existir</returns>
    public static Dictionary<string, float[]> CarregarConfiguracoesEqualizer()
    {
        try
        {
            if (_localSettings.Values.TryGetValue(EqualizerSettingsKey, out var jsonData) && jsonData is string jsonString)
            {
                return JsonSerializer.Deserialize<Dictionary<string, float[]>>(jsonString);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações de equalização: {ex.Message}");
        }
        
        return new Dictionary<string, float[]>();
    }
    
    /// <summary>
    /// Carrega as configurações de efeitos do armazenamento local
    /// </summary>
    /// <returns>Dicionário com configurações de efeitos ou null se não existir</returns>
    public static Dictionary<string, EffectSettings> CarregarConfiguracoesEfeitos()
    {
        try
        {
            if (_localSettings.Values.TryGetValue(EffectSettingsKey, out var jsonData) && jsonData is string jsonString)
            {
                return JsonSerializer.Deserialize<Dictionary<string, EffectSettings>>(jsonString);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações de efeitos: {ex.Message}");
        }
        
        return new Dictionary<string, EffectSettings>();
    }
}