using System.Collections.ObjectModel;
using Vigram;
using Vigram.Models;
using Vigram.Services;

namespace MyMauiApp;

public partial class MainPage : ContentPage
{
    private IBluetoothService? _bluetoothService;
    private IPeripheralService? _peripheralService;

    private readonly ObservableCollection<KeyValuePair<string, string>> _devices = new();
    private string? _connectedDevice;

    private Label _authLabel;
    private Label _bluetoothStatus;
    private ListView _devicesList;
    private Button _scanButton;

    private Label _stateLabel;

    private Label _stateConfigLabel;
    private Label _ggaLabel;
    private Label _gstLabel;
    private Label _txtLabel;
    private Label _batteryLabel;
    private Label _versionLabel;
    private Button _batteryButton;
    private Button _versionButton;
    private Grid _scanSection;

    private readonly StackLayout _peripheralSection;

    private bool _isScanning = false;

    public MainPage()
    {
        Title = "Vigram SDK Example";

        // ----------------- Bluetooth UI -----------------
        _authLabel = new Label
        {
            Text = "SDK: Initializing...",
            HorizontalOptions = LayoutOptions.Start,
            FontSize = 18
        };

        _bluetoothStatus = new Label
        {
            Text = "Bluetooth: -",
            HorizontalOptions = LayoutOptions.Start
        };

        _scanButton = new Button
        {
            Text = "Start Scan",
            HorizontalOptions = LayoutOptions.Start
        };
        _scanButton.Clicked += OnScanClicked;

        _devicesList = new ListView
        {
            ItemsSource = _devices,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label
                {
                    VerticalOptions = LayoutOptions.Start,
                    VerticalTextAlignment = TextAlignment.Start,
                    Padding = new Thickness(0, 5)
                };
                label.SetBinding(Label.TextProperty, "Value");
                return new ViewCell { View = label };
            }),
            HasUnevenRows = true
        };
        _devicesList.ItemTapped += OnDeviceTapped;

        // ----------------- Peripheral UI -----------------
        _stateLabel = new Label { Text = "State: -" };
        _stateConfigLabel = new Label { Text = "Configuration state: -" };
        _ggaLabel = new Label { Text = "GGA: -" };
        _gstLabel = new Label { Text = "GST: -" };
        _txtLabel = new Label { Text = "TXT: -" };
        _batteryLabel = new Label { Text = "Battery: -" };
        _versionLabel = new Label { Text = "Version: -" };

        _batteryButton = new Button { Text = "Request Battery" };
        _batteryButton.Clicked += OnBatteryClicked;

        _versionButton = new Button { Text = "Request Version" };
        _versionButton.Clicked += OnVersionClicked;

        _peripheralSection = new StackLayout
        {
            Padding = new Thickness(20),
            Spacing = 10,
            IsVisible = false,
            VerticalOptions = LayoutOptions.FillAndExpand,
            Children =
            {
                new BoxView { HeightRequest = 1, Color = Colors.Gray },
                new Label { Text = "Peripheral Info", HorizontalOptions = LayoutOptions.Center },
                _stateLabel,
                _stateConfigLabel,
                new Label { Text = "GGA Message" },
                _ggaLabel,
                new Label { Text = "GST Message" },
                _gstLabel,
                new Label { Text = "TXT Message" },
                _txtLabel,
                _batteryButton,
                _batteryLabel,
                _versionButton,
                _versionLabel
            }
        };

        // ----------------- Layout -----------------
        var topSection = new StackLayout
        {
            Spacing = 10,
            Children =
            {
                _authLabel,
                _scanButton,
                _bluetoothStatus
            }
        };

        var scanGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        scanGrid.Children.Add(topSection);
        Grid.SetRow(topSection, 0);

        scanGrid.Children.Add(_devicesList);
        Grid.SetRow(_devicesList, 1);

        var mainLayout = new Grid();
        mainLayout.Children.Add(scanGrid);
        mainLayout.Children.Add(_peripheralSection);

        _scanSection = scanGrid;

        Content = new ScrollView
        {
            Content = mainLayout
        };

#if IOS
        // Initialize Vigram SDK authentication using your API key.
        // This allows the app to use Vigram services.
        var auth = VigramSdk.AuthenticationService("YOUR_TOKEN");
        auth.Initialize(success =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _authLabel.Text = success ? "✅ SDK Ready" : "❌ Auth Failed";
            });
        });
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if IOS
        // Get an instance of Vigram's Bluetooth service.
        // Used for scanning, connecting, and managing BLE devices.
        _bluetoothService = VigramSdk.BluetoothService();
