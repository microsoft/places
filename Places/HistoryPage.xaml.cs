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
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Places.Utilities;

/// <summary>
//  The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace Places
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HistoryPage : Page
    {
        #region Private members
        /// <summary>
        /// List with place history
        /// </summary>
        private List<string> _myData;

        /// <summary>
        /// Image Path for the place type image
        /// </summary>
        private string _imagePath;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public HistoryPage()
        {
            this.InitializeComponent();
            this.Loaded += HistoryPage_Loaded;
        }
       
        /// <summary>
        /// Is called when page loading is done
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        void HistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Find the Image control inside the ListBox Data Template
            CreatePlacesHistory();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get the list of history data passed from previous page 
            _myData = e.Parameter as List<string>;
        }

        /// <summary>
        /// Add places to listbox
        /// </summary>
        /// <param name="targetElement"></param>
        private void CreatePlacesHistory()
        {
            var places = new List<PlaceHistory>();
            // Set image source for each type of place
            foreach (var p in _myData)
            {
                if (p.Contains("Home"))
                {
                    _imagePath = "ms-appx:///Assets/show-home.png";
                }
                if (p.Contains("Work"))
                {
                    _imagePath = "ms-appx:///Assets/show-work.png";
                }
                if (p.Contains("Frequent"))
                { 
                    _imagePath = "ms-appx:///Assets/show-current-location.png";
                }
                if (p.Contains("Known"))
                {
                    _imagePath = "ms-appx:///Assets/show-current-location.png";
                }
                if (p.Contains("Frequent"))
                {
                    _imagePath = "ms-appx:///Assets/show-next-favorite.png";
                }
                PlaceHistory placeHistory = new PlaceHistory() { Text = p, ImagePath = _imagePath };
                places.Add(placeHistory);             
            }
            this.PlacesSummary.DataContext = places;
        }
    }
}