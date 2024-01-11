using Fulcrum.View;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(700, 130, 390, 700));
            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };
            SetTitleBar(AppTitleBar);
        }

        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            FrameNavigationOptions navOptions = new FrameNavigationOptions();
            navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;
            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                navOptions.IsNavigationStackEnabled = false;
            }

            var selectedItem = (NavigationViewItem)args.SelectedItem;
            switch (selectedItem.Tag.ToString())
            {
                case "HomeSelected":
                    {
                        Type pageType = typeof(HomePage);
                        _ = contentFrame.Navigate(pageType);
                        break;
                    }

                default:
                    {
                        Type pageType = typeof(SettingsPage);
                        _ = contentFrame.Navigate(pageType);
                        break;
                    }
            }


        }

    }
}
