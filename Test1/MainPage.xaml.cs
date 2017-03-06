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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Devices.Sensors; // Required to access the sensor platform and the compass
using Windows.Devices.Enumeration;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Test1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture _mediaCapture;
        bool _isPreviewing;

        Compass _compass;
        LightSensor _lightsensor;

        ProximitySensor _proximitysensor;
        DeviceWatcher _watcher;

        DisplayRequest _displayRequest = new DisplayRequest();

        // Rotation metadata to apply to the preview stream (MF_MT_VIDEO_ROTATION)
        // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;

            //Compass
            _compass = Compass.GetDefault(); // Get the default compass object
            // Assign an event handler for the compass reading-changed event
            if (_compass != null)
            {
                // Establish the report interval for all scenarios
                uint minReportInterval = _compass.MinimumReportInterval;
                uint reportInterval = minReportInterval > 50 ? minReportInterval : 50;
                _compass.ReportInterval = reportInterval;
                _compass.ReadingChanged += new TypedEventHandler<Compass, CompassReadingChangedEventArgs>(CompassReadingChanged);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Compass failure");
                txtMagnetic.Text = "Compass\nfailure";
            }

            //Light Sensor
            _lightsensor = LightSensor.GetDefault();
            if (_lightsensor != null)
            {
                // Establish the report interval for all scenarios
                uint minReportInterval = _lightsensor.MinimumReportInterval;
                uint reportInterval = minReportInterval > 50 ? minReportInterval : 50;
                _lightsensor.ReportInterval = reportInterval;

                // Establish the even thandler
                _lightsensor.ReadingChanged += new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(LightReadingChanged);
            }

            //Proximity Sensor
            if (_lightsensor != null)
            {
                // Establish the report interval for all scenarios
                uint minReportInterval = _lightsensor.MinimumReportInterval;
                uint reportInterval = minReportInterval > 50 ? minReportInterval : 50;
                _lightsensor.ReportInterval = reportInterval;

                // Establish the even thandler
                _lightsensor.ReadingChanged += new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(LightReadingChanged);
            }

            //Watcher pre nájdenie Proximity senzoru
            _watcher = DeviceInformation.CreateWatcher(ProximitySensor.GetDeviceSelector());
            _watcher.Added += OnProximitySensorAdded;
            _watcher.Start();
        }

        //Invoke when Proximity sensor found
        private void OnProximitySensorAdded(DeviceWatcher sender, DeviceInformation device)
        {
            if (null == _proximitysensor)
            {
                ProximitySensor foundSensor = ProximitySensor.FromId(device.Id);
                if (null != foundSensor)
                {
                    if (null != foundSensor)
                    {
                        _proximitysensor = foundSensor;
                        _proximitysensor.ReadingChanged += new TypedEventHandler<ProximitySensor, ProximitySensorReadingChangedEventArgs>(ProximityReadingChanged);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Could not get a proximity sensor from the device id");
                }
            }
        }

        //Zmena údajov proximity snímača
        async private void ProximityReadingChanged(ProximitySensor sender, ProximitySensorReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ProximitySensorReading reading = e.Reading;
                if (null != reading)
                {
                    ProximityText.Text = reading.IsDetected ? "Detected" : "Not detected";
                }
            });
        }

        //Zmena údajov magnetometra
        private async void CompassReadingChanged(object sender, CompassReadingChangedEventArgs e)
        {
            String smer;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                CompassReading reading = e.Reading;
                RotateTransform _rotateTransform = new RotateTransform();

                if (reading.HeadingTrueNorth.HasValue)
                {
                    if (reading.HeadingTrueNorth >= 315 || reading.HeadingTrueNorth < 45) smer = "Sever";
                    else if (reading.HeadingTrueNorth >= 45 && reading.HeadingTrueNorth < 135) smer = "Východ";
                    else if (reading.HeadingTrueNorth >= 135 && reading.HeadingTrueNorth < 225) smer = "Juh";
                    else smer = "Západ";

                    _rotateTransform.Angle = 360 - (int)reading.HeadingTrueNorth;
                    image.RenderTransform = _rotateTransform;

                    txtMagnetic.Text = smer + "\n" + String.Format("{0,3:0}°", reading.HeadingTrueNorth);
                } 
                else txtMagnetic.Text = "No reading.";
            });
        }

        private async void LightReadingChanged(object sender, LightSensorReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
             {
                 LightSensorReading reading = e.Reading;
                 LightText.Text = String.Format("{0,5:0.0}Lux", reading.IlluminanceInLux);
             });
        }

        //Spustenie kamery
        private async Task StartPreviewAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;

                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
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
                    _isPreviewing = false;
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

        private async Task SetPreviewRotationAsync()
        {
            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, 90);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        //Stlačenie tlačidla
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            if (_isPreviewing == false)
            {
                button.Content = "Camera STOP";
                await StartPreviewAsync();
                await SetPreviewRotationAsync();
            }
            else
            {
                button.Content = "Camera START";
                await CleanupCameraAsync();
            }
                
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

                TurnOffSensors();
            }
        }

        //Obnovenie aplikácie
        private void Application_Resuming(object sender, object e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                //Zapnutie kompasu
                if (_compass != null)
                {
                    // Establish the report interval for all scenarios
                    uint minReportInterval = _compass.MinimumReportInterval;
                    uint reportInterval = minReportInterval > 50 ? minReportInterval : 50;
                    _compass.ReportInterval = reportInterval;
                    _compass.ReadingChanged += new TypedEventHandler<Compass, CompassReadingChangedEventArgs>(CompassReadingChanged);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Compass failure");
                    txtMagnetic.Text = "Compass\nfailure";
                }

                //Zapnutie Light senzoru
                if (_lightsensor != null)
                {
                    // Establish the report interval for all scenarios
                    uint minReportInterval = _lightsensor.MinimumReportInterval;
                    uint reportInterval = minReportInterval > 50 ? minReportInterval : 50;
                    _lightsensor.ReportInterval = reportInterval;
                    _lightsensor.ReadingChanged += new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(LightReadingChanged);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Ligth Sensor failure");
                    LightText.Text = "LightSensor\nfailure";
                }

                //Zapnutie Proximity senzora
                if (_lightsensor != null)
                {
                    _proximitysensor.ReadingChanged += new TypedEventHandler<ProximitySensor, ProximitySensorReadingChangedEventArgs>(ProximityReadingChanged);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Proximity Sensor failure");
                    LightText.Text = "ProximitySensor\nfailure";
                }
            }
        }

        //Odídenie z aplikácie
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
            TurnOffSensors();
        }

        void TurnOffSensors()
        {
            //Vypnutie kompasu
            if (_compass != null)
            {
                _compass.ReportInterval = 0;
                _compass.ReadingChanged -= new TypedEventHandler<Compass, CompassReadingChangedEventArgs>(CompassReadingChanged);
            }

            //Vypnutie Light senzora
            if (_lightsensor != null)
            {
                _lightsensor.ReportInterval = 0;
                _lightsensor.ReadingChanged -= new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(LightReadingChanged);
            }

            //Vypnutie Proximity senzora
            if (_lightsensor != null)
            {
                _proximitysensor.ReadingChanged -= new TypedEventHandler<ProximitySensor, ProximitySensorReadingChangedEventArgs>(ProximityReadingChanged);
            }
        }

        /*protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_compass != null)
            {
                // Establish the report interval for all scenarios
                uint minReportInterval = _compass.MinimumReportInterval;
                uint reportInterval = 50;// minReportInterval > 16 ? minReportInterval : 16;
                _compass.ReportInterval = reportInterval;
                _compass.ReadingChanged += new TypedEventHandler<Compass, CompassReadingChangedEventArgs>(CompassReadingChanged);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Compass failure");
                txtMagnetic.Text = "Compass\nfailure";
            }
        }*/

        //Stlačenie toggle tlačidla
        private void toggleButton_Click(object sender, RoutedEventArgs e)
        {
            toggleButton.IsChecked = false;
            if (Splitter.IsPaneOpen == false) Splitter.IsPaneOpen = true;
            else Splitter.IsPaneOpen = false;
        }
    }
}
