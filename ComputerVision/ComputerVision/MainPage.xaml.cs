using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ComputerVision
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Microsoft cognitive service - Computer Vision
        // The subscriptionKey string key and the uri base have to be in the same region
        const string subscriptionKey = "4667d551d2504931b6cd71ffdea1118e";
        const string uriBase = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/analyze";

        // MediaCapture and its state variables
        private MediaCapture _mediaCapture;
        private VideoEncodingProperties _previewProperties;
        private bool _isPreviewing;

        // Prevent the screen from sleeping while the camera is running
        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        // Display size        
        private int _videoFrameWidth;
        private int _videoFrameHeight;

        // Bitmap holder and Image stream
        private SoftwareBitmap _softwareBitmap;
        private WriteableBitmap _imgSource;

        // Image byte array
        private byte[] _byteData;



        #region Constructor, lifecycle and navigation

        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Suspending += Application_Suspending;
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
                //OpenCropField();
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
            //OpenCropField();
        }
        private async void cropConfirmButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Start managing image source
            await MakeAnalysisRequest(_byteData);

            // Change visibility
            cropConfirmButton.Visibility = Visibility.Collapsed;
            cropCancelButton.Visibility = Visibility.Collapsed;
        }
        private void cropCancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("Cancel");
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
        // Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on
        private async Task StartPreviewAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings { });
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
                // Get byte array from file stream
                BinaryReader br = new BinaryReader(fileStream.AsStream());
                _byteData = br.ReadBytes((int)fileStream.Size);

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
        // Gets the analysis of the specified image file by using the Computer Vision REST API.
        static async Task MakeAnalysisRequest(byte[] _byteData)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "visualFeatures=Categories,Description,Color&language=en";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            using (ByteArrayContent content = new ByteArrayContent(_byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                // Display the JSON response.
                Debug.WriteLine("\nResponse:\n");
                Debug.WriteLine(JsonPrettyPrint(contentString));
            }
        }
        // Formats the given JSON string by adding line breaks and indents.
        static string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            string INDENT_STRING = "    ";
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < json.Length; i++)
            {
                var ch = json[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && json[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        #endregion Helper functions
    }
    static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }
    }
}
