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
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;
using Lumia.Sense;
using Places.Common;
using System.Collections.Generic;
using Windows.Globalization;
using Places.Utilities;
using System.Threading;

namespace Places
{
    /// <summary>
    /// Application main page
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
        /// </summary>
        private ObservableDictionary _defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// Index of the currently active known place
        /// </summary>
        private int _currentKnownPlaceIndex = -1;

        /// <summary>
        /// Constructs a new ResourceLoader object
        /// </summary>
        private ResourceLoader _resourceLoader = new ResourceLoader();

        /// <summary>
        /// List with days for the filter option
        /// </summary>
        private readonly List<DaySelectionItem> _daySelectionList = new List<DaySelectionItem>();
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MapPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            if( _instanceMap == null )
            {
                _instanceMap = this;
            }
            this.menuFlyout = new MenuFlyout();
            this._navigationHelper = new NavigationHelper( this );
            PlacesMap.MapServiceToken = "xxx";
            PopulateDaySelectionList();
            DaySelectionSource.Source = _daySelectionList;

            // Activate and deactivate the SensorCore when the visibility of the app changes
            Window.Current.VisibilityChanged += async ( oo, ee ) =>
            {
                if( !ee.Visible )
                {
                    if( _placeMonitor != null )
                    {
                        await CallSensorcoreApiAsync( async () => { await _placeMonitor.DeactivateAsync(); } );
                    }
                }
                else
                {
                    await ValidateSettingsAsync();
                    if( _placeMonitor == null )
                    {
                        await InitializeAsync();
                    }
                    else
                    {
                        await CallSensorcoreApiAsync( async () => { await _placeMonitor.ActivateAsync(); } );
                    }
                }
            };
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this page
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this._defaultViewModel; }
        }

        #region NavigationHelper registration
        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override async void OnNavigatedTo( NavigationEventArgs e )
        {
            this._navigationHelper.OnNavigatedTo( e );
            if( e.NavigationMode == NavigationMode.Back )
            {
                await ValidateSettingsAsync();
                if( _placeMonitor == null )
                {
                    await InitializeAsync();
                }
            }
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            this._navigationHelper.OnNavigatedFrom( e );
        }
        #endregion

        /// <summary>
        /// Populate day selection list
        /// </summary>
        private void PopulateDaySelectionList()
        {
            GeographicRegion userRegion = new GeographicRegion();
            var userDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter( "shortdate", new[] { userRegion.Code } );
            for( int i = 0; i < 7; i++ )
            {
                var nameOfDay = System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames[ ( (int)DateTime.Now.DayOfWeek + 7 - i ) % 7 ];
                if( i == 0 )
                {
                    nameOfDay += " " + _resourceLoader.GetString( "Today" );
                }
                DateTime itemDate = DateTime.Now.Date - TimeSpan.FromDays( i );
                _daySelectionList.Add(
                    new DaySelectionItem(
                        nameOfDay + " " + userDateFormat.Format( itemDate ),
                        itemDate ) );
            }
            // Add the option to show everything
            _daySelectionList.Add( new DaySelectionItem( _resourceLoader.GetString( "All" ), null ) );
        }

        /// <summary>
        /// Day selection button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="args">Event arguments</param>
        private void DayFilterButton_Click( object sender, RoutedEventArgs e )
        {
            if( menuFlyout != null )
            {
                menuFlyout.Items.Clear();
                foreach( var item in DaySelectionSource.View )
                { 
                    flyoutItem = new MenuFlyoutItem();
                    flyoutItem.Text = item.ToString();
                    flyoutItem.Tag = item;
                    flyoutItem.FontSize = 22;
                    flyoutItem.FlowDirection = Windows.UI.Xaml.FlowDirection.LeftToRight;
                    flyoutItem.Click += flyoutItem_Click;
                    menuFlyout.Items.Add( flyoutItem );
                }
                menuFlyout.Items.Add( new MenuFlyoutItem() );
                menuFlyout.ShowAt( CmdBar );
            }
        }

        /// <summary>
        /// Draws the route for all the tracks in the selected day.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains the event data.</param>
        private async void flyoutItem_Click( object sender, RoutedEventArgs e )
        {
            var flyoutItem = e.OriginalSource as MenuFlyoutItem;
            try
            {
                FilterTime.Text = (flyoutItem.Tag as DaySelectionItem ).Name;
                await UpdateKnownPlacesAsync( ( flyoutItem.Tag as DaySelectionItem ).Day.HasValue ? ( flyoutItem.Tag as DaySelectionItem ).Day.Value : (DateTime?)null );
            }
            catch( Exception )
            {
            }
        }

        /// <summary>
        /// Display home on the map, if is recognised
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private async void HomeButton_Click( object sender, Windows.UI.Xaml.RoutedEventArgs e )
        {
            bool foundPlace = false;
            if( _app.Places != null )
            {
                foreach( Place place in _app.Places )
                {
                    if( place.Kind == PlaceKind.Home )
                    {
                        PlacesMap.Center = new Geopoint( place.Position );
                        PlacesMap.ZoomLevel = 13;
                        foundPlace = true;
                        break;
                    }
                }
            }
            // It takes some time for SensorCore SDK to figure out your known locations
            if( !foundPlace )
            {
                var messageText = _resourceLoader.GetString( "HomeNotFound" ) + " " + _resourceLoader.GetString( "DontWorry/Text" );
                var messageHeader = _resourceLoader.GetString( "LocationNotDefined" );
                var messageDialog = new MessageDialog( messageText, messageHeader );
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Display work on the map, if is recognised
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private async void WorkButton_Click( object sender, Windows.UI.Xaml.RoutedEventArgs e )
        {
            bool foundPlace = false;
            if( _app.Places != null )
            {
                foreach( Place place in _app.Places )
                {
                    if( place.Kind == PlaceKind.Work )
                    {
                        PlacesMap.Center = new Geopoint( place.Position );
                        PlacesMap.ZoomLevel = 13;
                        foundPlace = true;
                        break;
                    }
                }
            }
            // It takes some time for SensorCore SDK to figure out your known locations
            if( !foundPlace )
            {
                var messageText = _resourceLoader.GetString( "WorkNotFound" ) + " " + _resourceLoader.GetString( "DontWorry/Text" );
                var messageHeader = _resourceLoader.GetString( "LocationNotDefined" );
                var messageDialog = new MessageDialog( messageText, messageHeader );
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Display on the map the current location
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private async void CurrentButton_Click( object sender, Windows.UI.Xaml.RoutedEventArgs e )
        {
            if( _currentLocation == null )
            {
                var text = _resourceLoader.GetString( "CurrentNotFound" ) + " " + _resourceLoader.GetString( "DontWorry/Text" );
                var header = _resourceLoader.GetString( "LocationNotDefined" );
                var dialog = new MessageDialog( text, header );
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
        private async void FrequentButton_Click( object sender, Windows.UI.Xaml.RoutedEventArgs e )
        {
            if( _app.Places != null )
            {
                var notHomeNorWork =
                    from place in _app.Places
                    where place.Kind != PlaceKind.Home && place.Kind != PlaceKind.Work
                    orderby place.Kind descending
                    select place;
                if( notHomeNorWork.Count() == 0 )
                {
                    var text = _resourceLoader.GetString( "FrequentNotFound" ) + " " + _resourceLoader.GetString( "DontWorry/Text" );
                    var header = _resourceLoader.GetString( "LocationNotDefined" );
                    var dialog = new MessageDialog( text, header );
                    await dialog.ShowAsync();
                    return;
                }
                _currentKnownPlaceIndex++;
                if( _currentKnownPlaceIndex >= notHomeNorWork.Count() )
                {
                    _currentKnownPlaceIndex = 0;
                }
                PlacesMap.Center = new Geopoint( notHomeNorWork.ElementAt( _currentKnownPlaceIndex ).Position );
                PlacesMap.ZoomLevel = 13;
            }
            else
            {
                var text = _resourceLoader.GetString( "FrequentNotFound" ) + " " + _resourceLoader.GetString( "DontWorry/Text" );
                var header = _resourceLoader.GetString( "LocationNotDefined" );
                var dialog = new MessageDialog( text, header );
                await dialog.ShowAsync();
                return;
            }
        }

        /// <summary>
        /// Navigates to details page for the selected place
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">Provides data about user input for the map tapped</param>
        private void OnTapped(MapControl sender, MapInputEventArgs args)
        {
            try
            {
                var elementList = PlacesMap.FindMapElementsAtOffset(args.Position);
                foreach (var element in elementList)
                {
                    var mapIcon = element as MapIcon;
                    if (mapIcon != null)
                    {
                        Place place = MapExtensions.GetValue(mapIcon);
                        var frame = Window.Current.Content as Frame;
                        var resultStr = place.Kind + "\n" + place.Position.Latitude.ToString() + "\n" + place.Position.Longitude.ToString() + "\n" +
                            place.Radius.ToString() + "\n" + place.LengthOfStay.ToString() + "\n" + place.TotalLengthOfStay.ToString() + "\n" + place.TotalVisitCount.ToString();
                        this.Frame.Navigate(typeof(PivotPage), resultStr);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Place history button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void HistoryButton_Click( object sender, Windows.UI.Xaml.RoutedEventArgs e )
        {
            // Fetch complete stack of places
            if( _placeHistory.Count != 0 )
            {
                // Pass list of places to HistoryPage
                this.Frame.Navigate( typeof( HistoryPage ), _placeHistory );
            }
            else
            {
                // Show message if no history data 
                MessageDialog dialog = new MessageDialog( _resourceLoader.GetString( "NoHistoryData/Text" ) );
                dialog.Commands.Add( new UICommand( _resourceLoader.GetString( "OkButton/Text" ) ) );
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Navigate to about page
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Contains state information and event data associated with a routed event.</param>
        private void AboutButton_Click( object sender, Windows.UI.Xaml.RoutedEventArgs e )
        {
            this.Frame.Navigate( typeof( AboutPage ) );
        }
    }
}
