using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
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
using Windows.UI.Input;
using Windows.UI.ViewManagement;
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
    public static class Constants
    {
        public const double ELLIPSE_RADIIUS = 10;

    }

    public struct Coordinate
    {
        public double xPos;
        public double yPos;       
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Azure Storage Account and Key                
        private static readonly StorageCredentials _credentials = new StorageCredentials("objectdetection9a6d", "+8s9aGusj+5w5iBnXCdqE/OGV3qhLZZFfTrkZxVh+/hEX4cBEX9lRcywiY/q2O1BUuDIqtXQ5YrIV1og6JKotg==");
        private static readonly CloudBlobContainer _container = new CloudBlobContainer(new Uri("http://objectdetection9a6d.blob.core.windows.net/images-container"), _credentials);
        private static readonly CloudBlockBlob _blockBlob = _container.GetBlockBlobReference("imageBlob.jpg");

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

        // Shapes holder of currently created shapes
        private Polygon _polygon;
        private Ellipse _ellipseTL;
        private Ellipse _ellipseBR;
        private Rectangle _rectangle;

        // Crop is available
        private bool _isCropping;

        // X and Y coordinates of a polygon object
        private PointerPoint _pt;
        private Coordinate _orgPosTL;
        private Coordinate _orgPosTR;
        private Coordinate _orgPosBL;
        private Coordinate _orgPosBR;

        // new coorditates
        private Coordinate _newPosTL;
        private Coordinate _newPosTR;
        private Coordinate _newPosBL;
        private Coordinate _newPosBR;

        #region Constructor, lifecycle and navigation

        public MainPage()
        {
            this.InitializeComponent();

            // Do not cache the state of the UI when suspending/navigating
            NavigationCacheMode = NavigationCacheMode.Disabled;

            ApplicationView.PreferredLaunchViewSize = new Size(600, 500);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            Application.Current.Suspending += Application_Suspending;
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

        private async void CameraButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Visibility change
            previewControl.Visibility = Visibility.Visible;
            photoButton.Visibility = Visibility.Visible;
            imageControl.Visibility = Visibility.Collapsed;

            // Start previewing
            await InitializeCameraAsync();            
        }

        private async void FileButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Visibility change
            previewControl.Visibility = Visibility.Collapsed;
            photoButton.Visibility = Visibility.Collapsed;
            imageControl.Visibility = Visibility.Visible;

            // Stop priviewing while attemping to open the file
            await CleanupPreviewAsync();

            var picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                FileTypeFilter = { ".jpg", ".jpeg", ".png" },
            };

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {                
                await LoadImage(file);                
            }

            // Open cropping field
            OpenCropField();
        }

        private async void PhotoButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ProcessImageAsync();

            // Visibility change
            previewControl.Visibility = Visibility.Collapsed;
            photoButton.Visibility = Visibility.Collapsed;
            imageControl.Visibility = Visibility.Visible;

            // Open cropping field
            OpenCropField();
        }

        // Handler for the ManipulationDelta event.
        // ManipulationDelta data is loaded into the
        // translation transform and applied to the Rectangle.
        private void CropGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Move the rectangle.
            _translateTransform.X += e.Delta.Translation.X;
            _translateTransform.Y += e.Delta.Translation.Y;
        }

        private void CropGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pt = e.GetCurrentPoint(this);

            _orgPosTL.xPos = _pt.Position.X;
            _orgPosTL.yPos = _pt.Position.Y;
            _orgPosTR.xPos = _pt.Position.X + 200;
            _orgPosTR.yPos = _pt.Position.Y;
            _orgPosBL.xPos = _pt.Position.X;
            _orgPosBL.yPos = _pt.Position.Y + 100;
            _orgPosBR.xPos = _pt.Position.X + 200;
            _orgPosBR.yPos = _pt.Position.Y + 100;
            
                                    
            
        }

        private void CropGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            
        }

        private void CropGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

        }

   

        #endregion Event handlers


        #region MediaCapture methods

        private async Task InitializeCameraAsync()
        {
            await CleanupPreviewAsync();

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

                // Prevent the device from sleeping while previewing
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

        private async Task CleanupPreviewAsync()
        {
            if (_mediaCapture != null)
            {
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                }
                
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // Cleanup the UI
                    previewControl.Source = null;                    
                    if (_displayRequest != null)
                    {
                        // Allow the device screen to sleep now that the preview is stopped
                        _displayRequest.RequestRelease();
                    }

                    // Cleanup the media capture
                    _mediaCapture.Dispose();
                    _mediaCapture = null; 
                });
            }

            if (_isCropping)
            {
                // Unlock this cropping field
                _isCropping = false;

                // Visibility change
                cropGrid.Visibility = Visibility.Collapsed;
                cropGrid.Children.Remove(_polygon);
            }
        }

        private async Task ProcessImageAsync()
        {
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

                    // Upload an image blob to Azure storage
                    await _blockBlob.DeleteIfExistsAsync();
                    await _blockBlob.UploadFromFileAsync(file);              
                }                
            }                        
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

        private void OpenCropField()
        {
            if (!_isCropping)
            {
                // Get the display size
                var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
                var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);
                var centerX = size.Width * 0.5;
                var centerY = size.Height * 0.5;

                // Lock this cropping field to create more polygons and ellipses
                _isCropping = true;

                _polygon = new Polygon();
                _polygon.Fill = new SolidColorBrush(Windows.UI.Colors.LightYellow);

                var points = new PointCollection();
                // Direction of points, TopLeft->TopRight->BottomRight->BottomLeft
                points.Add(new Windows.Foundation.Point(size.Width * 0.25, size.Height * 0.25));
                points.Add(new Windows.Foundation.Point(size.Width * 0.75, size.Height * 0.25));
                points.Add(new Windows.Foundation.Point(size.Width * 0.75, size.Height * 0.75));
                points.Add(new Windows.Foundation.Point(size.Width * 0.25, size.Height * 0.75));
                _polygon.Points = points;
                          
                _rectangle = new Rectangle();
                _rectangle.Height = size.Height * 0.5;
                _rectangle.Width = size.Width * 0.5;
                _rectangle.Fill = new SolidColorBrush(Windows.UI.Colors.BlanchedAlmond);                
                //_rectangle.Margin = new Thickness(size.Width * 0.25, size.Height * 0.25, 0, 0);                

                _ellipseTL = new Ellipse();
                _ellipseTL.Height = Constants.ELLIPSE_RADIIUS;
                _ellipseTL.Width = Constants.ELLIPSE_RADIIUS;
                _ellipseTL.Fill = new SolidColorBrush(Windows.UI.Colors.LightBlue);
                _ellipseTL.Margin = new Thickness(-(size.Width * 0.5) , -(size.Height * 0.5), 0, 0);

                _ellipseBR = new Ellipse();
                _ellipseBR.Height = Constants.ELLIPSE_RADIIUS;
                _ellipseBR.Width = Constants.ELLIPSE_RADIIUS;
                _ellipseBR.Fill = new SolidColorBrush(Windows.UI.Colors.LightGreen);
                _ellipseBR.Margin = new Thickness((size.Width * 0.5), (size.Height * 0.5), 0, 0);



                // Visibility change
                cropGrid.Visibility = Visibility.Visible;
                //cropGrid.Children.Add(_polygon);
                cropGrid.Children.Add(_rectangle);
                cropGrid.Children.Add(_ellipseTL);
                cropGrid.Children.Add(_ellipseBR);
            }

        }

        private async Task UploadImage()
        {
            
        }

        #endregion Helper functions        


    }
}
