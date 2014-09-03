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
        private bool updatingDialog = false;

        public ActivateSensorCore()
        {
            InitializeComponent();
            
            var app = Application.Current as Places.App; // The application model
            sensorCoreActivationStatus = app.sensorCoreActivationStatus;
        }


        async Task UpdateDialog()
        {
            if (updatingDialog || (sensorCoreActivationStatus.ActivationRequestResult != ActivationRequestResults.NotAvailableYet))
            {
                 return;
            }
            updatingDialog = true;
            MotionDataActivationBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            LocationActivationBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            Exception failure = null;
            try
            {
                // GetDefaultAsync will throw if MotionData is disabled  
                PlaceMonitor monitor = await Lumia.Sense.PlaceMonitor.GetDefaultAsync();

                // But confirm that MotionData is really enabled by calling ActivateAsync,
                // to cover the case where the MotionData has been disabled after the app has been launched.
                await monitor.ActivateAsync(); 
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
            updatingDialog = false;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                Window.Current.VisibilityChanged += Current_VisibilityChanged;
                sensorCoreActivationStatus.Ongoing = true;
                sensorCoreActivationStatus.ActivationRequestResult = ActivationRequestResults.NotAvailableYet;
            }
            await UpdateDialog();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                Window.Current.VisibilityChanged -= Current_VisibilityChanged;
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
