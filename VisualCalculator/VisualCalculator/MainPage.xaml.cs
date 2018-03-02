using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VisualCalculator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // MediaCapture and its state variables
        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;

        // Prevent the screen from sleeping while the camera is running
        private readonly DisplayRequest _displayRequest = new DisplayRequest();



        #region Constructor, lifecycle and navigation

        public MainPage()
        {
            this.InitializeComponent();

            // initialise and clean up the camera
            Application.Current.Suspending += ApplicationSuspendingAsync;
            Application.Current.Resuming += ApplicationResumingAsync;
        }

        // Cleanup process starts when the application is suspended
        private async void ApplicationSuspendingAsync(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                //await CleanupUiAsync();

                await CleanupCameraAsync();

                deferral.Complete();
            }
        }

        // Setup process starts when the application is resuming
        private async void ApplicationResumingAsync(object sender, object e)
        {
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                //await SetupUIAsync();

                await SetupCameraAsync();
            }
        }

        // Called when a page becomes the active page in a frame.
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //await SetupUIAsync();

            await SetupCameraAsync();
        }

        // Called when a page is no longer the active page in a frame.
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {

            //await CleanupUiAsync();

            await CleanupCameraAsync();
        }

        #endregion Constructor, lifecycle and navigation



        #region MediaCapture functions

        private async Task SetupCameraAsync()
        {
            Debug.WriteLine("InitializeCameraAsync");

            if (_mediaCapture == null)
            {
                // Initialize MediaCapture
                try
                {
                    // Create MediaCapture and its settings
                    _mediaCapture = new MediaCapture();

                    // Register for a notification when something goes wrong
                    _mediaCapture.Failed += MediaCapture_Failed;

                    await _mediaCapture.InitializeAsync();

                    _displayRequest.RequestActive();
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("The app was denied access to the camera");
                }

                try
                {
                    PreviewControl.Source = _mediaCapture;
                    await _mediaCapture.StartPreviewAsync();
                    _isPreviewing = true;
                }
                catch (System.IO.FileLoadException)
                {
                    _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
                }

            }
        }

        private async Task CleanupCameraAsync()
        {
            if (_mediaCapture != null)
            {
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (_displayRequest != null)
                    {
                        _displayRequest.RequestRelease();
                    }

                    // Deregister for a notification when something goes wrong
                    _mediaCapture.Failed += MediaCapture_Failed;

                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });
            }
        }

        #endregion MediaCapture functions



        #region Ui functions

        #endregion Ui functions



        #region EventHandler functions

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            // Handle failed media capturing
            // Cleanup camera async
            // Call dispatcher
            Debug.WriteLine("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message);
        }

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                Debug.WriteLine("The camera preview can't be displayed because another app has exclusive access");
                //ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !_isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await SetupCameraAsync();
                });
            }
        }

        #endregion EventHandler functions

    }
}
