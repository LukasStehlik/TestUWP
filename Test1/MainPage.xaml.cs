using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Windows.Media.Capture;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Test1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int counter=0;

        MediaCapture _mediaCapture;
        bool _isPreviewing;

        DisplayRequest _displayRequest;

        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Suspending += Application_Suspending;
        }

        //Spustenie kamery
        private async Task StartPreviewAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();
                _mediaCapture.SetPreviewRotation(VideoRotation.Clockwise90Degrees);

                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;

                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed. {0}", ex.Message);
            }
        }

        //Vypnutie kamery
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

                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });
            }

        }

        //Stlačenie tlačidla
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Test");
            if (counter == 0) textBox.Text = "Hello World";
            else textBox.Text = "You shall not pass !!!";
            counter++;

            await StartPreviewAsync();
        }

        //Vypnutie aplikácie
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }

        //Odídenie z aplikácie
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
        }

        //Stlačenie tlačidla
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Splitter.IsPaneOpen = false;
        }

        //Stlačenie toggle tlačidla
        private void toggleButton_Click(object sender, RoutedEventArgs e)
        {
            toggleButton.IsChecked = false;
            if (Splitter.IsPaneOpen == false) Splitter.IsPaneOpen = true;
            else Splitter.IsPaneOpen = false;
        }
    }
}
