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
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Places.Utilities;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Geolocation.Geofencing;
using System.Collections.Generic;

using Monitor = Lumia.Sense.PlaceMonitor;
//using Monitor = Lumia.Sense.Testing.PlaceMonitorSimulator;

namespace Places
{
    /// <summary>
    /// All the logic related to Sensorcore and mapping
    /// Known issues : App does not remove geofences if location of a known place changes
    /// </summary>
    public sealed partial class MapPage
    {
        // Constants
        private readonly Color PlacesColorHome = Color.FromArgb(AlphaLevelOfPlacesCircle, 31, 127, 31);
        private readonly Color PlacesColorWork = Color.FromArgb(AlphaLevelOfPlacesCircle, 255, 163, 31);
        private readonly Color PlacesColorFrequent = Color.FromArgb(AlphaLevelOfPlacesCircle, 127, 127, 255);
        private readonly Color PlacesColorKnown = Color.FromArgb(AlphaLevelOfPlacesCircle, 255, 127, 127);
        private readonly Color PlacesColorUnknown = Color.FromArgb(AlphaLevelOfPlacesCircle, 127, 127, 127);
        private const int AlphaLevelOfPlacesCircle = 127;
        private const double RadianToDegrees = 180.0 / Math.PI;
        private const double Wgs84A = 6378137.0; // Length of the earth's semi-major axis
        private const double Wgs84B = 6356752.3;

        private Places.App _app = Application.Current as Places.App;
        private Monitor _monitor;
        private Geolocator _geolocator;
        private CancellationTokenSource _cancellationTokenSource;
        private DeviceAccessInformation _accessInfo;
        private Geopoint _currentLocation;
        private bool _fullScreen;

