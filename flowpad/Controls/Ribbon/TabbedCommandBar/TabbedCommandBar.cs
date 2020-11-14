using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Animation;

namespace flowpad.Controls.Ribbon
{
    [ContentProperty(Name = nameof(Items))]
    public class TabbedCommandBar : Control
    {
        private NavigationView RibbonNavigationView = null;
        private ContentControl RibbonContent = null;
        private Storyboard TabChangedStoryboard = null;

        // This should probably be made public at some point
        private TabbedCommandBarItem SelectedTab { get; set; }

        // I would prefer this be an IList<TabbedCommandBarItem>, but Intellisense really doesn't like that.
        /// <summary>
        /// A list of <see cref="TabbedCommandBarItem"/>s to display in this <see cref="TabbedCommandBar"/>.
        /// </summary>
        public IList<object> Items
        {
            get { return (IList<object>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IList<object>), typeof(TabbedCommandBar), new PropertyMetadata(new List<object>()));

        /// <summary>
        /// A <see cref="UIElement"/> to be displayed in the footer of the ribbon tab strip.
        /// </summary>
        public UIElement Footer
        {
            get { return (UIElement)GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }
        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register(nameof(Footer), typeof(UIElement), typeof(TabbedCommandBar), new PropertyMetadata(new Border()));

        /// <summary>
        /// Creates a new instance of a <see cref="TabbedCommandBar"/>.
        /// </summary>
        public TabbedCommandBar()
        {
            DefaultStyleKey = typeof(TabbedCommandBar);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Get RibbonContent first, since setting SelectedItem requires it
            RibbonContent = GetTemplateChild("PART_" + nameof(RibbonContent)) as ContentControl;

            RibbonNavigationView = GetTemplateChild("PART_" + nameof(RibbonNavigationView)) as NavigationView;
            if (RibbonNavigationView != null)
            {
                // Populate the NavigationView with items
                // TODO: Get binding working, necessary for contextual tabs
                RibbonNavigationView.MenuItems.Clear();
                foreach (TabbedCommandBarItem item in Items)
                {
                    RibbonNavigationView.MenuItems.Add(item);
                }
                RibbonNavigationView.PaneFooter = Footer;

                RibbonNavigationView.SelectionChanged += RibbonNavigationView_SelectionChanged;
                RibbonNavigationView.SelectedItem = RibbonNavigationView.MenuItems.FirstOrDefault();
            }

            TabChangedStoryboard = GetTemplateChild(nameof(TabChangedStoryboard)) as Storyboard;
        }

        private void RibbonNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is TabbedCommandBarItem item)
            {
                RibbonContent.Content = item;
                TabChangedStoryboard?.Begin();
            }
            else if (args.SelectedItem is NavigationViewItem navItem)
            {
                // This code is a hack and is only temporary, because I can't get binding to work.
                // RibbonContent might be null here, there should be a check
                RibbonContent.Content = Items[System.Math.Min(Items.Count - 1, RibbonNavigationView.MenuItems.IndexOf(navItem))];
            }
        }
    }
}
