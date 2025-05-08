using Windows.Storage;
using Windows.System;

namespace Fulcrum.Bu.Services
{
    /// <summary>
    /// Gerencia as teclas de atalho específicas do aplicativo Fulcrum
    /// </summary>
    public class AppHotKeyManager : IDisposable
    {
        private readonly HotKeyService _hotKeyService;
        private readonly AudioManager _audioManager;
        private readonly Dictionary<string, int> _registeredHotKeyIds = new();
        private bool _isDisposed = false;
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        // Chaves para salvar as configurações
        private const string HOTKEYS_SETTINGS_KEY = "HotKeySettings";
        private const string ENABLE_HOTKEYS_KEY = "EnableHotKeys";

        /// <summary>
        /// Inicializa uma nova instância do gerenciador de teclas de atalho
        /// </summary>
        public AppHotKeyManager()
        {
            _hotKeyService = HotKeyService.Instance;
            _audioManager = AudioManager.Instance;
        }

        /// <summary>
        /// Verifica se as teclas de atalho globais estão habilitadas
        /// </summary>
        public bool HotKeysEnabled
        {
            get
            {
                if (_localSettings.Values.ContainsKey(ENABLE_HOTKEYS_KEY))
                {
                    return (bool)_localSettings.Values[ENABLE_HOTKEYS_KEY];
                }
                return true; // Habilitado por padrão
            }
            set
            {
                _localSettings.Values[ENABLE_HOTKEYS_KEY] = value;
                if (value)
                {
                    RegisterAllHotKeys();
                }
                else
                {
                    UnregisterAllHotKeys();
                }
            }
        }

        /// <summary>
        /// Registra todas as teclas de atalho padrão
        /// </summary>
        public void RegisterAllHotKeys()
        {
            if (!HotKeysEnabled) return;

            // Desregistra teclas existentes para evitar duplicidade
            UnregisterAllHotKeys();

            // Reproduzir/Pausar - Ctrl+Alt+P
            RegisterHotKey("PlayPause", VirtualKey.P,
                HotKeyModifiers.Control | HotKeyModifiers.Alt,
                TogglePlayPause);

            // Aumentar volume - Ctrl+Alt+Up
            RegisterHotKey("VolumeUp", VirtualKey.Up,
                HotKeyModifiers.Control | HotKeyModifiers.Alt,
                IncreaseVolume);

            // Diminuir volume - Ctrl+Alt+Down
            RegisterHotKey("VolumeDown", VirtualKey.Down,
                HotKeyModifiers.Control | HotKeyModifiers.Alt,
                DecreaseVolume);

            // Mutar/Desmutar - Ctrl+Alt+M
            RegisterHotKey("Mute", VirtualKey.M,
                HotKeyModifiers.Control | HotKeyModifiers.Alt,
                ToggleMute);

            System.Diagnostics.Debug.WriteLine("Teclas de atalho padrão registradas");
        }

        /// <summary>
        /// Registra uma tecla de atalho específica
        /// </summary>
        private void RegisterHotKey(string name, VirtualKey key, HotKeyModifiers modifiers, Action action)
        {
            // Desregistra se já existir
            UnregisterHotKey(name);

            // Registra o novo atalho
            int id = _hotKeyService.RegisterHotKey(key, modifiers, action);
            if (id != -1)
            {
                _registeredHotKeyIds[name] = id;
            }
        }

        /// <summary>
        /// Remove o registro de uma tecla de atalho
        /// </summary>
        private void UnregisterHotKey(string name)
        {
            if (_registeredHotKeyIds.TryGetValue(name, out int id))
            {
                _hotKeyService.UnregisterHotKey(id);
                _registeredHotKeyIds.Remove(name);
            }
        }

        /// <summary>
        /// Remove o registro de todas as teclas de atalho
        /// </summary>
        public void UnregisterAllHotKeys()
        {
            foreach (var id in _registeredHotKeyIds.Values)
            {
                _hotKeyService.UnregisterHotKey(id);
            }
            _registeredHotKeyIds.Clear();
            System.Diagnostics.Debug.WriteLine("Todas as teclas de atalho foram desregistradas");
        }

        #region Ações de teclas de atalho

        /// <summary>
        /// Ação para alternar reprodução/pausa
        /// </summary>
        private void TogglePlayPause()
        {
            _audioManager.TogglePlayback();
            System.Diagnostics.Debug.WriteLine($"Atalho: Alternar reprodução/pausa - {(_audioManager.IsPlaying ? "Reproduzindo" : "Pausado")}");
        }

        /// <summary>
        /// Ação para aumentar o volume principal
        /// </summary>
        private void IncreaseVolume()
        {
            _audioManager.IncreaseMainVolume(0.05f);
            System.Diagnostics.Debug.WriteLine("Atalho: Aumentar volume global em 5%");
        }

        /// <summary>
        /// Ação para diminuir o volume principal
        /// </summary>
        private void DecreaseVolume()
        {
            _audioManager.DecreaseMainVolume(0.05f);
            System.Diagnostics.Debug.WriteLine("Atalho: Diminuir volume global em 5%");
        }

        /// <summary>
        /// Ação para alternar mudo/com som
        /// </summary>
        private void ToggleMute()
        {
            _audioManager.ToggleMute();
            System.Diagnostics.Debug.WriteLine($"Atalho: Audio {(_audioManager.IsMuted ? "mutado" : "desmutado")}");
        }

        #endregion

        /// <summary>
        /// Libera recursos utilizados pelo gerenciador de teclas de atalho
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                UnregisterAllHotKeys();
                _isDisposed = true;
            }
        }
    }
}