using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace flowpad.Controls.Ribbon
{
    public sealed class RibbonGroup : ItemsControl
    {
        public RibbonGroup()
        {
            DefaultStyleKey = typeof(RibbonGroup);
        }

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            "Label", typeof(string), typeof(RibbonGroup), new PropertyMetadata("")
        );
        public string Label {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