#endif
    }

    private void OnScanClicked(object? sender, EventArgs? e)
    {
        if (_bluetoothService == null) return;

        if (!_isScanning)
        {
            _devices.Clear();
            _bluetoothStatus.Text = "Scanning...";
            _scanButton.Text = "Stop Scan";
            _isScanning = true;

            // Start scanning for BLE devices via Vigram SDK
            // The callback provides the UUID and name of each discovered device.
            _bluetoothService.StartScan((uuid, name) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!_devices.Any(d => d.Key == uuid))
                    {
                        _devices.Add(new KeyValuePair<string, string>(uuid, name));
                    }
                });
            });
        }
        else
        {
            // Stop scanning using Vigram SDK
            _bluetoothService.StopScan();
            _bluetoothStatus.Text = "Scan stopped";
            _scanButton.Text = "Start Scan";
            _isScanning = false;
        }
    }

    private void OnDeviceTapped(object? sender, ItemTappedEventArgs e)
    {
        if (_bluetoothService == null || e.Item == null) return;

        var selected = (KeyValuePair<string, string>)e.Item;
        string deviceUuid = selected.Key;
        string deviceName = selected.Value;

        _bluetoothStatus.Text = $"Connecting to {deviceName} ({deviceUuid})...";

        // Connect to a BLE device using Vigram SDK
        _bluetoothService.Connect(deviceUuid, success =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (success)
                {
                    _connectedDevice = deviceUuid;
                    _bluetoothStatus.Text = $"✅ Connected to {deviceName}";
                    _scanSection.IsVisible = false;
                    _peripheralSection.IsVisible = true;
                    // Initialize peripheral-specific functions (NMEA, battery, version)
                    InitializePeripheralSection(deviceUuid);
                }
                else
                {
                    _bluetoothStatus.Text = "❌ Connection failed";
                }
            });
        });
    }

    private void InitializePeripheralSection(string deviceUuid)
    {
#if IOS
        // Get an instance of Vigram peripheral service for the connected device.
        // Used to observe device state, NMEA messages, and configuration.
        _peripheralService = VigramSdk.PeripheralService();
        _peripheralService.Start(deviceUuid);

        // Subscribe to peripheral configuration updates via Vigram SDK
        _peripheralService.ObserveConfigurationState((state, message) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
          {
              switch (state)
              {
                  case ConfigurationState.InProgress:
                      var p = $"Configuring...\n{message}";
                      _stateConfigLabel.Text = $"Configuration state: {message}";
                      break;
                  case ConfigurationState.Done:
                      _stateConfigLabel.Text = "Configuration state: Done";
                      break;
                  case ConfigurationState.Failed:
                      _stateConfigLabel.Text = $"Configuration state: Failed: {message}";
                      break;
                  case ConfigurationState.PeripheralError:
                      _stateConfigLabel.Text = $"Configuration state: Peripheral error: {message}";
                      break;
                  default:
                      break;
              }
          });
        });

        // Observe the peripheral connection state
        _peripheralService.ObserveState(state =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                switch (state)
                {
                    case CBPeripheralState.Unknown:
                        _stateLabel.Text = $"State: Unknown";
                        break;
                    case CBPeripheralState.Disconnected:
                        _stateLabel.Text = $"State: Disconnected";
                        _peripheralService?.Stop();
                        ResetToScanView();
                        break;
                    case CBPeripheralState.Connected:
                        _stateLabel.Text = $"State: Connected";
                        // Start observing NMEA messages (GGA, GST, TXT)
                        _peripheralService.ObserveNmea(OnNmeaReceived);
                        break;
                }
            });
        });
