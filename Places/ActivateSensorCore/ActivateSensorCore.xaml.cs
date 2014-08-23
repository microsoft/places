using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Lumia.Sense;
using Windows.Storage;
using System.Threading.Tasks;


namespace Places
{
    /// <summary>
    /// A Page that guides the users on their SensorCore activation.
    /// </summary>
    /// 
    public sealed partial class ActivateSensorCore : Page
    {

        private ActivateSensorCoreStatus sensorCoreActivationStatus;

        public ActivateSensorCore()
        {
            InitializeComponent();
            Loaded += ActivateSensorCore_Loaded;
            Window.Current.VisibilityChanged += Current_VisibilityChanged;
            
            var app = Application.Current as Places.App; // The application model
            sensorCoreActivationStatus = app.sensorCoreActivationStatus;
        }


        async void ActivateSensorCore_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateDialog();
        }

        async Task UpdateDialog()
        {
            MotionDataActivationBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            LocationActivationBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            sensorCoreActivationStatus.ActivationRequestResult = ActivationRequestResults.AskMeLater;

            Exception failure = null;
            try
            {
                await Lumia.Sense.PlaceMonitor.GetDefaultAsync();
            }
            catch (Exception exception)
            {
                switch (SenseHelper.GetSenseError(exception.HResult))
                {
                    case SenseError.LocationDisabled:
                        LocationActivationBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                    case SenseError.SenseDisabled:
                        MotionDataActivationBox.Visibility = Windows.UI.Xaml.Visibility.Visible;  
                        break;
                    default:
                        // do something clever here
                        break;
                }

                failure = exception;
            }
            if (failure == null)
            {
                // All is good now, dismiss the dialog.

                sensorCoreActivationStatus.ActivationRequestResult = ActivationRequestResults.AllEnabled;
                this.Frame.GoBack();
            }
        }
     

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                sensorCoreActivationStatus.Ongoing = true;
            }
        }

        async void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (e.Visible)
            {
                await UpdateDialog();
            }
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            sensorCoreActivationStatus.ActivationRequestResult = ActivationRequestResults.AskMeLater;
            this.Frame.GoBack();
        }

        private void NeverButton_Click(object sender, RoutedEventArgs e)
        {
            sensorCoreActivationStatus.ActivationRequestResult = ActivationRequestResults.NoAndDontAskAgain;
            this.Frame.GoBack();
        }


        private async void MotionDataActivationButton_Click(object sender, RoutedEventArgs e)
        {
            await SenseHelper.LaunchSenseSettingsAsync();
            // Although asynchoneous, this completes before the user has actually done anything.
            // The application will loose control, the system settings will be displayed.
            // We will get the control back to our application via a visibilityChanged event.
        }

        private async void LocationActivationButton_Click(object sender, RoutedEventArgs e)
        {
            await SenseHelper.LaunchLocationSettingsAsync();
            // Although asynchoneous, this completes before the user has actually done anything.
            // The application will loose control, the system settings will be displayed.
            // We will get the control back to our application via a visibilityChanged event.
        }
    }
}
