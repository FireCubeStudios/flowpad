using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace flowpad.Controls.Ribbon
{
    public class TabbedCommandBarItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Normal { get; set; }
        public DataTemplate Contextual { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return ((TabbedCommandBarItem)item).IsContextual ? Contextual : Normal;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}
