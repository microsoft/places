using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Lumia.Sense;
using Places.Common;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Linq;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Places
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MapPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private int chosenFrequentId = -1;

        public MapPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            InitCore();

            PlacesMap.MapServiceToken = "xxx";
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
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
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private void OnTapped(MapControl sender, MapInputEventArgs args)
        {
            try
            {
                var elementList = PlacesMap.FindMapElementsAtOffset(args.Position);
                foreach (var element in elementList)
                {
                    var icon = element as MapIcon;

                    if (icon != null)
                    {
                        this.Frame.Navigate(typeof(PivotPage), icon);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

        private async void OnHomeClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            bool found = false;
            if(app.places != null)
                foreach (Place place in app.places)
                {
                    if (place.Kind == PlaceKind.Home)
                    {
                        PlacesMap.Center = new Geopoint(place.Position);
                        PlacesMap.ZoomLevel = 16;
                        found = true;
                        break;
                    }
                }

            // It takes some time for SensorCore SDK to figure out your known locations
            if (!found)
            {
                var loader = new ResourceLoader();
                var text = loader.GetString("HomeNotFound");
                var header = loader.GetString("LocationNotDefined");
                var dialog = new MessageDialog(text, header);
                
                await dialog.ShowAsync();
            }
        }

        private async void OnWorkClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            bool found = false;

            foreach (Place place in app.places)
            {
                if (place.Kind == PlaceKind.Work)
                {
                    PlacesMap.Center = new Geopoint(place.Position);
                    PlacesMap.ZoomLevel = 16;
                    found = true;
                    break;
                }
            }
            // It takes some time for SensorCore SDK to figure out your known locations
            if (!found)
            {
                var loader = new ResourceLoader();
                var text = loader.GetString("WorkNotFound");
                var header = loader.GetString("LocationNotDefined");
                var dialog = new MessageDialog(text, header);

                await dialog.ShowAsync();
            }
        }

        private void OnCurrentClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            PlacesMap.Center = currentLocation;
            PlacesMap.ZoomLevel = 16;
        }

        private void OnFrequentClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var notHomeNorWork =
                from place in app.places
                where place.Kind != PlaceKind.Home && place.Kind != PlaceKind.Work
                orderby place.Kind descending
                select place;

            if (notHomeNorWork.Count() == 0)
            {
                return;
            }

            chosenFrequentId++;
            if (chosenFrequentId >= notHomeNorWork.Count())
            {
                chosenFrequentId = 0;
            }

            PlacesMap.Center = new Geopoint(notHomeNorWork.ElementAt(chosenFrequentId).Position);
            PlacesMap.ZoomLevel = 16;
        }

        private void OnAboutClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }
    }
}