#endif
    }

    private void OnNmeaReceived(NmeaMessage msg)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (msg)
            {
                case NmeaMessage.Gga gga:
                    _ggaLabel.Text = FormatGga(gga.Data);
                    break;
                case NmeaMessage.Gst gst:
                    _gstLabel.Text = FormatGst(gst.Data);
                    break;
                case NmeaMessage.Txt txt:
                    _txtLabel.Text = FormatTxt(txt.Data);
                    break;
                case NmeaMessage.Unknown unknown:
                    _txtLabel.Text = $"Unknown: {unknown.Raw}";
                    break;
            }
        });
    }

    private void OnBatteryClicked(object? sender, EventArgs? e)
    {
        if (_peripheralService == null) return;

        // Request battery status from the connected peripheral via Vigram SDK
        _peripheralService.RequestBattery(battery =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _batteryLabel.Text = $"Battery: {battery}%";
            });
        });
    }

    private void OnVersionClicked(object? sender, EventArgs? e)
    {
        if (_peripheralService == null) return;

        // Request firmware version from the connected peripheral via Vigram SDK
        _peripheralService.RequestVersion(version =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _versionLabel.Text = $"Version: {version ?? "empty"}";
            });
        });
    }

    private void ResetToScanView()
    {
        _connectedDevice = null;
        _devices.Clear();
        _isScanning = false;
        _scanButton.Text = "Start Scan";

        _peripheralSection.IsVisible = false;
        _scanSection.IsVisible = true;

        ResetPeripheralUI();
    }

    private void ResetPeripheralUI()
    {
        _bluetoothStatus.Text = "Scan stopped";
        _stateLabel.Text = "State: -";
        _ggaLabel.Text = "GGA: -";
        _gstLabel.Text = "GST: -";
        _txtLabel.Text = "TXT: -";
        _batteryLabel.Text = "Battery: -";
        _versionLabel.Text = "Version: -";
    }

    #region Format Methods
    private string FormatGga(GgaMessage gga)
    {
        return $"Time: {gga.Time}\n" +
               $"Timestamp: {gga.Timestamp}\n" +
               $"Latitude: {gga.LocationLatitude}\n" +
               $"Longitude: {gga.LocationLongitude}\n" +
               $"Quality: {gga.Quality}\n" +
               $"Satellite Count: {gga.SatelliteCount}\n" +
               $"HDOP: {gga.Hdop}\n" +
               $"Reference Altitude: {gga.ReferenceAltitude}\n" +
               $"Geoid Separation: {gga.GeoidSeparation}\n" +
               $"Correction Age: {gga.CorrectionAge}\n" +
               $"Correction Station ID: {gga.CorrectionStationID}\n" +
               $"Coordinate Latitude: {gga.CoordinateLatitude}\n" +
               $"Coordinate Longitude: {gga.CoordinateLongitude}\n" +
               $"Accuracy Horizontal: {gga.AccuracyHorizontal}\n" +
               $"Accuracy Vertical: {gga.AccuracyVertical}\n" +
               $"VDOP: {gga.Vdop}\n" +
               $"PDOP: {gga.Pdop}";
    }

    private string FormatGst(GstMessage gst)
    {
        return $"Time: {gst.Time}\n" +
               $"Timestamp: {gst.Timestamp}\n" +
               $"RMS: {gst.Rms}\n" +
               $"SemiMajor1SigmaError: {gst.SemiMajor1SigmaError}\n" +
               $"SemiMinor1SigmaError: {gst.SemiMinor1SigmaError}\n" +
               $"ErrorEllipseOrientation: {gst.ErrorEllipseOrientation}\n" +
               $"Latitude Error: {gst.LatitudeError}\n" +
               $"Longitude Error: {gst.LongitudeError}\n" +
               $"Altitude Error: {gst.AltitudeError}\n" +
               $"Accuracy Horizontal: {gst.AccuracyHorizontal}\n" +
               $"Accuracy Vertical: {gst.AccuracyVertical}\n" +
               $"Coordinate Latitude: {gst.CoordinateLatitude}\n" +
               $"Coordinate Longitude: {gst.CoordinateLongitude}\n" +
               $"HDOP: {gst.Hdop}\n" +
               $"VDOP: {gst.Vdop}\n" +
               $"PDOP: {gst.Pdop}\n" +
               $"Satellite Count: {gst.SatelliteCount}";
    }

    private string FormatTxt(TxtMessage txt)
    {
        return $"Time: {txt.Time}\n" +
               $"Timestamp: {txt.Timestamp}\n" +
               $"Latitude: {txt.CoordinateLatitude}\n" +
               $"Longitude: {txt.CoordinateLongitude}\n" +
               $"Satellite Count: {txt.SatelliteCount}\n" +
               $"HDOP: {txt.Hdop}\n" +
               $"VDOP: {txt.Vdop}\n" +
               $"PDOP: {txt.Pdop}\n" +
               $"Accuracy Horizontal: {txt.AccuracyHorizontal}\n" +
               $"Accuracy Vertical: {txt.AccuracyVertical}\n" +
               $"Total Number Of Message: {txt.TotalNumberOfMessage}\n" +
               $"Message Number: {txt.MessageNumber}\n" +
               $"Text Identifier: {txt.TextIdentifier}\n" +
               $"Message: {txt.Message}";
    }
    #endregion
}