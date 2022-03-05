using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace flowpad.Controls.Ribbon
{
    public sealed class RibbonColumn : ItemsControl
    {
        public RibbonColumn()
        {
            DefaultStyleKey = typeof(RibbonColumn);
        }

        public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
            "Spacing", typeof(double), typeof(RibbonColumn), new PropertyMetadata(1.0)
        );
        public double Spacing {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }
    }
}
