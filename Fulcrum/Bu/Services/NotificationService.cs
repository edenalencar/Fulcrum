using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;

namespace Fulcrum.Bu.Services
{
    /// <summary>
    /// Serviço para gerenciar as notificações do sistema Windows
    /// </summary>
    public sealed class NotificationService : IDisposable
    {
        // Implementação do padrão Singleton
        private static readonly Lazy<NotificationService> _instance = new(() => new NotificationService());
        public static NotificationService Instance => _instance.Value;

        private bool _initialized = false;
        private readonly Dictionary<string, int> _notificationGroups = new();
        private DispatcherQueue? _dispatcherQueue;

        /// <summary>
        /// Evento disparado quando uma notificação é ativada pelo usuário
        /// </summary>
        public event EventHandler<string>? NotificationActivated;

        private NotificationService()
        {
            // Construtor privado para implementar o padrão Singleton
        }

        /// <summary>
        /// Inicializa o serviço de notificações
        /// </summary>
        /// <param name="dispatcherQueue">Fila de despacho para operações na thread da UI</param>
        public void Initialize(DispatcherQueue dispatcherQueue)
        {
            if (_initialized) return;

            _dispatcherQueue = dispatcherQueue;

            try
            {
                // Registra o manipulador de ativação de notificações
                AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
                
                // Registra o aplicativo para notificações
                AppNotificationManager.Default.Register();

                _initialized = true;
                System.Diagnostics.Debug.WriteLine("Serviço de notificações inicializado com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao inicializar serviço de notificações: {ex.Message}");
            }
        }

        /// <summary>
        /// Envia uma notificação simples
        /// </summary>
        /// <param name="title">Título da notificação</param>
        /// <param name="message">Mensagem a ser exibida</param>
        /// <param name="group">Grupo de notificações (opcional)</param>
        public void ShowNotification(string title, string message, string group = "default")
        {
            if (!_initialized)
            {
                System.Diagnostics.Debug.WriteLine("Serviço de notificações não inicializado. A notificação não será exibida.");
                return;
            }

            try
            {
                // Cria uma nova notificação
                var builder = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message);

                // Adiciona o grupo para organizar as notificações
                if (!string.IsNullOrEmpty(group))
                {
                    builder.SetGroup(group);
                }

                // Envia a notificação
                var notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);

                System.Diagnostics.Debug.WriteLine($"Notificação enviada: {title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao enviar notificação: {ex.Message}");
            }
        }

        /// <summary>
        /// Envia uma notificação do temporizador de sono
        /// </summary>
        /// <param name="title">Título da notificação</param>
        /// <param name="message">Mensagem a ser exibida</param>
        public void ShowSleepTimerNotification(string title, string message)
        {
            try
            {
                var builder = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .SetGroup("SleepTimer");

                // Adiciona ações aos botões (opcional)
                builder.AddButton(new AppNotificationButton("Reabrir")
                    .AddArgument("action", "reopen_app"))
                .AddButton(new AppNotificationButton("Reativar Sons")
                    .AddArgument("action", "play_all"));

                // Envia a notificação
                var notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);
                
                System.Diagnostics.Debug.WriteLine("Notificação do temporizador de sono enviada com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao enviar notificação do temporizador: {ex.Message}");
            }
        }

        /// <summary>
        /// Manipula eventos de notificação invocada
        /// </summary>
        private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            try
            {
                // Obtém os argumentos da notificação
                var arguments = args.Argument;
                
                if (_dispatcherQueue == null)
                {
                    System.Diagnostics.Debug.WriteLine("DispatcherQueue não disponível para processar a ação da notificação");
                    return;
                }

                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (arguments.Contains("action=reopen_app"))
                    {
                        // Disparar evento para reabrir/ativar o app
                        NotificationActivated?.Invoke(this, "reopen_app");
                    }
                    else if (arguments.Contains("action=play_all"))
                    {
                        // Disparar evento para reativar a reprodução
                        NotificationActivated?.Invoke(this, "play_all");
                        AudioManager.Instance.PlayAll();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao processar notificação: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpa todas as notificações do aplicativo
        /// </summary>
        public async Task ClearAllNotifications()
        {
            if (!_initialized) return;

            try
            {
                await AppNotificationManager.Default.RemoveAllAsync();
                System.Diagnostics.Debug.WriteLine("Todas as notificações foram removidas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao limpar notificações: {ex.Message}");
            }
        }

        /// <summary>
        /// Libera recursos utilizados pelo serviço
        /// </summary>
        public void Dispose()
        {
            if (!_initialized) return;

            try
            {
                AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
                AppNotificationManager.Default.Unregister();
                _initialized = false;
                System.Diagnostics.Debug.WriteLine("Serviço de notificações desregistrado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao desregistrar serviço de notificações: {ex.Message}");
            }
        }
    }
}