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
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Lumia.Sense;
using Places.Common;
using System.Collections.Generic;
using Windows.Globalization;
using Places.Utilities;
using System.Threading;

/// <summary>
/// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace Places
{
    /// <summary>
    /// All the logic releated to the usage of Place Monitor API.
    /// Shows home, work,and all the known and frequent places (geo-locations) on the map.
    /// </summary>
    public sealed partial class MapPage : Page
    {
        #region Private members
        /// <summary>
        /// Navigation Helper instance
        /// </summary>
        private NavigationHelper _navigationHelper;

        /// <summary>
        /// Map Page instance
        /// </summary>
        public static MapPage _instanceMap;

        /// <summary>
        /// View model instance
        /// This can be changed to a strongly typed view model
        /// </summary>
        private ObservableDictionary _defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// Frequent id to return the place position 
        /// </summary>
        private int _chosenFrequentId = -1;

        /// <summary>
        /// Constructs a new ResourceLoader object
        /// </summary>
        private ResourceLoader _resourceLoader = new ResourceLoader();

        /// <summary>
        /// List with days for the filter option
        /// </summary>
        private readonly List<DaySelectionItem> _optionList = new List<DaySelectionItem>();

        /// <summary>
        /// Selected day for filter option
        /// </summary>
        private DaySelectionItem _selectedDay;

        /// <summary>
        /// Synchronization object
        /// </summary>
        public SemaphoreSlim _sync = new SemaphoreSlim( 1 );
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MapPage()
        {
            this.InitializeComponent();
            if(_instanceMap == null)
               _instanceMap = this;
            this.menuFlyout = new MenuFlyout();
            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this._navigationHelper.SaveState += this.NavigationHelper_SaveState;
            PlacesMap.MapServiceToken = "xxx";
            FillDateList();
            this.listSource.Source = _optionList;
            _selectedDay = _optionList.Last();
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this._defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {       
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
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
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedTo(e);
            ActivateSensorCoreStatus status = _app.SensorCoreActivationStatus;
            if (e.NavigationMode == NavigationMode.Back && status.onGoing)
            {
                status.onGoing = false;
                if (status.activationRequestResult != ActivationRequestResults.AllEnabled)
                {
                    MessageDialog dialog = new MessageDialog(_resourceLoader.GetString("NoLocationOrMotionDataError/Text"), _resourceLoader.GetString("Information/Text"));
                    dialog.Commands.Add(new UICommand(_resourceLoader.GetString("OkButton/Text")));
                    await dialog.ShowAsync();
                    new System.Threading.ManualResetEvent(false).WaitOne(500);
                    Application.Current.Exit();
                }
            }
            PlacesMap.Center = new Geopoint(new BasicGeoposition()
            {
                Latitude = 60.17,
                Longitude = 24.83
            });
            if (ActivityReader.Instance().ActivityMonitorProperty != null)
            {
                ActivityReader.Instance().ActivityMonitorProperty.ReadingChanged += ActivityReader.Instance().activityMonitor_ReadingChanged;
            }
            if (!iLaunched)
            {
                iLaunched = true;
                await InitCore();    
            }              
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedFrom(e);
            if (ActivityReader.Instance().ActivityMonitorProperty != null)
            {
                ActivityReader.Instance().ActivityMonitorProperty.ReadingChanged -= ActivityReader.Instance().activityMonitor_ReadingChanged;
            }
        }
        #endregion

        /// <summary>
        /// Fill the list with current day of week, and in descending order rest of the weekdays
        /// </summary>
        private void FillDateList()
        {
            int today = (int)DateTime.Now.DayOfWeek; // Current day
            int count = 0;
            for (int i = today; i >= 0; i--)
            {
                var item = new DaySelectionItem { Day = DateTime.Now.Date - TimeSpan.FromDays(count) };
                var nameOfDay = System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames[i];
                // Add an indicator to current day
                if (count == 0)
                {
                    nameOfDay += " " + _resourceLoader.GetString("Today");
                }
                GeographicRegion userRegion = new GeographicRegion();
                var userDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate", new[] { userRegion.Code });
                var dateDefault = userDateFormat.Format(item.Day);
                item.Name = nameOfDay + " " + dateDefault;
                _optionList.Add(item);
                count++;
                // First day of the week, but not all weekdays still listed,
                // continue from the last weekday
                if (i == 0 && count <= 6)
                {
                    i = 7;
                }
                else if (count == 10) // All weekdays listed, exit the loop
                {
                    i = 0;
                }
            }
            // Add the option to show everything
            _optionList.Add(new DaySelectionItem { Name = _resourceLoader.GetString("All") });
        }

        /// <summary>
        /// Opens a menu flyout with options to choose from
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains the event data.</param>
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (menuFlyout != null)
            {            
                menuFlyout.Items.Clear();
                for (int i = 0; i < listSource.View.Count; i++)
                {
                    flyoutItem = new MenuFlyoutItem();
                    flyoutItem.Text = this.listSource.View[i].ToString();
                    flyoutItem.FontSize = 22;
                    flyoutItem.FlowDirection = Windows.UI.Xaml.FlowDirection.LeftToRight;
                    flyoutItem.Click += flyoutItem_Click;
                    menuFlyout.Items.Add(flyoutItem);
                }
                menuFlyout.Items.Add(new MenuFlyoutItem());
                menuFlyout.ShowAt(CmdBar);
            }
        }

        /// <summary>
        /// Draws the route for all the tracks in the selected day.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains the event data.</param>
        private async void flyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = e.OriginalSource as MenuFlyoutItem;
            try
            {
                for (int i = 0; i < listSource.View.Count; i++)
                    if (flyoutItem.Text.Contains(listSource.View[i].ToString()))
                        _selectedDay = (DaySelectionItem)listSource.View[i];              
                await UpdateKnownPlacesAsync();
                FilterTime.Text = _selectedDay.Name;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Display home on the map, if is recognised
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private async void OnHomeClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            bool foundPlace = false;
            if (_app.Places != null)
            {
                foreach (Place place in _app.Places)
                {
                    if (place.Kind == PlaceKind.Home)
                    {
                        PlacesMap.Center = new Geopoint(place.Position);
                        PlacesMap.ZoomLevel = 13;
                        foundPlace = true;
                        break;
                    }
                }
            }
            // It takes some time for SensorCore SDK to figure out your known locations
            if (!foundPlace)
            {
                var messageText = _resourceLoader.GetString("HomeNotFound") + " " + _resourceLoader.GetString("DontWorry/Text");
                var messageHeader = _resourceLoader.GetString("LocationNotDefined");
                var messageDialog = new MessageDialog(messageText, messageHeader);
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Display work on the map, if is recognised
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private async void OnWorkClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            bool foundPlace = false;
             if (_app.Places != null)
            {
                foreach (Place place in _app.Places)
                {
                    if (place.Kind == PlaceKind.Work)
                    {
                        PlacesMap.Center = new Geopoint(place.Position);
                        PlacesMap.ZoomLevel = 13;
                        foundPlace = true;
                        break;
                    }
                }
            }
            // It takes some time for SensorCore SDK to figure out your known locations
            if (!foundPlace)
            {
                var messageText = _resourceLoader.GetString("WorkNotFound") + " " + _resourceLoader.GetString("DontWorry/Text");
                var messageHeader = _resourceLoader.GetString("LocationNotDefined");
                var messageDialog = new MessageDialog(messageText, messageHeader);
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Display on the map the current location
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private async void OnCurrentClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_currentLocation == null)
            {
                var text = _resourceLoader.GetString("CurrentNotFound") + " " + _resourceLoader.GetString("DontWorry/Text");
                var header = _resourceLoader.GetString("LocationNotDefined");
                var dialog = new MessageDialog(text, header);
                await dialog.ShowAsync();
            }
            else
            {
                PlacesMap.Center = _currentLocation;
                PlacesMap.ZoomLevel = 13;
            }
        }

        /// <summary>
        /// Display frequent place on the map, if is recognised
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private async void OnFrequentClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_app.Places != null)
            {
                var notHomeNorWork =
                    from place in _app.Places
                    where place.Kind != PlaceKind.Home && place.Kind != PlaceKind.Work
                    orderby place.Kind descending
                    select place;
                if (notHomeNorWork.Count() == 0)
                {
                    var text = _resourceLoader.GetString("FrequentNotFound") + " " + _resourceLoader.GetString("DontWorry/Text");
                    var header = _resourceLoader.GetString("LocationNotDefined");
                    var dialog = new MessageDialog(text, header);
                    await dialog.ShowAsync();
                    return;
                }
                _chosenFrequentId++;
                if (_chosenFrequentId >= notHomeNorWork.Count())
                {
                    _chosenFrequentId = 0;
                }
                PlacesMap.Center = new Geopoint(notHomeNorWork.ElementAt(_chosenFrequentId).Position);
                PlacesMap.ZoomLevel = 13;
            }
            else
            {
                var text = _resourceLoader.GetString("FrequentNotFound") + " " + _resourceLoader.GetString("DontWorry/Text");
                var header = _resourceLoader.GetString("LocationNotDefined");
                var dialog = new MessageDialog(text, header);
                await dialog.ShowAsync();
                return;
            }
        }

        /// <summary>
        /// Get history button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnPollHistory(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
              // Pass list of places to HistoryPage
                this.Frame.Navigate(typeof(HistoryPage));
        }

        /// <summary>
        /// Navigate to about page
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private void OnAboutClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }
    }
}
