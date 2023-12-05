using Fulcrum.Bu;
using Fulcrum.Util;
using Fulcrum.View;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fulcrum
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public Reprodutor cafeteria;
        public Reprodutor chuva;
        public Reprodutor fogueira;
        public Reprodutor lancha;
        public Reprodutor ondas;
        public Reprodutor passaros;
        public Reprodutor praia;
        public Reprodutor trem;
        public Reprodutor ventos;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(700, 130, 390, 700));
            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };
            SetTitleBar(AppTitleBar);            
            IniciarSons();
        }

        public void IniciarSons()
        {
            cafeteria = new ReprodutorCafeteria();
            chuva = new ReprodutorChuva();
            fogueira = new ReprodutorFogueira();
            lancha = new ReprodutorLancha();
            ondas = new ReprodutorOndas();
            passaros = new ReprodutorPassaros();
            praia = new ReprodutorPraia();
            trem = new ReprodutorTrem();
            ventos = new ReprodutorVentos();
            playpause.Symbol = Symbol.Pause;
        }

        public void PlaySons()
        {
            cafeteria.Play();
            chuva.Play();
            fogueira.Play();
            lancha.Play();
            ondas.Play();
            passaros.Play();
            praia.Play();
            trem.Play();
            ventos.Play();
            playpause.Symbol = Symbol.Pause;
        }

        public void PausarSons()
        {
            cafeteria.Pause();
            chuva.Pause();
            fogueira.Pause();
            lancha.Pause();
            ondas.Pause();
            passaros.Pause();
            praia.Pause();
            trem.Pause();
            ventos.Pause();
            playpause.Symbol = Symbol.Play;
        }

        public void AlterarVolume(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (playpause.Symbol == Symbol.Play)
            {
                PlaySons();
            }
            Slider slider = sender as Slider;
            if (slider != null && slider.Tag.ToString() == Constantes.cafeteria)
            {
                cafeteriastatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                cafeteria.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.chuva)
            {
                chuvastatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                chuva.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.fogueira)
            {
                fogueirastatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                fogueira.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.lancha)
            {
                lanchastatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                lancha.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.ondas)
            {
                ondasstatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                ondas.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.passaros)
            {
                passarosstatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                passaros.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.praia)
            {
                praiastatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                praia.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.trem)
            {
                tremstatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                trem.AlterarVolume(slider.Value / 100f);
            }
            else if (slider != null && slider.Tag.ToString() == Constantes.ventos)
            {
                ventostatus.Foreground = slider.Value == 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                ventos.AlterarVolume(slider.Value / 100f);
            }
        }

        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                FrameNavigationOptions navOptions = new FrameNavigationOptions();
                navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;
                Type pageType = typeof(SettingsPage);
                _ = ContentFrame.Navigate(pageType);
            }
            else
            {
                if (playpause.Symbol == Symbol.Pause)
                {
                    PausarSons();
                }
                else
                {
                    PlaySons();
                }
            }
        }

    }
}
