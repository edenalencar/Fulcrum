using System;
using System.Threading;
using System.Threading.Tasks;
using Fulcrum.Bu.Services;

namespace Fulcrum.Bu;

/// <summary>
/// Serviço para gerenciar o temporizador de sono (sleep timer)
/// </summary>
public sealed class SleepTimerService
{
    // Implementação do padrão Singleton
    private static readonly Lazy<SleepTimerService> _instance = new(() => new SleepTimerService());
    public static SleepTimerService Instance => _instance.Value;

    // Campos privados
    private readonly object _lockObject = new();
    private CancellationTokenSource? _timerCts;
    private DateTime _endTime;
    private int _remainingMinutes;
    private bool _isTimerActive;

    // Eventos para informar quando o temporizador muda
    public event EventHandler<int>? TimerUpdated;         // Dispara quando o tempo restante é atualizado
    public event EventHandler? TimerStarted;              // Dispara quando o temporizador é iniciado
    public event EventHandler? TimerCancelled;            // Dispara quando o temporizador é cancelado
    public event EventHandler? TimerCompleted;            // Dispara quando o temporizador termina naturalmente
    
    /// <summary>
    /// Construtor privado para Singleton
    /// </summary>
    private SleepTimerService() { }

    /// <summary>
    /// Obtém se o temporizador está ativo
    /// </summary>
    public bool IsTimerActive => _isTimerActive;

    /// <summary>
    /// Obtém o tempo restante em minutos
    /// </summary>
    public int RemainingMinutes => _remainingMinutes;

    /// <summary>
    /// Obtém a hora de término programada
    /// </summary>
    public DateTime EndTime => _endTime;

    /// <summary>
    /// Inicia um temporizador de sono
    /// </summary>
    /// <param name="minutes">Duração do temporizador em minutos</param>
    /// <returns>True se o temporizador foi iniciado com sucesso</returns>
    public bool StartTimer(int minutes)
    {
        if (minutes <= 0)
            return false;

        lock (_lockObject)
        {
            // Cancela um temporizador existente, se houver
            CancelTimer();

            // Configura o novo temporizador
            _timerCts = new CancellationTokenSource();
            _isTimerActive = true;
            _remainingMinutes = minutes;
            _endTime = DateTime.Now.AddMinutes(minutes);

            // Notifica os ouvintes que o temporizador foi iniciado
            TimerStarted?.Invoke(this, EventArgs.Empty);

            // Inicia a tarefa de temporizador em segundo plano
            RunTimerAsync(_timerCts.Token);

            return true;
        }
    }

    /// <summary>
    /// Cancela o temporizador de sono ativo
    /// </summary>
    public void CancelTimer()
    {
        lock (_lockObject)
        {
            if (_isTimerActive && _timerCts != null)
            {
                _timerCts.Cancel();
                _timerCts.Dispose();
                _timerCts = null;
                _isTimerActive = false;
                _remainingMinutes = 0;

                // Notifica os ouvintes que o temporizador foi cancelado
                TimerCancelled?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Executa o temporizador como uma tarefa assíncrona
    /// </summary>
    private async void RunTimerAsync(CancellationToken token)
    {
        try
        {
            // Temporizador principal
            while (_remainingMinutes > 0 && !token.IsCancellationRequested)
            {
                // Aguarda 1 minuto (ou menos para testes)
                await Task.Delay(TimeSpan.FromMinutes(1), token);

                lock (_lockObject)
                {
                    if (!_isTimerActive) break;

                    _remainingMinutes--;
                    
                    // Notifica os ouvintes sobre a atualização do temporizador
                    TimerUpdated?.Invoke(this, _remainingMinutes);
                }
            }

            // Se chegamos aqui sem cancelamento, o temporizador terminou naturalmente
            if (!token.IsCancellationRequested)
            {
                lock (_lockObject)
                {
                    _isTimerActive = false;
                    _timerCts?.Dispose();
                    _timerCts = null;

                    // Notifica os ouvintes que o temporizador foi concluído
                    TimerCompleted?.Invoke(this, EventArgs.Empty);

                    // Pausa todos os áudios quando o temporizador termina
                    AudioManager.Instance.PauseAll();
                    
                    // Envia notificação do sistema
                    NotificationService.Instance.ShowSleepTimerNotification(
                        "Temporizador de Sono Concluído", 
                        "O temporizador de sono foi encerrado e todos os sons foram pausados."
                    );
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Operação de cancelamento normal, não precisa fazer nada
        }
        catch (Exception ex)
        {
            // Log de erro
            System.Diagnostics.Debug.WriteLine($"Erro no temporizador de sono: {ex.Message}");
        }
    }

    /// <summary>
    /// Formata o tempo restante para exibição
    /// </summary>
    /// <returns>String formatada do tempo restante (ex: "35 min")</returns>
    public string GetFormattedTimeRemaining()
    {
        if (!_isTimerActive) return string.Empty;

        if (_remainingMinutes < 60)
        {
            return $"{_remainingMinutes} min";
        }
        else
        {
            int hours = _remainingMinutes / 60;
            int mins = _remainingMinutes % 60;
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }
    }
}