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
using System.Net;
using System.Net.Http;
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
        public double X;
        public double Y;
    }

    public struct Location
    {
        public double Top;
        public double Bottom;
        public double Left;
        public double Right;
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
        private VideoEncodingProperties _previewProperties;
        private bool _isPreviewing;        

        // Crop image and its state variables
        private bool _isCropping;

        // Bitmap holder and Image stream
        private SoftwareBitmap _softwareBitmap;
        private WriteableBitmap _imgSource;        

        // Prevent the screen from sleeping while the camera is running
        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        // Padding constant
        private readonly int PADDING = 1;

        // Movement scale constant
        private readonly double MOVE_SCALE = 0.5;

        // Global translation transform used for changing the position of 
        // the Rectangle based on input data from the touch contact.
        private TranslateTransform _translateTransform;

        // Shapes holder of currently created shapes
        private Polygon _polygon;
        private Ellipse _ellipseTL;
        private Ellipse _ellipseBR;
        private Rectangle _rectangle;
        private Rect _cropRect;
        
        // Display size
        private Size _size;
        private double _centerX;
        private double _centerY;
        private int _videoFrameWidth;
        private int _videoFrameHeight;
        
        // X and Y coordinates of a polygon object
        private PointerPoint _pt;
        private Coordinate _orgPos;
        private Coordinate _movePos;
        private Location _rectPos;
       
        // New coorditates
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
            
            Application.Current.Suspending += Application_Suspending;                   

            Window.Current.SizeChanged += Current_SizeChanged;
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
        } 

        #endregion Constructor, lifecycle and navigation



        #region Event handlers

        private async void CameraButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await InitialiseCameraAsync();

            // Visibility change
            cameraGrid.Visibility = Visibility.Visible;
            cropGrid.Visibility = Visibility.Collapsed;
            cropConfirmButton.Visibility = Visibility.Collapsed;
            cropCancelButton.Visibility = Visibility.Collapsed;
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
                cropConfirmButton.Visibility = Visibility.Visible;
                cropCancelButton.Visibility = Visibility.Visible;

                // Open cropping field
                OpenCropField();
            }            
        }

        private async void PhotoButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await CaptureImageAsync();

            // Visibility change
            cameraGrid.Visibility = Visibility.Collapsed;
            cropGrid.Visibility = Visibility.Visible;
            cropConfirmButton.Visibility = Visibility.Visible;
            cropCancelButton.Visibility = Visibility.Visible;

            // Open cropping field
            OpenCropField();
        }

        private void CropField_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pt = e.GetCurrentPoint(this);

            // Set Original position
            _orgPos.X = _pt.Position.X;
            _orgPos.Y = _pt.Position.Y;

            // Set Initial moved position
            _movePos.X = _orgPos.X;
            _movePos.Y = _orgPos.Y;

            // Set Original rectangle position
            //_rectPosTL.X = _rect.X;
            //_rectPosTL.Y = _rect.Y;
            //_rectPosBR.X = _rect.X + _rect.Width;
            //_rectPosBR.Y = _rect.Y + _rect.Height;

            // Pointer moved event added and pointer released removed
            cropGrid.PointerMoved += CropField_PointerMoved;
            cropGrid.PointerReleased += CropField_PointerReleased;            

            Debug.WriteLine("\tOriginal Position X: {0} and Y: {1}", _orgPos.X, _orgPos.Y);
        }

        private void CropField_PointerMoved(object sender, PointerRoutedEventArgs e)
        {            
            _pt = e.GetCurrentPoint(this);

            // Off the size of grid, pointer moved event will be removed            
            if ((int)_pt.Position.Y > (int)_orgPos.Y) // Break point : below original Y-axis
            {
                if ((_rectPos.Bottom = _cropRect.Y + _cropRect.Height) < _videoFrameHeight) // Check point : rectangle is still within the bottom of the frame
                {
                    if ((int)_pt.Position.X > (int)_orgPos.X) // Break point : right-hand side of original X-axis
                    {
                        if ((_rectPos.Right = _cropRect.X + _cropRect.Width) < _videoFrameWidth) // Check point : rectangle is still within the right-hand side of the frame
                        {
                            _cropRect.X += MOVE_SCALE;
                        }
                        else
                        {
                            Debug.WriteLine("Hit Right");
                        }
                        //Debug.WriteLine("3");
                    }
                    else if ((int)_pt.Position.X < (int)_orgPos.X) // Break point : left-hand side of original X-axis
                    {
                        if ((_rectPos.Left = _cropRect.X) > 0) // Check point : rectangle is still within the left-hand side of the frame
                        {
                            _cropRect.X -= MOVE_SCALE;
                        }
                        else
                        {
                            Debug.WriteLine("Hit Left");
                        }
                        //Debug.WriteLine("5");
                    }
                    _cropRect.Y += MOVE_SCALE;
                }
                else
                {
                    Debug.WriteLine("Hit Bottom");
                }
            }
            else if ((int)_pt.Position.Y < (int)_orgPos.Y) // Break point : above original Y-axis
            {
                if ((_rectPos.Top = _cropRect.Y) > 0) // Check point : rectangle is still within the top of the frame
                {
                    if ((int)_pt.Position.X > (int)_orgPos.X) // Break point : right-hand side of original X-axis
                    {
                        if ((_rectPos.Right = _cropRect.X + _cropRect.Width) < _videoFrameWidth) // Check point : rectangle is still within the right-hand side of the frame
                        {
                            _cropRect.X += MOVE_SCALE;
                        }
                        else
                        {
                            Debug.WriteLine("Hit Right");
                        }
                        //Debug.WriteLine("1");
                    }
                    else if ((int)_pt.Position.X < (int)_orgPos.X) // Break point : left-hand side of original X-axis
                    {
                        if ((_rectPos.Left = _cropRect.X) > 0) // Check point : rectangle is still within the right-hand side of the frame
                        {
                            _cropRect.X -= MOVE_SCALE;
                        }
                        else
                        {
                            Debug.WriteLine("Hit Left");
                        }
                        //Debug.WriteLine("7");
                    }
                    _cropRect.Y -= MOVE_SCALE;
                }
                else
                {
                    Debug.WriteLine("Hit Top");
                }
            }
            else  // Break point : center of original Y-axis
            {
                if ((int)_pt.Position.X > (int)_orgPos.X) // Break point : right-hand side of original X-axis
                {
                    if ((_rectPos.Right = _cropRect.X + _cropRect.Width) < _videoFrameWidth) // Check point : rectangle is still within the right-hand side of the frame
                    {
                        _cropRect.X += MOVE_SCALE;
                    }
                    else
                    {
                        Debug.WriteLine("Hit Left");
                    }
                    //Debug.WriteLine("2");
                }
                else if ((int)_pt.Position.X < (int)_orgPos.X) // Break point : left-hand side of original X-axis
                {
                    if ((_rectPos.Left = _cropRect.X) > 0) // Check point : rectangle is still within the right-hand side of the frame
                    {
                        _cropRect.X -= MOVE_SCALE;
                    }
                    else
                    {
                        Debug.WriteLine("Hit Right");
                    }
                    //Debug.WriteLine("6");
                }
                else // Break point : center of original X-axis
                {
                    Debug.WriteLine("Center");
                }
            }            
            //clipControl.SetValue(RectangleGeometry.RectProperty, _cropRect);
        }

        private void CropField_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pt = e.GetCurrentPoint(this);

            // Pointer moved event added and pointer released removed
            cropGrid.PointerMoved -= CropField_PointerMoved;

            Debug.WriteLine("Last X: {0} and Y: {1}", _pt.Position.X, _pt.Position.Y);
        }


        private void Rect_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            rectangle.Opacity = 0.5;
        }

        private void Rect_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //, PointerRoutedEventArgs e2
            //_pt = e2.GetCurrentPoint(this);
            // translate
            rectTransform.TranslateX += e.Delta.Translation.X;
            rectTransform.TranslateY += e.Delta.Translation.Y;
            // scale
            //rectTransform.ScaleX += 0.01;
            //rectTransform.ScaleY += 0.01;
        }

        private void Rect_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            rectangle.Opacity = 0.3;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            
            Debug.WriteLine("\tChanged App resolution Width: {0}, Height: {1}", _size.Width, _size.Height);
            Debug.WriteLine("\tviewPanel: Width: {0}, Height: {1}", viewPanel.Width, viewPanel.Height, viewPanel.RenderSize.Width, viewPanel.RenderSize.Height);
            Debug.WriteLine("\tviewPanel.RenderSize: Width: {0}, Height: {1}", viewPanel.RenderSize.Width, viewPanel.RenderSize.Height);
            Debug.WriteLine("\tviewPanel.RenderTransformOrigin: X: {0}, Y: {1}", viewPanel.RenderTransformOrigin.X, viewPanel.RenderTransformOrigin.Y);
        }

        private async void cropConfirmButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Start managing image source
            await ManageImageSource(_imgSource);
            
            // Change visibility
            cropConfirmButton.Visibility = Visibility.Collapsed;
            cropCancelButton.Visibility = Visibility.Collapsed;
        }

        private void cropCancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("Cancel");
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
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings {});
                var previewResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
                var photoResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);

                VideoEncodingProperties allResolutionsAvailable;
                uint height, width;
                //use debugger at the following line to check height & width for video preview resolution
                for (int i = 0; i < previewResolution.Count; i++)
                {
                    allResolutionsAvailable = previewResolution[i] as VideoEncodingProperties;
                    height = allResolutionsAvailable.Height;
                    width = allResolutionsAvailable.Width;

                    Debug.WriteLine("\tVideo Preview resolution {0}-th Height: {1}, Width: {2}", i, height, width);
                }
                //use debugger at the following line to check height & width for captured photo resolution
                for (int i = 0; i < photoResolution.Count; i++)
                {
                    allResolutionsAvailable = photoResolution[i] as VideoEncodingProperties;
                    height = allResolutionsAvailable.Height;
                    width = allResolutionsAvailable.Width;

                    Debug.WriteLine("\tCaptured Photo resolution {0}-th Height: {1}, Width: {2}", i, height, width);
                }                

                // Prevent the device from sleeping while previewing
                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                // Video Preview resolution 8-th Height: 480, Width: 640
                // Captured Photo resolution 8-th Height: 480, Width: 640
                var selectedPreviewResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).ElementAt(8);
                var selectedPhotoResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).ElementAt(8);

                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, selectedPreviewResolution);
                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, selectedPhotoResolution);

                // Get information about the current preview
                _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                
                // Set the camera preview and the size
                previewControl.Source = _mediaCapture;                
                _videoFrameHeight = (int)_previewProperties.Height;
                _videoFrameWidth = (int)_previewProperties.Width;

                Debug.WriteLine("\tCurrent preview resolution Height: {0}, Width: {1}", _videoFrameHeight, _videoFrameWidth);

                // Set the root grid size as previewing size
                ApplicationView.GetForCurrentView().TryResizeView(new Size
                {
                    Height = _videoFrameHeight + commandBarPanel.ActualHeight,
                    Width = _videoFrameWidth
                });
                Debug.WriteLine("\tRootGrid resolution Height: {0}, Width: {1}", rootGrid.Height, rootGrid.Width);

                await _mediaCapture.StartPreviewAsync();

                Debug.WriteLine("\tPreviewControl resolution Height: {0}, Width: {1}", previewControl.Height, previewControl.Width);
                
                _isPreviewing = true;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                Debug.WriteLine("The app was denied access to the camera");
                return;
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

        private async Task CaptureImageAsync()
        {                                    
            // Create the video frame to request a SoftwareBitmap preview frame.
            var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, _videoFrameWidth, _videoFrameHeight);

            // Capture the preview frame.
            using (var currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
            {
                _softwareBitmap = currentFrame.SoftwareBitmap;       

                // Resize WriteableBitmap
                // await ResizeWriteableBitmap(imgSource)
                // in ResizeBitmapImage, it will resize the bitmap image using current view size then
                // return resized WriteableBitmap

                // private async Task<WriteableBitmap> ResizeWriteableBitmap(WriteableBitmap imgSource)

                _imgSource = new WriteableBitmap(_softwareBitmap.PixelWidth, _softwareBitmap.PixelHeight);

                _softwareBitmap.CopyToBuffer(_imgSource.PixelBuffer);                
                imageControl.Source = _imgSource;
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



        #region MediaEdit methods

        // After the response received, depeding on the values
        // whether valuese can be modified/removed from the storage or a new value could be inserted
        private async Task InsertImageToStorage()
        {
            
        }

        private async Task RemoveImageFromStorage()
        {

        }

        private async Task UpdateImageInStorage()
        {

        }

        #endregion MediaEdit methods



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
                _centerX = _videoFrameWidth * 0.5;
                _centerY = _videoFrameHeight * 0.5;
                
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


                // PointerPoint Evnents
                // Start when cropfield is opened
                // End when cropfield is closed

                cropGrid.PointerPressed += CropField_PointerPressed;
                //cropGrid.PointerMoved += CropButton_PointerMoved;                
                
                var cropButton = new Button();
                cropButton.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
                var symbolIcon = new SymbolIcon(Symbol.Crop);
                symbolIcon.MinHeight = 16;
                symbolIcon.MinWidth = 16;
                cropButton.Margin = new Thickness((_size.Width * 0.75), (_size.Height * 0.5), 0, 0);

                //cropButton.Content = symbolIcon;

                // Visibility change
                //cropGrid.Visibility = Visibility.Visible;
                //cropGrid.Children.Add(_polygon);
                //cropGrid.Children.Add(_rectangle);
                //cropGrid.Children.Add(_ellipseTL);
                //cropGrid.Children.Add(_ellipseBR);
                //cropGrid.Children.Add(cropButton);     
                
                _cropRect = new Rect();
                var initialX = (_videoFrameWidth - _centerX) * 0.5;
                var initialY = (_videoFrameHeight - _centerY) * 0.5;
                _cropRect.X = initialX;
                _cropRect.Y = initialY;
                _cropRect.Width = _videoFrameWidth * 0.5;
                _cropRect.Height = _videoFrameHeight * 0.5;                
                //clipControl.Rect = _cropRect;                

                Debug.WriteLine("\tRect Height: {0}, Width: {1}", _cropRect.Height, _cropRect.Width);
            }
        }

        private void ResizeSoftwareBitmap()
        {
            // Create new WriteableBitmap to accommodate cropped image source
            var croppedImgSource = new WriteableBitmap((int)_cropRect.Width, (int)_cropRect.Height);
            // _softwareBitmap.

        }

        private async Task ManageImageSource(WriteableBitmap imageSource)
        {
            using (var stream = imageSource.PixelBuffer.AsStream())
            {
                var buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, buffer.Length);

                // Conver image to base64 string
                var base64String = string.Empty;
                base64String = await ImageToBase64(buffer, imageSource.PixelWidth, imageSource.PixelHeight);

                // Send http post request with base64 string to azure function
                await SendHttpRequest(base64String);

                // Store image file into local/remote storeages
                await ManageFile(buffer, imageSource.PixelWidth, imageSource.PixelHeight);
            }                                         
        }

        private async Task<string> ImageToBase64(byte[] buffer, int width, int height)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                // Save the image file with jpg extension 
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96.0, 96.0, buffer);
                await encoder.FlushAsync();
                return Convert.ToBase64String(buffer);
            }
        }

        private async Task ManageFile(byte[] buffer, int width, int height)
        {            
            // Save the writeableBitmap object to JPG Image file 
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("JPG File", new List<string>() { ".jpg" });
            var file = await picker.PickSaveFileAsync();

            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {                
                if (file == null)
                {
                    return;
                }                
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);
                // Save the image file with jpg extension 
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96.0, 96.0, buffer);
                await encoder.FlushAsync();                
            }

            // Upload an image blob to Azure storage
            await _blockBlob.DeleteIfExistsAsync();
            await _blockBlob.UploadFromFileAsync(file);
        }

        private static async Task SendHttpRequest(string base64String)
        {
            var client = new HttpClient();
            var resp = await client.GetAsync("https://objectdetectionopencv.azurewebsites.net/api/DetectObjectsCSharp_v02?code=vpmvzhrlBrYsQQpqRvvVvD6muz6gaRPGgZ3SWTBCOwLNY6JIhXsPnA==&name=Yongjin");
            var content = await resp.Content.ReadAsStringAsync();

            Debug.WriteLine(content);
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://objectdetectionopencv.azurewebsites.net/api/DetectObjectsCSharp_v02?code=vpmvzhrlBrYsQQpqRvvVvD6muz6gaRPGgZ3SWTBCOwLNY6JIhXsPnA==");
            //request.Method = "POST";
            //request.ContentType = "application/json";
            //Stream stream = request.GetRequestStream();
            //string json = "{\"name\": \"Azure\" }";
            //byte[] buffer = Encoding.UTF8.GetBytes(json);
            //stream.Write(buffer, 0, buffer.Length);
            //HttpWebResponse res = (HttpWebResponse)request.GetResponse();

        }

        #endregion Helper functions           
    }
}