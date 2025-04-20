using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.System;
using Microsoft.UI.Dispatching;

namespace Fulcrum.Bu.Services
{
    /// <summary>
    /// Serviço para registrar e gerenciar teclas de atalho globais
    /// </summary>
    public sealed class HotKeyService : IDisposable
    {
        private static readonly Lazy<HotKeyService> _instance = new(() => new HotKeyService());
        public static HotKeyService Instance => _instance.Value;

        private IntPtr _windowHandle;
        private readonly Dictionary<int, Action> _registeredHotKeys = new();
        private int _currentId = 1;
        private bool _isDisposed = false;
        private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;

        // Constantes para modificadores
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // Constante para mensagem de hotkey
        private const int WM_HOTKEY = 0x0312;

        // P/Invoke para registrar hotkeys globais
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HotKeyService()
        {
            // Construtor privado para singleton
        }

        /// <summary>
        /// Inicializa o serviço de hotkeys
        /// </summary>
        /// <param name="windowHandle">Handle da janela principal</param>
        /// <param name="dispatcherQueue">DispatcherQueue para executar ações na thread da UI</param>
        public void Initialize(IntPtr windowHandle, Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
        {
            _windowHandle = windowHandle;
            _dispatcherQueue = dispatcherQueue;
            System.Diagnostics.Debug.WriteLine("HotKeyService inicializado");
        }

        /// <summary>
        /// Registra uma tecla de atalho global
        /// </summary>
        /// <param name="key">Tecla virtual</param>
        /// <param name="modifiers">Modificadores (Alt, Ctrl, Shift, Win)</param>
        /// <param name="action">Ação a ser executada quando a tecla for pressionada</param>
        /// <returns>ID do registro ou -1 se falhar</returns>
        public int RegisterHotKey(VirtualKey key, HotKeyModifiers modifiers, Action action)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(HotKeyService));
            
            uint modValue = 0;
            if (modifiers.HasFlag(HotKeyModifiers.Alt)) modValue |= MOD_ALT;
            if (modifiers.HasFlag(HotKeyModifiers.Control)) modValue |= MOD_CONTROL;
            if (modifiers.HasFlag(HotKeyModifiers.Shift)) modValue |= MOD_SHIFT;
            if (modifiers.HasFlag(HotKeyModifiers.Windows)) modValue |= MOD_WIN;
            modValue |= MOD_NOREPEAT; // Evitar repetição automática

            int id = _currentId++;
            
            if (RegisterHotKey(_windowHandle, id, modValue, (uint)key))
            {
                _registeredHotKeys[id] = action;
                System.Diagnostics.Debug.WriteLine($"Tecla de atalho registrada com ID {id}");
                return id;
            }
            
            System.Diagnostics.Debug.WriteLine($"Falha ao registrar tecla de atalho {key} com modificador {modifiers}");
            return -1;
        }

        /// <summary>
        /// Remove o registro de uma tecla de atalho
        /// </summary>
        /// <param name="id">ID do registro a ser removido</param>
        public void UnregisterHotKey(int id)
        {
            if (_isDisposed) return;
            
            if (_registeredHotKeys.ContainsKey(id))
            {
                UnregisterHotKey(_windowHandle, id);
                _registeredHotKeys.Remove(id);
                System.Diagnostics.Debug.WriteLine($"Tecla de atalho com ID {id} desregistrada");
            }
        }

        /// <summary>
        /// Processa mensagens de tecla de atalho
        /// </summary>
        /// <param name="msg">Mensagem do Windows</param>
        /// <returns>True se a mensagem foi processada</returns>
        public bool ProcessWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_registeredHotKeys.TryGetValue(id, out var action) && action != null && _dispatcherQueue != null)
                {
                    // Executar a ação na thread da UI
                    _dispatcherQueue.TryEnqueue(() => {
                        action.Invoke();
                    });
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Libera todos os recursos
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                foreach (var id in _registeredHotKeys.Keys)
                {
                    UnregisterHotKey(_windowHandle, id);
                }
                _registeredHotKeys.Clear();
                _isDisposed = true;
                System.Diagnostics.Debug.WriteLine("HotKeyService liberado");
            }
        }
    }

    /// <summary>
    /// Modificadores para teclas de atalho
    /// </summary>
    [Flags]
    public enum HotKeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }
}