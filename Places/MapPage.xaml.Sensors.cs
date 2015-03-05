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

/// <summary>
/// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace Places
{
    /// <summary>
    /// All the logic related to Sensorcore and mapping
    /// Known issues : App does not remove geofences if location of a known place changes
    /// </summary>
    public sealed partial class MapPage
    {
        #region Private constants
        /// <summary>
        /// Home color mapping
        /// </summary>
        private readonly Color PlacesColorHome = Color.FromArgb(AlphaLevelOfPlacesCircle, 31, 127, 31);

        /// <summary>
        /// holds all the pushpins in the map
        /// </summary>
        List<PushPin> pins = null;

        /// <summary>
        /// Work color mapping
        /// </summary>
        private readonly Color PlacesColorWork = Color.FromArgb(AlphaLevelOfPlacesCircle, 255, 163, 31);

        /// <summary>
        /// Frequent color mapping
        /// </summary>
        private readonly Color PlacesColorFrequent = Color.FromArgb(AlphaLevelOfPlacesCircle, 127, 127, 255);

        /// <summary>
        /// Known color mapping
        /// </summary>
        private readonly Color PlacesColorKnown = Color.FromArgb(AlphaLevelOfPlacesCircle, 255, 127, 127);

        /// <summary>
        /// Unknown color mapping
        /// </summary>
        private readonly Color PlacesColorUnknown = Color.FromArgb(AlphaLevelOfPlacesCircle, 127, 127, 127);

        /// <summary>
        /// The alpha component from Color structure is set to a specified value
        /// Valid values are 0 through 255.
        /// </summary>
        private const int AlphaLevelOfPlacesCircle = 127;

        /// <summary>
        /// Mean radius of planet Earth
        /// </summary>
        const int MEAN_RADIUS = 6371;

        /// <summary>
        /// Conversion from one radian to degrees 
        /// </summary>
        private const double RadianToDegrees = 180.0 / Math.PI;

        /// <summary>
        /// Conversion from one degree to radian
        /// </summary>
        private const double DegreesToRadian = Math.PI / 180;

        /// <summary>
        /// Length of the earth's semi-major axis A
        /// </summary
        private const double Wgs84A = 6378137.0;

        /// <summary>
        /// Length of the earth's semi-major axis B
        /// </summary
        private const double Wgs84B = 6356752.3;
        #endregion

        #region Private members
        /// <summary>
        ///  The application model
        /// </summary>
        private Places.App _app = Application.Current as Places.App;

        /// <summary>
        /// Place monitor instance
        /// </summary>
        public Monitor _placeMonitor;

        /// <summary>
        /// check to see launching finished or not
        /// </summary>
        private bool iLaunched = false;

        /// <summary>
        /// Geolocator instance
        /// </summary>
        private Geolocator _geoLocator;

        /// <summary>
        /// List of activities for a place
        /// </summary>
        List<Activity> activitiesToShow = new List<Activity>();

        /// <summary>
        /// CancellationToken Source instance
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Information about access to a device
        /// </summary>
        private DeviceAccessInformation _accessInfo;

        /// <summary>
        /// Current location
        /// </summary>
        private Geopoint _currentLocation;

        /// <summary>
        ///  Full screen mode for map control
        /// </summary>
        private bool _fullScreen;
        #endregion

        /// <summary>
        /// Initialize SensorCore and find the current position
        /// </summary>
        private async Task InitCore()
        {
            try
            {
                // Following code assumes that device has new software(SensorCoreSDK1.1 based)
                if (!(await PlaceMonitor.IsSupportedAsync()))
                {
                    MessageDialog dlg = new MessageDialog("Unfortunately this device does not support viewing visited places");
                    await dlg.ShowAsync();
                    Application.Current.Exit();
                }
                else
                {
                    uint apiSet = await SenseHelper.GetSupportedApiSetAsync();
                    MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                    if (!settings.LocationEnabled)
                    {
                        MessageDialog dlg = new MessageDialog("In order to collect and view visited places you need to enable location in system settings. Do you want to open settings now? if no, applicatoin will exit", "Information");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { Application.Current.Exit(); })));
                        await dlg.ShowAsync();
                    }
                    if (!settings.PlacesVisited)
                    {
                        MessageDialog dlg = null;
                        if (settings.Version < 2)
                        {
                            // device which has old motion data settings.
                            // this is equal to motion data settings on/off in old system settings(SDK1.0 based)
                            dlg = new MessageDialog("In order to collect and view visited places you need to enable Motion data in Motion data settings. Do you want to open settings now? if no, application will exit", "Information");
                        }
                        else
                        {
                            dlg = new MessageDialog("In order to collect and view visited places you need to 'enable Places visited' and 'DataQuality to detailed' in Motion data settings. Do you want to open settings now? if no, application will exit", "Information");
                        }
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) =>{ Application.Current.Exit(); })));
                        await dlg.ShowAsync();
                    }
                }
            }
            catch (Exception)
            {
            }
            // This is not implemented by the simulator, uncomment for the PlaceMonitor                      
            if (_placeMonitor == null)
            {
                // Init SensorCore
                if (await CallSensorcoreApiAsync(async () => { _placeMonitor = await Monitor.GetDefaultAsync(); }))
                {
                    Debug.WriteLine("PlaceMonitor initialized.");
                    // Update list of known places
                    await UpdateKnownPlacesAsync();
                    HomeButton.IsEnabled = true;
                    WorkButton.IsEnabled = true;
                    FrequentButton.IsEnabled = true;
                    CurrentButton.IsEnabled = true;
                    await ActivityReader.Instance().Initialize();
                }
                else return;
            }
            else
            {
                var loader = new ResourceLoader();
                MessageDialog dialog = new MessageDialog(loader.GetString("NoMotionDataSupport/Text"), loader.GetString("Information/Text"));
                dialog.Commands.Add(new UICommand(loader.GetString("OkButton/Text")));
                await dialog.ShowAsync();
                new System.Threading.ManualResetEvent(false).WaitOne(500);
                Application.Current.Exit();
            }
            // Init Geolocator
            try
            {
                _accessInfo = DeviceAccessInformation.CreateFromDeviceClass(DeviceClass.Location);
                _accessInfo.AccessChanged += OnAccessChanged;
                // Get a geolocator object
                _geoLocator = new Geolocator();
                // Get cancellation token
                _cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = _cancellationTokenSource.Token;
                Geoposition geoposition = await _geoLocator.GetGeopositionAsync().AsTask(token);
                _currentLocation = new Geopoint(new BasicGeoposition()
                {
                    Latitude = geoposition.Coordinate.Point.Position.Latitude,
                    Longitude = geoposition.Coordinate.Point.Position.Longitude
                });
                // Focus on the current location
                OnCurrentClicked(this, null);
            }
            catch (UnauthorizedAccessException)
            {
                if (DeviceAccessStatus.DeniedByUser == _accessInfo.CurrentStatus)
                {
                    Debug.WriteLine("Location has been disabled by the user. Enable access through the settings charm.");
                }
                else if (DeviceAccessStatus.DeniedBySystem == _accessInfo.CurrentStatus)
                {
                    Debug.WriteLine("Location has been disabled by the system. The administrator of the device must enable location access through the location control panel.");
                }
                else if (DeviceAccessStatus.Unspecified == _accessInfo.CurrentStatus)
                {
                    Debug.WriteLine("Location has been disabled by unspecified source. The administrator of the device may need to enable location access through the location control panel, then enable access through the settings charm.");
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // The application does not have the right capability or the location master switch is off
                    Debug.WriteLine("location is disabled in phone settings.");
                }
            }
            finally
            {
                _cancellationTokenSource = null;
            }
            // Activate and deactivate the SensorCore when the visibility of the app changes
            Window.Current.VisibilityChanged += async (oo, ee) =>
            {
                if (_placeMonitor != null)
                {
                    if (!ee.Visible)
                    {
                        await CallSensorcoreApiAsync(async () => { await _placeMonitor.DeactivateAsync(); });
                    }
                    else
                    {
                        await CallSensorcoreApiAsync(async () => { await _placeMonitor.ActivateAsync(); });
                    }
                }
            };
        }

        /// <summary>
        /// Error handling for device level error conditions
        /// </summary>
        /// <param name="sender">Information about access to a device</param>
        /// <param name="args">AccessChanged event to listen for access changes</param>
        public async void OnAccessChanged(DeviceAccessInformation sender, DeviceAccessChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (DeviceAccessStatus.DeniedByUser == args.Status)
                {
                    Debug.WriteLine("Location has been disabled by the user. Enable access through the settings charm.");
                }
                else if (DeviceAccessStatus.DeniedBySystem == args.Status)
                {
                    Debug.WriteLine("Location has been disabled by the system. The administrator of the device must enable location access through the location control panel.");
                }
                else if (DeviceAccessStatus.Unspecified == args.Status)
                {
                    Debug.WriteLine("Location has been disabled by unspecified source. The administrator of the device may need to enable location access through the location control panel, then enable access through the settings charm.");
                }
                else if (DeviceAccessStatus.Allowed == args.Status)
                {
                }
                else
                {
                    Debug.WriteLine("Unknown device access information status");
                }
            });
        }

        /// <summary>
        /// Create a geo-fence around certain geopoint with a given radius
        /// </summary>
        /// <param name="fenceKey"The key of the fence></param>
        /// <param name="latitude">Latitude of the place</param>
        /// <param name="longitude">Longitude of the place</param>
        /// <param name="radius">Radius of the place</param>
        private void CreateGeofence(string fenceKey, double latitude, double longitude, double radius, Color fenceColor)
        {
            if (GeofenceMonitor.Current.Geofences.All(v => v.Id != fenceKey))
            {
                var position = new BasicGeoposition { Latitude = latitude, Longitude = longitude, Altitude = 0.0 };
                CreateCircle(position, radius, fenceColor);
                // The geofence is a circular region
                var geocircle = new Geocircle(position, radius);
                // Listen for enter geofence and exit geofence events
                MonitoredGeofenceStates mask = 0;
                mask |= MonitoredGeofenceStates.Entered;
                mask |= MonitoredGeofenceStates.Exited;
                var geofence = new Geofence(fenceKey, geocircle, mask, false);
                GeofenceMonitor.Current.Geofences.Add(geofence);
                GeofenceMonitor.Current.StatusChanged += EnterFence;
            }
            else
            {
                foreach (var f in GeofenceMonitor.Current.Geofences.Where(f => f.Id == fenceKey))
                {
                    var x = f.Geoshape as Geocircle;
                    if (x != null)
                    {
                        CreateCircle(x.Center, x.Radius, fenceColor);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Inform the user about when entering or exiting the geofence
        /// </summary>
        /// <param name="sender">Geofence Monitor</param>
        /// <param name="e">Event arguments</param>
        private async void EnterFence(GeofenceMonitor sender, object e)
        {
            var reports = sender.ReadReports();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                foreach (GeofenceStateChangeReport report in reports)
                {
                    GeofenceState state = report.NewState;
                    Geofence geofence = report.Geofence;
                    if (state == GeofenceState.Removed)
                    {
                        GeofenceMonitor.Current.Geofences.Remove(geofence);
                    }
                    else if (state == GeofenceState.Entered)
                    {
                        foreach (Place place in _app.Places)
                        {
                            if (place.Id.ToString() == geofence.Id)
                            {
                                var dia = new MessageDialog("You have entered - " + place.Kind.ToString());
                                await dia.ShowAsync();
                            }
                        }
                    }
                    else if (state == GeofenceState.Exited)
                    {
                        foreach (Place place in _app.Places)
                        {
                            if (place.Id.ToString() == geofence.Id)
                            {
                                var dia = new MessageDialog("You have left - " + place.Kind.ToString());
                                await dia.ShowAsync();
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Create a circle icon to the map
        /// </summary>
        /// <param name="position">Position for the location</param>
        /// <param name="radius">Radius of the circles drawn around the places indicates the uncertainty of the exact location</param>
        /// <param name="circleColor">The color of the circle based on the place type</param>
        private void CreateCircle(BasicGeoposition position, double radius, Color circleColor)
        {
            var circle = new MapPolygon
            {
                FillColor = circleColor,
                StrokeColor = circleColor,
                StrokeDashed = false,
                StrokeThickness = 1,
                Path = new Geopath(CreateGeoCircle(position, radius)),
            };
            PlacesMap.MapElements.Add(circle);
        }

        /// <summary>   
        /// Calculate a circular area around certain geoposition with a given radius
        /// </summary>
        /// <param name="center">The center of the geoposition</param>
        /// <param name="radius">Radius of the circles drawn around the places indicates the uncertainty of the exact location<</param>
        /// <returns>List of points</returns>
        public static List<BasicGeoposition> CreateGeoCircle(BasicGeoposition center, double radiusValue, int numberOfPoints = 360)
        {
            var locations = new List<BasicGeoposition>();
            var radius = radiusValue / 1000;
            double latCenter = center.Latitude * DegreesToRadian;
            double lonCenter = center.Longitude * DegreesToRadian;
            double distance = radius / MEAN_RADIUS;
            for (int i = 0; i <= numberOfPoints; i++)
            {
                double angle = i * DegreesToRadian;
                var latitude = Math.Asin(Math.Sin(latCenter) * Math.Cos(distance) + Math.Cos(latCenter) * Math.Sin(distance) * Math.Cos(angle));
                var longitude = ((lonCenter + Math.Atan2(Math.Sin(angle) * Math.Sin(distance) * Math.Cos(latCenter), Math.Cos(distance) - Math.Sin(latCenter) * Math.Sin(latitude)) + Math.PI) % (Math.PI * 2)) - Math.PI;
                locations.Add(new BasicGeoposition { Latitude = latitude * RadianToDegrees, Longitude = longitude * RadianToDegrees });
            }
            return locations;
        }

        /// <summary>
        /// Updates list of known places
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task UpdateKnownPlacesAsync()
        {
            if (_placeMonitor != null)
            {
                _app.Places = null;
                PlacesMap.MapElements.Clear();
                PlacesMap.Children.Clear();
                if (await CallSensorcoreApiAsync(async () =>
                { // Get selected day places, else all places
                    if (_selectedDay != null && !_selectedDay.Name.Equals("All", StringComparison.CurrentCultureIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine("GetPlace: " + _selectedDay.Name);
                        _app.Places = await _placeMonitor.GetPlaceHistoryAsync(_selectedDay.Day, TimeSpan.FromHours(24));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Places: Get all known places");
                        _app.Places = await _placeMonitor.GetKnownPlacesAsync();
                    }
                }))
                {
                    // Make sure that there were Places for the timespan
                    if (_app.Places != null && _app.Places.Count > 0)
                    {
                        PlacesMap.Children.Clear();
                        if (pins != null)
                        {
                            pins.Clear();
                            pins = null;
                        }
                        pins = new List<PushPin>();
                        int i = 0;
                        foreach (var p in _app.Places)
                        {
                            System.Diagnostics.Debug.WriteLine("Place {0} radius {1} Latitude {2} Longitude {3} Total visit count {4} Total length of stay {5} Length of stay {6} Kind {7} ", p.Kind, p.Radius, p.Position.Latitude, p.Position.Longitude, p.TotalVisitCount, p.TotalLengthOfStay, p.LengthOfStay, p.Kind);
                            pins.Add(new PushPin(p, p.Kind.ToString()));
                            MapControl.SetLocation(pins[i], new Geopoint(p.Position));
                            MapControl.SetNormalizedAnchorPoint(pins[i], new Point(0.15, 1));
                            PlacesMap.Children.Add(pins[i]);
                            i = i + 1;
                            Color placesColor;
                            //Set for each type of a place a certain color and set custom image for the MapIcon
                            switch (p.Kind)
                            {
                                case PlaceKind.Home:
                                    placesColor = PlacesColorHome;
                                    break;
                                case PlaceKind.Work:
                                    placesColor = PlacesColorWork;
                                    break;
                                case PlaceKind.Frequent:
                                    placesColor = PlacesColorFrequent;
                                    break;
                                case PlaceKind.Known:
                                    placesColor = PlacesColorKnown;
                                    break;
                                default:
                                    placesColor = PlacesColorUnknown;
                                    break;
                            }
                            CreateGeofence(p.Id.ToString(), p.Position.Latitude, p.Position.Longitude, p.Radius, placesColor);
                        }
                    }
                }
            }
            Debug.WriteLine("Known places updated.");
        }

        /// <summary>
        /// Performs asynchronous Sensorcore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">The function delegate to execute asynchronously when one task in the tasks completes.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public async Task<bool> CallSensorcoreApiAsync(Func<Task> action)
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch (Exception e)
            {
                failure = e;
            }
            if (failure != null)
            {
                switch (SenseHelper.GetSenseError(failure.HResult))
                {
                    case SenseError.LocationDisabled:
                    case SenseError.SenseDisabled:
                        if (!_app.SensorCoreActivationStatus.onGoing)
                        {
                            this.Frame.Navigate(typeof(ActivateSensorCore));
                        }
                        return false;
                    case SenseError.GeneralFailure:
                        return false;
                    case SenseError.IncompatibleSDK:
                        MessageDialog dialog2 = new MessageDialog("This application has become outdated. Please update to the latest version.", "Information");
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
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Provides event data for the Tapped event.</param>
        private void FullScreeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _fullScreen = !_fullScreen;
            if (_fullScreen)
            {
                CmdBar.Visibility = Visibility.Collapsed;
                TopPanel.Visibility = Visibility.Collapsed;
                FullScreeButton.Symbol = Symbol.BackToWindow;
            }
            else
            {
                CmdBar.Visibility = Visibility.Visible;
                TopPanel.Visibility = Visibility.Visible;
                FullScreeButton.Symbol = Symbol.FullScreen;
            }
        }
    }
}
