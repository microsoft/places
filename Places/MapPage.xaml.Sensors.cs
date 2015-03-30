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
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Input;
using Lumia.Sense;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Popups;
using Windows.UI.Core;
using Places.Utilities;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Geolocation.Geofencing;
using System.Collections.Generic;
using Monitor = Lumia.Sense.PlaceMonitor;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;
using System.Globalization;
using Windows.Foundation;

namespace Places
{
    /// <summary>
    /// Application main page (sensor related)
    /// </summary>
    public sealed partial class MapPage
    {
        #region Private constants
        /// <summary>
        /// Home color mapping
        /// </summary>
        private readonly Color PLACE_COLOR_HOME = Color.FromArgb( PLACE_CIRCLE_ALPHA, 31, 127, 31 );

        /// <summary>
        /// Work color mapping
        /// </summary>
        private readonly Color PLACE_COLOR_WORK = Color.FromArgb( PLACE_CIRCLE_ALPHA, 255, 163, 31 );

        /// <summary>
        /// Frequent color mapping
        /// </summary>
        private readonly Color PLACE_COLOR_FREQUENT = Color.FromArgb( PLACE_CIRCLE_ALPHA, 127, 127, 255 );

        /// <summary>
        /// Known color mapping
        /// </summary>
        private readonly Color PLACE_COLOR_KNOWN = Color.FromArgb( PLACE_CIRCLE_ALPHA, 255, 127, 127 );

        /// <summary>
        /// Unknown color mapping
        /// </summary>
        private readonly Color PLACE_COLOR_UNKNOWN = Color.FromArgb( PLACE_CIRCLE_ALPHA, 127, 127, 127 );

        /// <summary>
        /// The alpha component from Color structure is set to a specified value
        /// Valid values are 0 through 255.
        /// </summary>
        private const int PLACE_CIRCLE_ALPHA = 127;

        /// <summary>
        /// Mean radius of planet Earth
        /// </summary>
        const int MEAN_RADIUS = 6371;

        /// <summary>
        /// Conversion from one radian to degrees 
        /// </summary>
        private const double RADIAN_TO_DEGREES = 180.0 / Math.PI;

        /// <summary>
        /// Conversion from one degree to radian
        /// </summary>
        private const double DEGREE_TO_RADIAN = Math.PI / 180;

        /// <summary>
        /// Length of the earth's semi-major axis A
        /// </summary
        private const double WGS84A = 6378137.0;

        /// <summary>
        /// Length of the earth's semi-major axis B
        /// </summary
        private const double WGS84B = 6356752.3;
        #endregion

        #region Private members
        /// <summary>
        ///  The application model
        /// </summary>
        private Places.App _app = Application.Current as Places.App;

        /// <summary>
        /// Place history
        /// </summary>
        private List<string> _placeHistory = new List<string>();

        /// <summary>
        /// Place monitor instance
        /// </summary>
        private Monitor _placeMonitor;

        /// <summary>
        /// Geolocator instance
        /// </summary>
        private Geolocator _geoLocator;

        /// <summary>
        /// CancellationToken Source instance
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Current location
        /// </summary>
        private Geopoint _currentLocation;

        /// <summary>
        /// Are we running in full screen mode?
        /// </summary>
        private bool _fullScreen;
        #endregion

