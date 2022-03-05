using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace flowpad.Controls.Ribbon
{
    public class TabbedCommandBarItem : CommandBar
    {
        private ItemsControl PrimaryItemsControl;
        private Button MoreButton;

        /// <summary>
        /// Creates a new instance of a <see cref="TabbedCommandBarItem"/>.
        /// </summary>
        public TabbedCommandBarItem()
        {
            DefaultStyleKey = typeof(TabbedCommandBarItem);
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(string), typeof(TabbedCommandBarItem), new PropertyMetadata("Test")
        );
        /// <summary>
        /// The title of this ribbon tab.
        /// </summary>
        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
            nameof(Footer), typeof(UIElement), typeof(TabbedCommandBarItem), new PropertyMetadata(new Grid())
        );
        public UIElement Footer
        {
            get => (UIElement)GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        public static readonly DependencyProperty IsContextualProperty = DependencyProperty.Register(
            nameof(IsContextual), typeof(bool), typeof(TabbedCommandBarItem), new PropertyMetadata(false)
        );
        public bool IsContextual
        {
            get => (bool)GetValue(IsContextualProperty);
            set => SetValue(IsContextualProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            PrimaryItemsControl = GetTemplateChild("PrimaryItemsControl") as ItemsControl;
            if (PrimaryItemsControl != null)
            {
                PrimaryItemsControl.HorizontalAlignment = HorizontalAlignment.Left;
            }

            MoreButton = GetTemplateChild("MoreButton") as Button;
            if (MoreButton != null)
            {
                MoreButton.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }
    }
}