        /// <summary>
        /// Initialize SensorCore and find the current position
        /// </summary>
        private async void InitCore()
        {
            if (_monitor == null)
            {
                // This is not implemented by the simulator, uncomment for the PlaceMonitor
                if (await Monitor.IsSupportedAsync())
                {
                    // Init SensorCore
                    if (await CallSensorcoreApiAsync(async () => { _monitor = await Monitor.GetDefaultAsync(); }))
                    {
                        Debug.WriteLine("PlaceMonitor initialized.");

                        // Update list of known places
                        await UpdateKnownPlacesAsync();
                        HomeButton.IsEnabled = true;
                        WorkButton.IsEnabled = true;
                        FrequentButton.IsEnabled = true;
                        CurrentButton.IsEnabled = true;
                    }
                    else return;
                }
                else
                {
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
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
                    _geolocator = new Geolocator();
                    // Get cancellation token
                    _cancellationTokenSource = new CancellationTokenSource();
                    CancellationToken token = _cancellationTokenSource.Token;
                    Geoposition geoposition = await _geolocator.GetGeopositionAsync().AsTask(token);

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
                catch (TaskCanceledException)
                {
                    // task cancelled
                }
                catch (Exception ex)
                {
                    if ((uint)ex.HResult == 0x80004004)
                    {
                        // the application does not have the right capability or the location master switch is off
                        Debug.WriteLine("location is disabled in phone settings.");
                    }
                }
                finally
                {
                    _cancellationTokenSource = null;
                }
            }
            // Activate and deactivate the SensorCore when the visibility of the app changes
            Window.Current.VisibilityChanged += async (oo, ee) =>
            {
                if (_monitor != null)
                {
                    if (!ee.Visible)
                    {
                        await CallSensorcoreApiAsync(async () => { await _monitor.DeactivateAsync(); });
                    }
                    else
                    {
                        await CallSensorcoreApiAsync(async () => { await _monitor.ActivateAsync(); });
                    }
                }
            };
        }

        /// <summary>
        /// Error handling for device level error conditions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
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
        /// <param name="fenceKey"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="radius"></param>
        private void CreateGeofence(string fenceKey, double latitude, double longitude, double radius, Color fenceColor)
        {
            if (GeofenceMonitor.Current.Geofences.All(v => v.Id != fenceKey))
            {
                var position = new BasicGeoposition {Latitude = latitude, Longitude = longitude, Altitude = 0.0};
                CreateCircle(position, radius, fenceColor);

                // the geofence is a circular region
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="circleColor"></param>
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
        /// Mathematical implementation based on http://dotnetbyexample.blogspot.fi/2014/02/drawing-circle-shapes-on-windows-phone.html
        /// by Joost van Schaik
        /// </summary>
        /// <returns></returns>
        public static List<BasicGeoposition> CreateGeoCircle(BasicGeoposition center, double radius)
        {
            var locations = new List<BasicGeoposition>();

            // http://en.wikipedia.org/wiki/Earth_radius
            var an = Wgs84A * Wgs84A * Math.Cos(center.Latitude);
            var bn = Wgs84B * Wgs84B * Math.Sin(center.Latitude);
            var ad = Wgs84A * Math.Cos(center.Latitude);
            var bd = Wgs84B * Math.Sin(center.Latitude);
            var earthRadius = Math.Sqrt((an * an + bn * bn) / (ad * ad + bd * bd));

            for (int i = 0; i <= 360; i++)
            {
                double latCenter = center.Latitude * (Math.PI / 180);
                double lonCenter = center.Longitude * (Math.PI / 180);
                double distance = radius / earthRadius;
                double course = i * Math.PI / 180;

                var latitude = Math.Asin(Math.Sin(latCenter) * Math.Cos(distance) + Math.Cos(latCenter) * Math.Sin(distance) * Math.Cos(course));
                var longitude = ((lonCenter + Math.Atan2(Math.Sin(course) * Math.Sin(distance) * Math.Cos(latCenter), Math.Cos(distance) - Math.Sin(latCenter) * Math.Sin(latitude)) + Math.PI) % (Math.PI * 2)) - Math.PI;

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
            if (_monitor != null)
            {
                _app.Places = null;

                PlacesMap.MapElements.Clear();
    
                if (await CallSensorcoreApiAsync(async () => { _app.Places = await _monitor.GetKnownPlacesAsync();}))
                {
                    foreach (var p in _app.Places)
                    {
                        System.Diagnostics.Debug.WriteLine("Place {0} radius {1} Latitude {2} Longitude {3} ",p.Kind, p.Radius, p.Position.Latitude, p.Position.Longitude);
                        var icon = new MapIcon();
                        MapExtensions.SetValue(icon, p);

                        icon.NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1);
                        icon.Location = new Geopoint(p.Position);
                        icon.Title = p.Kind.ToString();
                        Color color;

                        switch (p.Kind)
                        {
                            case PlaceKind.Home:
                                color = PlacesColorHome;
                                break;
                            case PlaceKind.Work:
                                color = PlacesColorWork;
                                break;
                            case PlaceKind.Frequent:
                                color = PlacesColorFrequent;
                                break;
                            case PlaceKind.Known:
                                color = PlacesColorKnown;
                                break;
                            default:
                                color = PlacesColorUnknown;
                                break;
                        }

                        PlacesMap.MapElements.Add(icon);
                        CreateGeofence(p.Id.ToString(), p.Position.Latitude, p.Position.Longitude, p.Radius, color);
                    }
                }
            }
            Debug.WriteLine("Known places updated.");
        }

        /// <summary>
        /// Performs asynchronous Sensorcore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action"></param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        private async Task<bool> CallSensorcoreApiAsync(Func<Task> action)
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
                        if (!_app.SensorCoreActivationStatus.Ongoing)
                        {
                            this.Frame.Navigate(typeof(ActivateSensorCore));
                        }

                        return false;
                    default:
                        MessageDialog dialog = new MessageDialog("Failure: " + SenseHelper.GetSenseError(failure.HResult) + " while initializing Motion data. Application will exit.", "");
                        await dialog.ShowAsync();
                        return false;
                }
            }

            return true;
        }

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
