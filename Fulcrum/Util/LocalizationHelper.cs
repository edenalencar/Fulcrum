using System.Diagnostics;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Fulcrum.Util;

/// <summary>
/// Classe auxiliar para lidar com localização e acesso a recursos de strings
/// </summary>
public static class LocalizationHelper
{
    private static readonly Dictionary<string, string> _resources = new Dictionary<string, string>();
    private static bool _initialized = false;
    private static string _currentLanguage = "en-US"; // Padrão: português

    /// <summary>
    /// Inicializa o helper carregando os recursos do arquivo .resw
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        try
        {
            // Determinar a linguagem atual do sistema
            var currentLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

            if (!string.IsNullOrEmpty(currentLanguage))
            {
                if (currentLanguage.StartsWith("en"))
                    _currentLanguage = "en-US";
                else if (currentLanguage.StartsWith("pt"))
                    _currentLanguage = "pt-BR";
            }

            Debug.WriteLine($"Idioma detectado: {_currentLanguage}");
            LoadResources();
            _initialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro na inicialização do LocalizationHelper: {ex.Message}");
        }
    }

    /// <summary>
    /// Carrega os recursos do arquivo .resw para a memória
    /// </summary>
    private static async void LoadResources()
    {
        try
        {
            var installFolder = Package.Current.InstalledLocation;
            var resxFolder = await installFolder.GetFolderAsync("Strings");
            var langFolder = await resxFolder.GetFolderAsync(_currentLanguage);
            var resourceFile = await langFolder.GetFileAsync("Resources.resw");

            var fileContent = await FileIO.ReadTextAsync(resourceFile);

            using (var stringReader = new StringReader(fileContent))
            using (var xmlReader = XmlReader.Create(stringReader))
            {
                while (xmlReader.Read())
                {                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "data")
                    {
                        string? key = xmlReader.GetAttribute("name");
                        xmlReader.Read(); // Move para o próximo nó

                        while (xmlReader.NodeType != XmlNodeType.Element && xmlReader.Read())
                        {
                            // Avança até encontrar o elemento value
                        }

                        if (xmlReader.Name == "value")
                        {
                            string value = xmlReader.ReadElementContentAsString();
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                            {
                                _resources[key] = value;
                                Debug.WriteLine($"Recurso carregado: {key} = {value}");
                            }
                        }
                    }
                }
            }

            Debug.WriteLine($"Recursos carregados com sucesso: {_resources.Count} itens");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar recursos: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"Detalhe: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Obtém uma string localizada com tratamento de erro
    /// </summary>
    /// <param name="resourceKey">A chave do recurso localizado</param>
    /// <param name="defaultValue">Valor padrão a ser retornado em caso de erro</param>
    /// <returns>A string localizada ou o valor padrão em caso de falha</returns>
    public static string GetString(string resourceKey, string defaultValue = "")
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (string.IsNullOrEmpty(resourceKey))
            return defaultValue;        try
        {
            // Tenta obter do dicionário em memória primeiro
            if (_resources.TryGetValue(resourceKey, out string? value) && value != null)
            {
                return value;
            }

            // Fallback para o método padrão
            try
            {
                var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
                return resourceLoader.GetString(resourceKey) ?? defaultValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fallback falhou para recurso '{resourceKey}': {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao obter recurso '{resourceKey}': {ex.Message}");
        }

        return defaultValue;
    }

    /// <summary>
    /// Obtém uma string formatada usando um padrão localizado
    /// </summary>
    /// <param name="resourceKey">A chave do recurso localizado contendo o padrão de formato</param>
    /// <param name="defaultFormat">Formato padrão a ser usado em caso de erro</param>
    /// <param name="args">Argumentos para formatação</param>
    /// <returns>A string formatada ou uma string formatada com o padrão padrão em caso de falha</returns>
    public static string GetFormattedString(string resourceKey, string defaultFormat, params object[] args)
    {
        var format = GetString(resourceKey, defaultFormat);
        try
        {
            return string.Format(format, args);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao formatar string '{resourceKey}': {ex.Message}");
            try
            {
                return string.Format(defaultFormat, args);
            }
            catch
            {
                return defaultFormat;
            }
        }
    }

    /// <summary>
    /// Define o idioma atual e recarrega os recursos, se necessário
    /// </summary>
    /// <param name="language">O código do idioma (ex: "pt-BR", "en-US")</param>
    /// <returns>True se o idioma foi alterado com sucesso</returns>
    public static bool SetLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            return false;

        // Só faz algo se o idioma for diferente do atual
        if (language != _currentLanguage)
        {
            // Verifica se o idioma é suportado
            if (language.StartsWith("en"))
                language = "en-US";
            else if (language.StartsWith("pt"))
                language = "pt-BR";
            else
                return false; // Idioma não suportado

            Debug.WriteLine($"Alterando idioma de {_currentLanguage} para {language}");

            // Atualiza o idioma e limpa o cache
            _currentLanguage = language;
            _resources.Clear();
            _initialized = false;

            // Recarrega os recursos
            Initialize();
            return true;
        }

        return false; // Nada foi alterado
    }
}