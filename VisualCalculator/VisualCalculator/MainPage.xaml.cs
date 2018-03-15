using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
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
        // Blob name will be sent by http request
        private static readonly StorageCredentials _credentials = new StorageCredentials("objectdetection9a6d", "+8s9aGusj+5w5iBnXCdqE/OGV3qhLZZFfTrkZxVh+/hEX4cBEX9lRcywiY/q2O1BUuDIqtXQ5YrIV1og6JKotg==");
        private static readonly CloudBlobContainer _container = new CloudBlobContainer(new Uri("http://objectdetection9a6d.blob.core.windows.net/images-container"), _credentials);
        private static readonly CloudBlockBlob _blockBlob = _container.GetBlockBlobReference("imageBlob.jpg");

        // MediaCapture and its state variables
        private MediaCapture _mediaCapture;
        private bool _isPreviewing;

        // Crop image and its state variables
        private bool _isCropping;

        // Bitmap holder of currently loaded image.
        private SoftwareBitmap _softwareBitmap;
        private WriteableBitmap _imgSource;

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
        private Rect _rect;

        

        // Display size
        private Size _size;

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

            ApplicationView.PreferredLaunchViewSize = new Size(690, 540);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            Application.Current.Suspending += Application_Suspending;

            // Get the display size
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            _size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);            
        }

        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await InitialiseCameraAsync();
                deferral.Complete();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitialiseCameraAsync();
            Window.Current.SizeChanged += Current_SizeChanged;
        } 

        #endregion Constructor, lifecycle and navigation


        #region Event handlers

        private async void CameraButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await InitialiseCameraAsync();

            // Visibility change
            cameraGrid.Visibility = Visibility.Visible;
            cropGrid.Visibility = Visibility.Collapsed;


            Debug.WriteLine("_size: Width: {0}, Height: {1}", _size.Width, _size.Height);
            Debug.WriteLine("viewGrid: Width: {0}, Height: {1}", viewGrid.Width, viewGrid.Height, viewGrid.RenderSize.Width, viewGrid.RenderSize.Height);
            Debug.WriteLine("viewGrid.RenderSize: Width: {0}, Height: {1}", viewGrid.RenderSize.Width, viewGrid.RenderSize.Height);
            Debug.WriteLine("viewGrid.RenderTransformOrigin: X: {0}, Y: {1}", viewGrid.RenderTransformOrigin.X, viewGrid.RenderTransformOrigin.Y);
        }

        private async void FileButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                FileTypeFilter = { ".jpg", ".jpeg", ".png" },
            };

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await CleanupPreviewAndBitmapAsync();
                await LoadImageAsync(file);

                // Visibility change
                cameraGrid.Visibility = Visibility.Collapsed;
                cropGrid.Visibility = Visibility.Visible;

                Debug.WriteLine("_size: Width: {0}, Height: {1}", _size.Width, _size.Height);
                Debug.WriteLine("viewGrid: Width: {0}, Height: {1}", viewGrid.Width, viewGrid.Height, viewGrid.RenderSize.Width, viewGrid.RenderSize.Height);
                Debug.WriteLine("viewGrid.RenderSize: Width: {0}, Height: {1}", viewGrid.RenderSize.Width, viewGrid.RenderSize.Height);
                Debug.WriteLine("viewGrid.RenderTransformOrigin: X: {0}, Y: {1}", viewGrid.RenderTransformOrigin.X, viewGrid.RenderTransformOrigin.Y);
            }

            // Open cropping field
            //OpenCropField();
        }

        private async void PhotoButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ProcessImageAsync();

            // Visibility change
            cameraGrid.Visibility = Visibility.Collapsed;
            cropGrid.Visibility = Visibility.Visible;

            // Open cropping field
            //OpenCropField();
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

        private void CropButton_PointerPressed(object sender, PointerRoutedEventArgs e)
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

            // Pointer moved event added and pointer released removed
            cropGrid.PointerMoved += CropButton_PointerMoved;
            //cropGrid.PointerReleased -= CropButton_PointerReleased;

            Debug.WriteLine("Start X: {0} and Y: {1}", _pt.Position.X, _pt.Position.Y);
            Debug.WriteLine("X: {0}\nY: {1}\nWidth: {2}\nHeight: {3}", _rect.X, _rect.Y, _rect.Width, _rect.Height);


        }

        private void CropButton_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _pt = e.GetCurrentPoint(this);

            // Off the size of grid, pointer moved event will be removed
            if (_pt.Position.X < viewGrid.RenderTransformOrigin.X
                || _pt.Position.Y < viewGrid.RenderTransformOrigin.Y
                || _pt.Position.X > viewGrid.RenderSize.Width
                || _pt.Position.Y > viewGrid.RenderSize.Height)
            {
                // Pointer moved event added and pointer released removed
                cropGrid.PointerMoved -= CropButton_PointerMoved;
                Debug.WriteLine("Out of rect!");
            }

            Debug.WriteLine("Current X: {0} and Y: {1}", _pt.Position.X, _pt.Position.Y);
        }

        private void CropButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pt = e.GetCurrentPoint(this);

            // Pointer moved event added and pointer released removed
            cropGrid.PointerMoved -= CropButton_PointerMoved;

            Debug.WriteLine("Last X: {0} and Y: {1}", _pt.Position.X, _pt.Position.Y);
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            Debug.WriteLine("_size: Width: {0}, Height: {1}", _size.Width, _size.Height);
            Debug.WriteLine("viewGrid: Width: {0}, Height: {1}", viewGrid.Width, viewGrid.Height, viewGrid.RenderSize.Width, viewGrid.RenderSize.Height);
            Debug.WriteLine("viewGrid.RenderSize: Width: {0}, Height: {1}", viewGrid.RenderSize.Width, viewGrid.RenderSize.Height);
            Debug.WriteLine("viewGrid.RenderTransformOrigin: X: {0}, Y: {1}", viewGrid.RenderTransformOrigin.X, viewGrid.RenderTransformOrigin.Y);
        }

        #endregion Event handlers


        #region MediaCapture methods

        private async Task InitialiseCameraAsync()
        {
            await CleanupPreviewAndBitmapAsync();

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

        private async Task CleanupPreviewAndBitmapAsync()
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

            if (_softwareBitmap != null)
            {
                _softwareBitmap.Dispose();
                _softwareBitmap = null;
            }
        }

        private async Task ProcessImageAsync()
        {            
            // Display the captured image
            // Get information about the preview.
            // Store the image into my pictures folder
            var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
            var file = await myPictures.SaveFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);

            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(captureStream);
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    _softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                    _imgSource = new WriteableBitmap(_softwareBitmap.PixelWidth, _softwareBitmap.PixelHeight);

                    _softwareBitmap.CopyToBuffer(_imgSource.PixelBuffer);
                    imageControl.Source = _imgSource;
                    
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

        private async Task LoadImageAsync(StorageFile file)
        {
            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(fileStream);

                _softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                _imgSource = new WriteableBitmap(_softwareBitmap.PixelWidth, _softwareBitmap.PixelHeight);

                _softwareBitmap.CopyToBuffer(_imgSource.PixelBuffer);
                imageControl.Source = _imgSource;
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

        private void OpenCropField()
        {
            if (!_isCropping)
            {
                var centerX = _size.Width * 0.5;
                var centerY = _size.Height * 0.5;

                // Lock this cropping field to create more polygons and ellipses
                _isCropping = true;

                _polygon = new Polygon();
                _polygon.Fill = new SolidColorBrush(Windows.UI.Colors.LightYellow);

                var points = new PointCollection();
                // Direction of points, TopLeft->TopRight->BottomRight->BottomLeft
                points.Add(new Windows.Foundation.Point(_size.Width * 0.25, _size.Height * 0.25));
                points.Add(new Windows.Foundation.Point(_size.Width * 0.75, _size.Height * 0.25));
                points.Add(new Windows.Foundation.Point(_size.Width * 0.75, _size.Height * 0.75));
                points.Add(new Windows.Foundation.Point(_size.Width * 0.25, _size.Height * 0.75));
                _polygon.Points = points;

                _rectangle = new Rectangle();
                _rectangle.Height = _size.Height * 0.5;
                _rectangle.Width = _size.Width * 0.5;
                _rectangle.Fill = new SolidColorBrush(Windows.UI.Colors.BlanchedAlmond);
                //_rectangle.Margin = new Thickness(size.Width * 0.25, size.Height * 0.25, 0, 0);                

                _ellipseTL = new Ellipse();
                _ellipseTL.Height = Constants.ELLIPSE_RADIIUS;
                _ellipseTL.Width = Constants.ELLIPSE_RADIIUS;
                _ellipseTL.Fill = new SolidColorBrush(Windows.UI.Colors.LightBlue);
                _ellipseTL.Margin = new Thickness(-(_size.Width * 0.5), -(_size.Height * 0.5), 0, 0);

                _ellipseBR = new Ellipse();
                _ellipseBR.Height = Constants.ELLIPSE_RADIIUS;
                _ellipseBR.Width = Constants.ELLIPSE_RADIIUS;
                _ellipseBR.Fill = new SolidColorBrush(Windows.UI.Colors.LightGreen);
                _ellipseBR.Margin = new Thickness((_size.Width * 0.5), (_size.Height * 0.5), 0, 0);

                cropGrid.PointerPressed += CropButton_PointerPressed;
                // pointer moved event will be added in the pointer pressed event
                cropGrid.PointerReleased += CropButton_PointerReleased;

                var cropButton = new Button();
                cropButton.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
                var symbolIcon = new SymbolIcon(Symbol.Crop);
                symbolIcon.MinHeight = 16;
                symbolIcon.MinWidth = 16;
                cropButton.Margin = new Thickness((_size.Width * 0.75), (_size.Height * 0.5), 0, 0);

                //cropButton.Content = symbolIcon;

                // Visibility change
                cropGrid.Visibility = Visibility.Visible;
                //cropGrid.Children.Add(_polygon);
                //cropGrid.Children.Add(_rectangle);
                //cropGrid.Children.Add(_ellipseTL);
                //cropGrid.Children.Add(_ellipseBR);
                //cropGrid.Children.Add(cropButton);                

                _rect = new Rect();
                _rect.X = centerX - (_size.Width * 0.5);
                _rect.Y = centerY - (_size.Height * 0.5);
                _rect.Width = _size.Width * 0.5;
                _rect.Height = _size.Height * 0.5;
                //clipControl.Rect = _rect;
            }
        }

        #endregion Helper functions        

    }
}