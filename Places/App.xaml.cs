/*	
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
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Lumia.Sense;
using Places.Common;

/// <summary>
/// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace Places
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        #region Private members
        /// <summary>
        /// Instance of the TransitionCollection class
        /// </summary>
        private TransitionCollection transitions;

        /// <summary>
        /// Status of SensorCore
        /// </summary>
        private ActivateSensorCoreStatus _sensorCoreActivationStatus = new ActivateSensorCoreStatus();
        #endregion

        /// <summary>
        /// Gets or sets the status of SensorCore
        /// </summary>
        public ActivateSensorCoreStatus SensorCoreActivationStatus
        {
            get
            {
                return _sensorCoreActivationStatus;
            }
            private set
            {
                _sensorCoreActivationStatus = value;
            }
        }

        /// <summary>
        /// List of known places
        /// </summary>
        public IList<Place> Places
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
            HardwareButtons.BackPressed += this.HardwareButtons_BackPressed;
        }

        /// <summary>
        /// This event wraps HardwareButtons.BackPressed to allow other pages to override
        /// the default behavior by subscribing to this event and potentially
        /// handling the back button press a different way (e.g. dismissing dialogs).
        /// </summary>
        public event EventHandler<BackPressedEventArgs> BackPressed;

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active.
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page.
                rootFrame = new Frame();
                // Associate the frame with a SuspensionManager key.
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                //Change this value to a cache size that is appropriate for your application.
                rootFrame.CacheSize = 1;
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate.
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        // Something went wrong restoring state.
                        // Assume there is no state and continue.
                    }
                }
                // Place the frame in the current Window.
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }
                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter.
                if (!rootFrame.Navigate(typeof(MapPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active.
            Window.Current.Activate();
        }

        /// <summary>
        /// Restores the content transitions after the app has launched
        /// </summary>
        /// <param name="sender"The source of the event.></param>
        /// <param name="e">Event arguments</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Handles the back button press and navigates through the history of the root frame.
        /// </summary>
        /// <param name="sender">The source of the event. <see cref="HardwareButtons"/></param>
        /// <param name="e">Details about the back button press.</param>
        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }
            var handler = this.BackPressed;
            if (handler != null)
            {
                handler(sender, e);
            }
            if (frame.CanGoBack && !e.Handled)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
