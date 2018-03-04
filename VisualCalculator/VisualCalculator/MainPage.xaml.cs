﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

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
        private bool _isPreviewing;        

        // Bitmap holder of currently loaded image.
        private SoftwareBitmap _bitmap;

        // Prevent the screen from sleeping while the camera is running
        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        // Global translation transform used for changing the position of 
        // the Rectangle based on input data from the touch contact.
        private TranslateTransform _translateTransform;

        // Provide low-level details for each touch contact, 
        // including pointer motion and the ability to distinguish press and release events.
        //private PointerEventHandler _pointerEventHandler;
          
        #region Constructor, lifecycle and navigation

        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Suspending += Application_Suspending;

            // Listener for the ManipulationDelta event.
            touchRectangle.ManipulationDelta += touchRectangle_ManipulationDelta;
            // New translation transform populated in 
            // the ManipulationDelta handler.
            _translateTransform = new TranslateTransform();
            // Apply the translation to the Rectangle.
            touchRectangle.RenderTransform = this._translateTransform;

            // Listener fot the PointerEvents event.
            touchRectangle.PointerPressed += touchRectangle_PointerPressed;
            touchRectangle.PointerMoved += touchRectangle_PointerMoved;
            touchRectangle.PointerReleased += touchRectangle_PointerReleased;
        }
        
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await InitializeCameraAsync();
                deferral.Complete();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitializeCameraAsync();
        }

        #endregion Constructor, lifecycle and navigation


        #region Event handlers

        private async void cameraButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Visibility change
            previewControl.Visibility = Visibility.Visible;
            photoButton.Visibility = Visibility.Visible;
            imageControl.Visibility = Visibility.Collapsed;

            await InitializeCameraAsync();            
        }

        private async void fileButton_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            var picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                FileTypeFilter = { ".jpg", ".jpeg", ".png" },
            };

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {                
                await CleanupCameraAsync();
                
                // Visibility change
                previewControl.Visibility = _isPreviewing ? Visibility.Collapsed : Visibility.Visible;
                photoButton.Visibility = Visibility.Collapsed;
                imageControl.Visibility = Visibility.Visible;

                await LoadImage(file);
                await CropImage();
            }
        }

        private async void photoButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await TakePhotoAsync();
            await CropImage();
        }

        // Handler for the ManipulationDelta event.
        // ManipulationDelta data is loaded into the
        // translation transform and applied to the Rectangle.
        void touchRectangle_ManipulationDelta(object sender,
            ManipulationDeltaRoutedEventArgs e)
        {
            // Move the rectangle.
            _translateTransform.X += e.Delta.Translation.X;
            _translateTransform.Y += e.Delta.Translation.Y;
        }

        private void touchRectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            
        }

        private void touchRectangle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Rectangle rect = sender as Rectangle;

            // Change the dimensions of the Rectangle.
            if (null != rect)
            {
                rect.Width *= 1.05;
                rect.Height *= 1.05;
            }
        }

        private void touchRectangle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            
        }

        #endregion Event handlers


        #region MediaCapture methods

        private async Task InitializeCameraAsync()
        {
            await CleanupCameraAsync();

            if (_mediaCapture == null)
            {
                await StartPreviewAsync();
            }            
        }

        /// <summary>
        /// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on
        /// </summary>
        /// <returns></returns>
        private async Task StartPreviewAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                Debug.WriteLine("The app was denied access to the camera");
                return;
            }

            try
            {
                previewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
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
                    previewControl.Source = null;
                    if (_displayRequest != null)
                    {
                        _displayRequest.RequestRelease();
                    }

                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });
            }
        }

        private async Task TakePhotoAsync()
        {            
            // Store the image
            var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
            var file = await myPictures.SaveFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);            

            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(captureStream);
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    var properties = new BitmapPropertySet {
                        { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) }
                    };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);

                    await encoder.FlushAsync();
                }                
            }

            // Display the captured image
            // Get information about the preview.
            var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            int videoFrameWidth = (int)previewProperties.Width;
            int videoFrameHeight = (int)previewProperties.Height;

            // Create the video frame to request a SoftwareBitmap preview frame.
            var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, videoFrameWidth, videoFrameHeight);

            // Capture the preview frame.
            using (var currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
            {
                // Collect the resulting frame.
                _bitmap = currentFrame.SoftwareBitmap;

                var imgSource = new WriteableBitmap(_bitmap.PixelWidth, _bitmap.PixelHeight);

                _bitmap.CopyToBuffer(imgSource.PixelBuffer);
                imageControl.Source = imgSource;
            }

            // Visibility change
            previewControl.Visibility = Visibility.Collapsed;
            photoButton.Visibility = Visibility.Collapsed;
            imageControl.Visibility = Visibility.Visible;
        }

        #endregion MediaCapture methods


        #region Helper functions

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                Debug.WriteLine("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !_isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        private async Task LoadImage(StorageFile file)
        {
            using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);

                _bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                var imgSource = new WriteableBitmap(_bitmap.PixelWidth, _bitmap.PixelHeight);

                _bitmap.CopyToBuffer(imgSource.PixelBuffer);
                imageControl.Source = imgSource;
            }
        }

        private async Task CropImage()
        {
            Debug.WriteLine("croping");
        }



        #endregion Helper functions        


    }
}