        /// <summary>
        /// Makes sure necessary settings are enabled in order to use SensorCore
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task ValidateSettingsAsync()
        {
            try
            {
                if( !( await PlaceMonitor.IsSupportedAsync() ) )
                {
                    MessageDialog dlg = new MessageDialog( "Unfortunately this device does not support viewing visited places" );
                    await dlg.ShowAsync();
                    Application.Current.Exit();
                }
                else
                {
                    MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                    if( !settings.LocationEnabled )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to collect and view visited places you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dlg.ShowAsync();
                    }
                    if( !settings.PlacesVisited )
                    {
                        MessageDialog dlg = null;
                        if( settings.Version < 2 )
                        {
                            // Device has old Motion data settings
                            dlg = new MessageDialog( "In order to collect and view visited places you need to enable Motion data in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        }
                        else
                        {
                            dlg = new MessageDialog( "In order to collect and view visited places you need to enable 'Places visited' in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        }
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dlg.ShowAsync();
                    }
                }
            }
            catch( Exception )
            {
            }
        }

        /// <summary>
        /// Initializes sensors
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task InitializeAsync()
        {
            if( await CallSensorcoreApiAsync( async () => { _placeMonitor = await Monitor.GetDefaultAsync(); } ) )
            {
                // Update list of known places
                await UpdateKnownPlacesAsync( null );
                HomeButton.IsEnabled = true;
                WorkButton.IsEnabled = true;
                FrequentButton.IsEnabled = true;
                CurrentButton.IsEnabled = true;
                _placeHistory = await GetPlacesHistoryAsync();
            }
            else
            {
                Application.Current.Exit();
                return;
            }

            // Init Geolocator
            try
            {
                // Get a geolocator object
                _geoLocator = new Geolocator();
                // Get cancellation token
                _cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = _cancellationTokenSource.Token;
                Geoposition geoposition = await _geoLocator.GetGeopositionAsync().AsTask( token );
                _currentLocation = new Geopoint( new BasicGeoposition()
                {
                    Latitude = geoposition.Coordinate.Point.Position.Latitude,
                    Longitude = geoposition.Coordinate.Point.Position.Longitude
                } );
                // Focus on the current location
                CurrentButton_Click( this, null );
            }
            finally
            {
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Create geo fence around certain geopoint with a given radius
        /// </summary>
        /// <param name="fenceKey">The key of the fence</param>
        /// <param name="latitude">Latitude of the geo fence</param>
        /// <param name="longitude">Longitude of the geo fence</param>
        /// <param name="radius">Radius of the geo fence</param>
        /// <param name="fenceColor">UI color of the geo fence</param>
        private void CreateGeofence( string fenceKey, double latitude, double longitude, double radius, Color fenceColor )
        {
            if( GeofenceMonitor.Current.Geofences.All( v => v.Id != fenceKey ) )
            {
                var position = new BasicGeoposition { Latitude = latitude, Longitude = longitude, Altitude = 0.0 };
                CreateCircle( position, radius, fenceColor );
                // The geofence is a circular region
                var geocircle = new Geocircle( position, radius );
                // Listen for enter geofence and exit geofence events
                MonitoredGeofenceStates mask = 0;
                mask |= MonitoredGeofenceStates.Entered;
                mask |= MonitoredGeofenceStates.Exited;
                var geofence = new Geofence( fenceKey, geocircle, mask, false );
                GeofenceMonitor.Current.Geofences.Add( geofence );
                GeofenceMonitor.Current.StatusChanged += GeoFence_StatusChanged;
            }
            else
            {
                foreach( var f in GeofenceMonitor.Current.Geofences.Where( f => f.Id == fenceKey ) )
                {
                    var x = f.Geoshape as Geocircle;
                    if( x != null )
                    {
                        CreateCircle( x.Center, x.Radius, fenceColor );
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Called when geo fence status changes
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void GeoFence_StatusChanged( GeofenceMonitor sender, object e )
        {
            var reports = sender.ReadReports();
            await Dispatcher.RunAsync( CoreDispatcherPriority.Normal, async () =>
            {
                foreach( GeofenceStateChangeReport report in reports )
                {
                    GeofenceState state = report.NewState;
                    Geofence geofence = report.Geofence;
                    if( state == GeofenceState.Removed )
                    {
                        GeofenceMonitor.Current.Geofences.Remove( geofence );
                    }
                    else if( state == GeofenceState.Entered )
                    {
                        foreach( Place place in _app.Places )
                        {
                            if( place.Id.ToString() == geofence.Id )
                            {
                                var dia = new MessageDialog( "You have entered - " + place.Kind.ToString() );
                                await dia.ShowAsync();
                            }
                        }
                    }
                    else if( state == GeofenceState.Exited )
                    {
                        foreach( Place place in _app.Places )
                        {
                            if( place.Id.ToString() == geofence.Id )
                            {
                                var dia = new MessageDialog( "You have left - " + place.Kind.ToString() );
                                await dia.ShowAsync();
                            }
                        }
                    }
                }
            } );
        }

        /// <summary>
        /// Add a circle icon to the map
        /// </summary>
        /// <param name="position">Position of the location</param>
        /// <param name="radius">Radius of the circle</param>
        /// <param name="circleColor">Color of the circle</param>
        private void CreateCircle( BasicGeoposition position, double radius, Color circleColor )
        {
            var circle = new MapPolygon
            {
                FillColor = circleColor,
                StrokeColor = circleColor,
                StrokeDashed = false,
                StrokeThickness = 1,
                Path = new Geopath( CreateGeoCircle( position, radius ) ),
            };
            PlacesMap.MapElements.Add( circle );
        }

        /// <summary>   
        /// Returns a list of coordinates around a circular area with the given radius
        /// </summary>
        /// <param name="center">The center of the geoposition</param>
        /// <param name="radiusValue">Radius of the circles drawn around the places indicates the uncertainty of the exact location</param>
        /// <param name="numberOfPoints">Number of points to calculate.</param>
        /// <returns>List of points</returns>
        public static List<BasicGeoposition> CreateGeoCircle( BasicGeoposition center, double radiusValue, int numberOfPoints = 360 )
        {
            var locations = new List<BasicGeoposition>();
            double radius = radiusValue / 1000.0;
            double latCenter = center.Latitude * DEGREE_TO_RADIAN;
            double lonCenter = center.Longitude * DEGREE_TO_RADIAN;
            double distance = radius / MEAN_RADIUS;
            for( int i = 0; i <= numberOfPoints; i++ )
            {
                double angle = i * DEGREE_TO_RADIAN;
                var latitude = Math.Asin( Math.Sin( latCenter ) * Math.Cos( distance ) + Math.Cos( latCenter ) * Math.Sin( distance ) * Math.Cos( angle ) );
                var longitude = ( ( lonCenter + Math.Atan2( Math.Sin( angle ) * Math.Sin( distance ) * Math.Cos( latCenter ), Math.Cos( distance ) - Math.Sin( latCenter ) * Math.Sin( latitude ) ) + Math.PI ) % ( Math.PI * 2 ) ) - Math.PI;
                locations.Add( new BasicGeoposition { Latitude = latitude * RADIAN_TO_DEGREES, Longitude = longitude * RADIAN_TO_DEGREES } );
            }
            return locations;
        }

        /// <summary>
        /// Updates list of known places
        /// </summary>
        /// <param name="date">Date to search places from. Use <c>null</c> to search entire history.</param>
        /// <returns>Asynchronous task</returns>
        private async Task UpdateKnownPlacesAsync( DateTime? date )
        {
            if( _placeMonitor != null )
            {
                _app.Places = null;
                PlacesMap.MapElements.Clear();
                PlacesMap.Children.Clear();
                if( await CallSensorcoreApiAsync( async () =>
                {
                    if( date.HasValue )
                    {
                        _app.Places = await _placeMonitor.GetPlaceHistoryAsync( date.Value.Date, TimeSpan.FromHours( 24 ) );
                    }
                    else
                    {
                        _app.Places = await _placeMonitor.GetKnownPlacesAsync();
                    }
                } ) )
                {
                    // Make sure that there were Places for the timespan
                    if( _app.Places != null && 
                        _app.Places.Count > 0 )
                    {
                        PlacesMap.Children.Clear();
                       
                        foreach( var p in _app.Places )
                        {
                            System.Diagnostics.Debug.WriteLine( "Place {0} radius {1} Latitude {2} Longitude {3} Total visit count {4} Total length of stay {5} Length of stay {6} Kind {7} ", p.Kind, p.Radius, p.Position.Latitude, p.Position.Longitude, p.TotalVisitCount, p.TotalLengthOfStay, p.LengthOfStay, p.Kind );
                            var mapIcon = new MapIcon();
                            MapExtensions.SetValue(mapIcon, p);
                            mapIcon.NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 0.5);
                            mapIcon.Location = new Geopoint(p.Position);
                            mapIcon.Title = "'" + p.Kind.ToString() + "'" + " place";
                            Color placesColor;
                            //Set for each type of a place a certain color and set custom image for the MapIcon
                            switch( p.Kind )
                            {
                                case PlaceKind.Home:
                                    placesColor = PLACE_COLOR_HOME;
                                    break;
                                case PlaceKind.Work:
                                    placesColor = PLACE_COLOR_WORK;
                                    break;
                                case PlaceKind.Frequent:
                                    placesColor = PLACE_COLOR_FREQUENT;
                                    break;
                                case PlaceKind.Known:
                                    placesColor = PLACE_COLOR_KNOWN;
                                    break;
                                default:
                                    placesColor = PLACE_COLOR_UNKNOWN;
                                    break;
                            }
                            PlacesMap.MapElements.Add(mapIcon);
                            CreateGeofence(p.Id.ToString(), p.Position.Latitude, p.Position.Longitude, p.Radius, placesColor);
                        }
                    }
                }
            }
            Debug.WriteLine( "Known places updated." );
        }

        /// <summary>
        /// Performs asynchronous Sensorcore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">The function delegate to execute asynchronously when one task in the tasks completes.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public async Task<bool> CallSensorcoreApiAsync( Func<Task> action )
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch( Exception e )
            {
                failure = e;
            }
            if( failure != null )
            {
                switch( SenseHelper.GetSenseError( failure.HResult ) )
                {
                    case SenseError.LocationDisabled:
                        MessageDialog dialog = new MessageDialog( "In order to collect and view visited places you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        dialog.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dialog.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent( false ).WaitOne( 500 );
                        return false;
                    case SenseError.SenseDisabled:
                        {
                            MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                            MessageDialog dlg = null;
                            if( settings.Version < 2 )
                            {
                                // Device has old Motion data settings
                                dlg = new MessageDialog( "In order to collect and view visited places you need to enable Motion data in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                            }
                            else
                            {
                                dlg = new MessageDialog( "In order to collect and view visited places you need to enable 'Places visited' in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                            }
                            dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                            dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                            await dlg.ShowAsync();
                            new System.Threading.ManualResetEvent( false ).WaitOne( 500 );
                            return false;
                        }
                    case SenseError.GeneralFailure:
                        return false;
                    case SenseError.IncompatibleSDK:
                        MessageDialog dialog2 = new MessageDialog( "This application has become outdated. Please update to the latest version.", "Information" );
                        await dialog2.ShowAsync();
                        return false;
                    default:
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Set full screen map
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void FullScreenButton_Click( object sender, TappedRoutedEventArgs e )
        {
            _fullScreen = !_fullScreen;
            if( _fullScreen )
            {
                CmdBar.Visibility = Visibility.Collapsed;
                TopPanel.Visibility = Visibility.Collapsed;
                FullScreenButton.Symbol = Symbol.BackToWindow;
            }
            else
            {
                CmdBar.Visibility = Visibility.Visible;
                TopPanel.Visibility = Visibility.Visible;
                FullScreenButton.Symbol = Symbol.FullScreen;
            }
        }

        /// <summary>
        /// Retrieves the places history
        /// </summary>
        /// <returns>String of all places history</returns>
        private async Task<List<string>> GetPlacesHistoryAsync()
        {
            IList<string> resultStrFinal = new List<string>();
            IList<Place> result = null;
            if( _placeMonitor != null )
            {
                // Returns time ordered list of places visited during given time period
                await CallSensorcoreApiAsync( async () =>
                {
                    result = await _placeMonitor.GetPlaceHistoryAsync( DateTime.Today - TimeSpan.FromDays( 10 ), TimeSpan.FromDays( 10 ) );
                } );
                if( result != null )
                {
                    IEnumerable<Place> reverse = result.AsEnumerable().Reverse();
                    for( int i = 1; i < reverse.Count(); i++ )
                    {
                        string time = reverse.ElementAt( i ).Timestamp.ToString( "MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture );
                        var resultStr = "Place : " + reverse.ElementAt( i ).Kind.ToString() +
                            "\nTimestamp : " + time +
                            "\nLength Of Stay : " + reverse.ElementAt( i ).LengthOfStay.ToString() +
                            "\nTotal Length Of Stay : " + reverse.ElementAt( i ).TotalLengthOfStay.ToString() +
                            "\nTotal Visit Count : " + reverse.ElementAt( i ).TotalVisitCount.ToString();
                        resultStrFinal.Add( resultStr );
                    }
                }
            }
            // Returns a list with details of the places history
            return resultStrFinal.ToList();
        }
    }
}
