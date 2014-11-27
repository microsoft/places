using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Places.Common;
using Places.Utilities;
using Lumia.Sense;

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace Places
{
    public sealed partial class PivotPage : Page
    {
        private readonly NavigationHelper _navigationHelper;

        public PivotPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this._navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter != null)
            {
                var icon = e.NavigationParameter as MapIcon;

                if (icon != null)
                {
                    CreatePivotItem(MapExtensions.GetValue(icon));
                }
            }
        }

        /// <summary>
        /// Create a Pivot and PivotItem, and fill with place info
        /// </summary>
        /// <param name="place"></param>
        private void CreatePivotItem(Place place)
        {
            MainGrid.Children.Clear();
            var pivot = new Pivot {Title = "SENSORCORE SAMPLE", Margin = new Thickness(0, 12, 0, 0), Foreground=new SolidColorBrush(Colors.Black) };

            var item = new PivotItem { Header = place.Kind.ToString(), Foreground = new SolidColorBrush(Colors.Black)};
            
            var stack = new StackPanel();
            stack.Children.Add(CreateTextBlock("Latitude:", place.Position.Latitude.ToString()));
            stack.Children.Add(CreateTextBlock("Longitude:", place.Position.Longitude.ToString()));
            stack.Children.Add(CreateTextBlock("Radius:", place.Radius.ToString() + " m"));
            item.Content = stack;
            pivot.Items.Add(item);
            
            MainGrid.Children.Add(pivot);
        }

        /// <summary>
        /// Helper method to create a TextBlock to be used in the PivotItem
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TextBlock CreateTextBlock(string text, string value)
        {
            return new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 32,
                Text = text + " " + value
            };
        }
        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}
