using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace flowpad.Controls.Ribbon
{
    public class RibbonButton : Button, IRibbonButton
    {
        public RibbonButton()
        {
            DefaultStyleKey = typeof(RibbonButton);
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon", typeof(IconElement), typeof(RibbonButton), new PropertyMetadata(new SymbolIcon(Symbol.Add))
        );
        public IconElement Icon {
            get => (IconElement)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            "Label", typeof(string), typeof(RibbonButton), new PropertyMetadata("")
        );
        public string Label {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
            "IsCompact", typeof(bool), typeof(RibbonButton), new PropertyMetadata(false)
        );
        public bool IsCompact {
            get => (bool)GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }
    }

    public interface IRibbonButton
    {
        public IconElement Icon { get; set; }
        public string Label { get; set; }
        public bool IsCompact { get; set; }
    }
}
