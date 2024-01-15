using Fulcrum.Bu;
using Fulcrum.Util;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fulcrum.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {

        public HomePage()
        {
            this.InitializeComponent();
            IniciarSons();
        }

        private void IniciarSons()
        {
            if (AudioManager.Instance.GetQuantidadeReprodutor == 0)
            {
                AudioManager.Instance.AddAudioPlayer(Constantes.cafeteria, new ReprodutorCafeteria());
                AudioManager.Instance.AddAudioPlayer(Constantes.chuva, new ReprodutorChuva());
                AudioManager.Instance.AddAudioPlayer(Constantes.fogueira, new ReprodutorFogueira());
                AudioManager.Instance.AddAudioPlayer(Constantes.lancha, new ReprodutorLancha());
                AudioManager.Instance.AddAudioPlayer(Constantes.ondas, new ReprodutorOndas());
                AudioManager.Instance.AddAudioPlayer(Constantes.passaros, new ReprodutorPassaros());
                AudioManager.Instance.AddAudioPlayer(Constantes.praia, new ReprodutorPraia());
                AudioManager.Instance.AddAudioPlayer(Constantes.trem, new ReprodutorTrem());
                AudioManager.Instance.AddAudioPlayer(Constantes.ventos, new ReprodutorVentos());
                playpause.Symbol = Symbol.Pause;
            }
            foreach (var item in AudioManager.Instance.GetListReprodutores().Keys)
            {
                RestauraVolume(item);
                RestauraEstadoReprodutor(item);
            }
        }

        private void PlaySons()
        {
            AudioManager.Instance.PlayAll();
            playpause.Symbol = Symbol.Pause;
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
            ToolTipService.SetToolTip(playpause, resourceLoader.GetString("Pause/ToolTipService/ToolTip"));
        }

        private void PausarSons()
        {
            AudioManager.Instance.PauseAll();
            playpause.Symbol = Symbol.Play;
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
            ToolTipService.SetToolTip(playpause, resourceLoader.GetString("Play/ToolTipService/ToolTip"));
        }

        public void PlayOrPause(object sender, TappedRoutedEventArgs e)
        {
            switch (playpause.Symbol)
            {
                case Symbol.Pause:
                    PausarSons();
                    break;
                default:
                    PlaySons();
                    break;
            }
        }

        private void RestauraVolume(string idReprodutor)
        {
            Slider slider = this.FindName(idReprodutor) as Slider;
            slider.Value = AudioManager.Instance.GetReprodutorPorId(idReprodutor).reader.Volume * 100f;
        }

        private void RestauraEstadoReprodutor(string idReprodutor)
        {
            if (AudioManager.Instance.GetReprodutorPorId(idReprodutor).waveOut.PlaybackState == NAudio.Wave.PlaybackState.Paused)
                playpause.Symbol = Symbol.Play;

        }

        public void AlterarVolume(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (playpause.Symbol == Symbol.Play)
                PlaySons();

            Slider slider = sender as Slider;
            FontIcon status = this.FindName(slider.Name + "status") as FontIcon;
            status.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
            AudioManager.Instance.AlterarVolume(slider.Name, slider.Value / 100f);
        }
    }
}

