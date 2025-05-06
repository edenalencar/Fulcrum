using Fulcrum.Bu;
using Fulcrum.Util;
using Fulcrum.Bu.Services;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Microsoft.Windows.AppLifecycle;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Fulcrum;

/// <summary>
/// Classe principal do aplicativo que gerencia o ciclo de vida
/// </summary>
public partial class App : Application
{
    // Propriedade que expõe a janela principal, permitindo acesso externo
    public Window? Window { get; private set; }
    
    // Métodos nativos para diagnóstico de carregamento de DLLs
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);
    
    [DllImport("kernel32.dll")]
    private static extern uint GetLastError();

    /// <summary>
    /// Inicializa uma nova instância da classe App
    /// </summary>
    public App()
    {
        try
        {
            // Verificar e criar arquivo de log
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "fulcrum_startup.log");
            
            File.WriteAllText(logPath, 
                $"Iniciando Fulcrum: {DateTime.Now}\n" +
                $"Versão .NET: {Environment.Version}\n" +
                $"OS: {Environment.OSVersion}\n\n");
                
            // Testar carregamento de DLLs críticas
            string[] dllsParaTestar = new[]
            {
                "NAudio.dll",
                "NAudio.WinMM.dll",
                "Microsoft.UI.Xaml.dll",
                "Microsoft.WindowsAppRuntime.Bootstrap.dll",
                "Microsoft.WinUI.dll"
            };
            
            foreach (var dll in dllsParaTestar)
            {
                IntPtr handle = LoadLibrary(dll);
                if (handle == IntPtr.Zero)
                {
                    uint error = GetLastError();
                    File.AppendAllText(logPath, 
                        $"ERRO ao carregar {dll}: Código {error}\n");
                }
                else
                {
                    File.AppendAllText(logPath, 
                        $"Carregamento bem-sucedido: {dll}\n");
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                    "fulcrum_error.log"),
                    $"Erro na inicialização: {ex.Message}\n{ex.StackTrace}");
            }
            catch
            {
                // Se não puder criar o log, continuamos mesmo assim
            }
        }
        
        // Inicializa o componente após o diagnóstico
        this.InitializeComponent();
        this.UnhandledException += App_UnhandledException;
    }

    /// <summary>
    /// Manipulador para exceções não tratadas no aplicativo
    /// </summary>
    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        
        try
        {
            // Log da exceção
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "fulcrum_crash.log");
                
            File.WriteAllText(logPath, 
                $"Exceção não tratada: {DateTime.Now}\n" +
                $"Mensagem: {e.Exception.Message}\n" +
                $"Tipo: {e.Exception.GetType().FullName}\n" +
                $"StackTrace: {e.Exception.StackTrace}\n");
                
            if (e.Exception.InnerException != null)
            {
                File.AppendAllText(logPath,
                    $"\nInnerException: {e.Exception.InnerException.Message}\n" +
                    $"InnerException Stack: {e.Exception.InnerException.StackTrace}\n");
            }
        }
        catch
        {
            // Se falhar o log, continuamos mesmo assim
        }
    }

    /// <summary>
    /// Aplica as configurações salvas de tamanho de fonte
    /// </summary>
    private void AplicarConfiguracoesFonte()
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var tamanhoFonte = localSettings.Values[Constantes.Config.TamanhoFonte] as string ?? "Médio";
            
            // Determina o fator de escala com base no tamanho salvo
            double fatorEscala = tamanhoFonte switch
            {
                "Pequeno" => 0.85,
                "Grande" => 1.2,
                "Extra Grande" => 1.5,
                _ => 1.0 // Médio (padrão)
            };
            
            // Aplica o tamanho de fonte aos recursos globais
            if (Window?.Content is FrameworkElement rootElement)
            {
                var resources = rootElement.Resources;
                
                // Verifica se as chaves já existem, caso não, cria-as com valores padrão
                if (!resources.ContainsKey("TextControlThemeFontSize"))
                    resources.Add("TextControlThemeFontSize", 14.0);
                if (!resources.ContainsKey("BodyTextBlockFontSize"))
                    resources.Add("BodyTextBlockFontSize", 14.0);
                if (!resources.ContainsKey("SubtitleTextBlockFontSize"))
                    resources.Add("SubtitleTextBlockFontSize", 18.0);
                if (!resources.ContainsKey("TitleTextBlockFontSize"))
                    resources.Add("TitleTextBlockFontSize", 24.0);
                if (!resources.ContainsKey("TitleLargeTextBlockFontSize"))
                    resources.Add("TitleLargeTextBlockFontSize", 28.0);
                if (!resources.ContainsKey("HeaderTextBlockFontSize"))
                    resources.Add("HeaderTextBlockFontSize", 46.0);
                
                // Agora atualiza os tamanhos com o fator de escala
                resources["TextControlThemeFontSize"] = 14 * fatorEscala;
                resources["BodyTextBlockFontSize"] = 14 * fatorEscala;
                resources["SubtitleTextBlockFontSize"] = 18 * fatorEscala;
                resources["TitleTextBlockFontSize"] = 24 * fatorEscala;
                resources["TitleLargeTextBlockFontSize"] = 28 * fatorEscala;
                resources["HeaderTextBlockFontSize"] = 46 * fatorEscala;
                
                // Força uma atualização nos estilos existentes
                rootElement.RequestedTheme = rootElement.RequestedTheme;
                
                Debug.WriteLine($"Tamanho de fonte '{tamanhoFonte}' aplicado com fator de escala {fatorEscala}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao aplicar configurações de fonte: {ex.Message}");
        }
    }

    /// <summary>
    /// Configura o idioma do aplicativo com base nas preferências do sistema ou configurações do usuário
    /// </summary>
    private void ConfigureLanguage()
    {
        try
        {
            // Obtém as preferências de idioma do usuário
            var userLanguage = Windows.Globalization.ApplicationLanguages.Languages[0];
            
            // Define o idioma do aplicativo
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = userLanguage;
            
            // Configura o idioma para recursos
            var resourceContext = new Windows.ApplicationModel.Resources.Core.ResourceContext();
            resourceContext.Languages = new List<string> { userLanguage };
            
            System.Diagnostics.Debug.WriteLine($"[APP] Idioma configurado: {userLanguage}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[APP] Erro ao configurar idioma: {ex.Message}");
        }
    }

    /// <summary>
    /// Chamado quando o aplicativo é iniciado normalmente pelo usuário
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Inicializa os recursos com a cultura correta
        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
        
        try
        {
            // Configura o idioma do aplicativo
            ConfigureLanguage();

            Window = new MainWindow();
            
            // Inicializa o serviço de notificações
            NotificationService.Instance.Initialize(Window.DispatcherQueue);
            
            // Aplica configurações de fonte ao iniciar
            AplicarConfiguracoesFonte();
            
            Window.Activate();
        }
        catch (Exception ex)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "fulcrum_launch_error.log");
                    
                File.WriteAllText(logPath, 
                    $"Erro ao lançar o aplicativo: {DateTime.Now}\n" +
                    $"Mensagem: {ex.Message}\n" +
                    $"StackTrace: {ex.StackTrace}\n");
            }
            catch
            {
                // Se falhar o log, continuamos mesmo assim
            }
        }
    }
}
