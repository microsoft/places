/*	
The MIT License (MIT)
Copyright (c) 2015 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. 
 */
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Places.Common;
using Places.Utilities;
using Windows.Devices.Geolocation;

/// <summary>
/// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641
/// </summary>
namespace Places
{
    /// <summary>
    /// Details page for a geographical point
    /// </summary>   
    public sealed partial class PivotPage : Page
    {
        #region Private members
        /// <summary>
        /// Navigation Helper instance
        /// </summary>
        private readonly NavigationHelper _navigationHelper;

        /// <summary>
        /// Address of a Geopoint location
        /// </summary>
        private string _addressString;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public PivotPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += this.NavigationHelper_LoadState;
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
                var mapIcon = e.NavigationParameter.ToString();
                if (mapIcon != null)
                {

                    CreatePivotItem(mapIcon);
                }
            }
        }

        /// <summary>
        /// Create a Pivot and PivotItem, and fill with place info
        /// </summary>
        /// <param name="place">Place instance</param>
        private async void CreatePivotItem(string place)
        {
            // Get address of a Geopoint location  
            MainGrid.Children.Clear();
            string[] split = place.Split(new Char[] { '\n', ',' });
            Geopoint geoPoint = new Geopoint(new BasicGeoposition()
        {
            Latitude = Convert.ToDouble(split[1]),
            Longitude = Convert.ToDouble(split[2])
        });
            var newPivot = new Pivot { Title = "MICROSOFT SENSORCORE SAMPLE", Margin = new Thickness(0, 12, 0, 0), Foreground = new SolidColorBrush(Colors.Black) };
            var pivotItem = new PivotItem { Header = split[0].ToString(), Foreground = new SolidColorBrush(Colors.Black), FontSize = 20 };
            var stackPanel = new StackPanel();
            // Get address of a Geopoint location  
            var addressTask = GeoLocationHelper.GetAddress(geoPoint);
            stackPanel.Children.Add(CreateTextBlock("Latitude:", split[1].ToString()));
            stackPanel.Children.Add(CreateTextBlock("Longitude:", split[2].ToString()));
            stackPanel.Children.Add(CreateTextBlock("Radius:", split[3].ToString() + " m"));
            stackPanel.Children.Add(CreateTextBlock("Length of stay:", split[4].ToString()));
            stackPanel.Children.Add(CreateTextBlock("Total length of stay:", split[5].ToString()));
            stackPanel.Children.Add(CreateTextBlock("Total visit count:", split[6].ToString()));
            pivotItem.Content = stackPanel;
            newPivot.Items.Add(pivotItem);
            MainGrid.Children.Add(newPivot);
            _addressString = await addressTask;
            //Add address to the pivot
            stackPanel.Children.Add(CreateTextBlock("Address:", _addressString));
        }

        /// <summary>
        /// Helper method to create a TextBlock to be used in the PivotItem
        /// </summary>
        /// <param name="text">Inner text that is displayed in the TextBlock </param>
        /// <param name="value">Value for the TextBlock</param>
        /// <returns>TextBlock</returns>
        private static TextBlock CreateTextBlock(string text, string value)
        {
            return new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 27,
                Text = text + " " + value,
                TextWrapping = TextWrapping.Wrap
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
        }

        #region NavigationHelper registration
        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedTo(e);
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedFrom(e);
        }
        #endregion
    }
}