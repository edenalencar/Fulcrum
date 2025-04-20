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
    /// Chamado quando o aplicativo é iniciado normalmente pelo usuário
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            Window = new MainWindow();
            
            // Inicializa o serviço de notificações
            NotificationService.Instance.Initialize(Window.DispatcherQueue);
            
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
