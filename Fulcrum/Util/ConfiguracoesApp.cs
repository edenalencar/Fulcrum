using Fulcrum.Bu;
using System.Collections.Concurrent;
using System.Text.Json;
using Windows.Storage;

namespace Fulcrum.Util;

/// <summary>
/// Classe responsável por gerenciar as configurações do aplicativo
/// </summary>
public static class ConfiguracoesApp
{
    // Armazenamento local para as configurações
    private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    /// <summary>
    /// Chave para armazenar as configurações de equalizador
    /// </summary>
    private const string EQ_SETTINGS_KEY = "EqualizerSettings";

    /// <summary>
    /// Chave para armazenar as configurações de efeitos
    /// </summary>
    private const string FX_SETTINGS_KEY = "EffectsSettings";

    /// <summary>
    /// Salva as configurações de equalizador no armazenamento local
    /// </summary>
    /// <param name="equalizerSettings">Dicionário com as configurações de equalizador</param>
    public static void SalvarConfiguracoesEqualizer(ConcurrentDictionary<string, float[]> equalizerSettings)
    {
        try
        {
            // Converte o dicionário para um formato serializável
            var serializableDict = new Dictionary<string, float[]>();
            foreach (var entry in equalizerSettings)
            {
                serializableDict[entry.Key] = entry.Value;
            }

            // Serializa e salva nas configurações locais
            string json = JsonSerializer.Serialize(serializableDict);
            _localSettings.Values[EQ_SETTINGS_KEY] = json;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações do equalizador: {ex.Message}");
        }
    }

    /// <summary>
    /// Salva as configurações de efeitos no armazenamento local
    /// </summary>
    /// <param name="effectSettings">Dicionário com as configurações de efeitos</param>
    public static void SalvarConfiguracoesEfeitos(ConcurrentDictionary<string, EffectSettings> effectSettings)
    {
        try
        {
            // Converte o dicionário para um formato serializável
            var serializableDict = new Dictionary<string, EffectSettings>();
            foreach (var entry in effectSettings)
            {
                serializableDict[entry.Key] = entry.Value;
            }

            // Serializa e salva nas configurações locais
            string json = JsonSerializer.Serialize(serializableDict);
            _localSettings.Values[FX_SETTINGS_KEY] = json;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações de efeitos: {ex.Message}");
        }
    }

    /// <summary>
    /// Carrega as configurações de equalizador do armazenamento local
    /// </summary>
    /// <returns>Dicionário com as configurações de equalizador</returns>
    public static Dictionary<string, float[]> CarregarConfiguracoesEqualizer()
    {
        try
        {
            if (_localSettings.Values.TryGetValue(EQ_SETTINGS_KEY, out var savedJson) && savedJson is string json)
            {
                return JsonSerializer.Deserialize<Dictionary<string, float[]>>(json) ?? new Dictionary<string, float[]>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações do equalizador: {ex.Message}");
        }

        return new Dictionary<string, float[]>();
    }

    /// <summary>
    /// Carrega as configurações de efeitos do armazenamento local
    /// </summary>
    /// <returns>Dicionário com as configurações de efeitos</returns>
    public static Dictionary<string, EffectSettings> CarregarConfiguracoesEfeitos()
    {
        try
        {
            if (_localSettings.Values.TryGetValue(FX_SETTINGS_KEY, out var savedJson) && savedJson is string json)
            {
                return JsonSerializer.Deserialize<Dictionary<string, EffectSettings>>(json) ?? new Dictionary<string, EffectSettings>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações de efeitos: {ex.Message}");
        }

        return new Dictionary<string, EffectSettings>();
    }
}