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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Places.Utilities;
using System;
using Places.Common;
using System.Threading.Tasks;
using Lumia.Sense;
using System.Linq;
using System.Globalization;
using Windows.UI.Popups;

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
        /// Navigation Helper instance
        /// </summary>
        private readonly NavigationHelper _navigationHelper;
        /// <summary>
        /// List with place history
        /// </summary>
        private List<string> _myData;

        /// <summary>
        /// List of activities for a place
        /// </summary>
        List<Activity> activitiesToShow = new List<Activity>();

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

            SuspensionManager.KnownTypes.Add(typeof(List<string>));
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
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            await MapPage._instanceMap._sync.WaitAsync();
            try
            {
                _myData = await GetPlacesHistory();
            }
            finally
            {
                MapPage._instanceMap._sync.Release();
            }

            if (_myData.Count != 0)
            {
                CreatePlacesHistory();
            }
            else
            {
                // Show message if no history data 
                MessageDialog dialog = new MessageDialog("No places found", "Information");
                dialog.Commands.Add(new UICommand("Ok"));
                await dialog.ShowAsync();
                this.Frame.Navigate(typeof(MapPage));
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Retrieves the places history
        /// </summary>
        /// <returns>String of all places history</returns>
        private async Task<List<string>> GetPlacesHistory()
        {
            List<string> resultStrFinal = new List<string>();
            IList<Place> result = null;
            if (MapPage._instanceMap._placeMonitor != null)
            {
                // Returns time ordered list of places visited during given time period
                await MapPage._instanceMap.CallSensorcoreApiAsync(async () =>
                {
                    result = await MapPage._instanceMap._placeMonitor.GetPlaceHistoryAsync(DateTime.Today - TimeSpan.FromDays(10), TimeSpan.FromDays(10));
                });
                // Returns list of activies occured during given time period
                await MapPage._instanceMap.CallSensorcoreApiAsync(async () =>
                {
                    ActivityReader.Instance().History = await ActivityReader.Instance().ActivityMonitorProperty.GetActivityHistoryAsync(DateTime.Today - TimeSpan.FromDays(10), TimeSpan.FromDays(10));
                });
                if (result != null)
                {
                    IEnumerable<Place> reverse = result.AsEnumerable().Reverse();
                    for (int i = 1; i < reverse.Count(); i++)
                    {
                        activitiesToShow.Clear();
                        for (int j = 1; j < ActivityReader.Instance().History.Count; j++)
                        {
                            // Compare time of entry to the last location and the current location with the activity timestamp
                            // Retrieve the activities in a list
                            if ((ActivityReader.Instance().History.ElementAt(j).Timestamp.ToLocalTime() >= reverse.ElementAt(i - 1).Timestamp.ToLocalTime()) && (ActivityReader.Instance().History.ElementAt(j).Timestamp.ToLocalTime() < reverse.ElementAt(i).Timestamp.ToLocalTime()))
                            {
                                if (!activitiesToShow.Contains(ActivityReader.Instance().History.ElementAt(j).Mode))
                                {
                                    //Add the activity to the list
                                    activitiesToShow.Add(ActivityReader.Instance().History.ElementAt(j).Mode);
                                }
                            }
                        }
                        string time = reverse.ElementAt(i).Timestamp.ToString("MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        var resultStr = "Place : " + reverse.ElementAt(i).Kind.ToString() + "\nTimestamp : " + time + "\nLenght Of Stay : " + reverse.ElementAt(i).LengthOfStay.ToString() + "\nTotal Lenght Of Stay : " + reverse.ElementAt(i).TotalLengthOfStay.ToString() + "\nTotal Visit Count : " + reverse.ElementAt(i).TotalVisitCount.ToString() + DisplayMembers(activitiesToShow);
                        resultStrFinal.Add(resultStr);
                    }
                }
            }
            // Returns a list with details of the places history
            return resultStrFinal.ToList();
        }

        public string DisplayMembers(List<Activity> activities)
        {
            string displayActivities = string.Empty;
            if (activities.Count != 0)
            {
                displayActivities = "\nActivities: " + string.Join(", ", activities.ToList());
            }
            else
            {
                displayActivities = "\nActivities: Idle";
            }
            return displayActivities;
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