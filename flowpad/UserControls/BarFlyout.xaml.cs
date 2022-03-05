using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace flowpad.UserControls
{
    public sealed partial class BarFlyout : UserControl
    {
        public BarFlyout()
        {
            this.InitializeComponent();
        }
        public event EventHandler BackdropTapped;
        private void Backdrop_Tapped(object sender, TappedRoutedEventArgs e) => BackdropTapped?.Invoke(this, new EventArgs());



        public static readonly DependencyProperty PanelContentProperty = DependencyProperty.Register(

            "PanelContent", typeof(FrameworkElement), typeof(BarFlyout), new PropertyMetadata(default(FrameworkElement), OnPanelContentChanged));



        private static void OnPanelContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)

        {

            if (d is BarFlyout instance)

            {

                instance.Presenter.Content = e.NewValue;

            }

        }



        public FrameworkElement PanelContent

        {

            get => (FrameworkElement)GetValue(PanelContentProperty);

            set => SetValue(PanelContentProperty, value);

        }
    }
}
