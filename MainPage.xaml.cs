using System.Collections.ObjectModel;
using MyMauiApp.Controls;
using Vigram;
using Vigram.Models;
using Vigram.Services;

namespace MyMauiApp
{

    /// <summary>
    /// Main page for the MAUI application demonstrating Vigram SDK usage.
    /// Handles Bluetooth scanning, peripheral connection, NMEA observation, and NTRIP connection.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        // ----------------- Services -----------------
        private IBluetoothService? _bluetoothService;   // Vigram Bluetooth service
        private IPeripheralService? _peripheralService; // Vigram peripheral service
        private INtripService? _ntripService;           // Vigram NTRIP service

        // ----------------- Device & Peripheral State -----------------
        private readonly ObservableCollection<KeyValuePair<string, string>> _devices = new(); // Discovered BLE devices
        private string? _connectedDevice; // UUID of currently connected BLE device
        private bool _isScanning = false; // Indicates if a scan is in progress

        // ----------------- UI Elements -----------------
        private Label _authLabel;
        private Label _bluetoothStatus;
        private ListView _devicesList;
        private Button _scanButton;

        private Label _stateLabel;        // Peripheral state label
        private Label _stateConfigLabel;  // Configuration state label
        private Label _ggaLabel;          // GGA NMEA messages
        private Label _gstLabel;          // GST NMEA messages
        private Label _txtLabel;          // TXT NMEA messages
        private Label _batteryLabel;      // Battery level
        private Label _versionLabel;      // Firmware version
        private Button _batteryButton;    // Request battery button
        private Button _versionButton;    // Request version button
        private Grid _scanSection;        // BLE scanning UI section
        private ConfigurationModal? _configModal; // Modal for showing configuration progress

        private readonly StackLayout _peripheralSection; // Container for peripheral UI

        // ----------------- NTRIP Management -----------------
        private readonly NtripConfigManager _ntripManager = new(); // Persistent NTRIP config manager
        private NtripConnectionInformation _connectionInfo = new();
        private NtripMountPoint? _selectedMountPoint;

        // NTRIP UI elements
        private Entry _hostEntry;
        private Entry _portEntry;
        private Entry _usernameEntry;
        private Entry _passwordEntry;
        private Button _getMountpointsButton;
        private Label _selectedMountLabel;
        private Button _connectNtripButton;
        private Button _reconnectNtripButton;
        private Button _disconnectNtripButton;
        private Label _ntripStateLabel;
        private Button _manageConfigsButton;

        /// <summary>
        /// Main page constructor: initializes UI, sets up Vigram SDK authentication.
        /// </summary>
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

            // Device list showing discovered BLE devices
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
                    label.SetBinding(Label.TextProperty, "Value"); // Display device name
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

            // Container for peripheral-related UI
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

            InitializeNtripSection(_peripheralSection);

            Content = new ScrollView
            {
                Content = mainLayout
            };

#if IOS
            // ----------------- Vigram SDK Authentication -----------------
            var auth = VigramSdk.AuthenticationService("YOUR_TOKEN");
            auth.Initialize(
                onSuccess: () =>
                {
                    _authLabel.Text = "✅ SDK Ready";
                },
                onError: message =>
                {
                    _authLabel.Text = $"❌ Auth Error: {message}";
                });
#endif
        }

        /// <summary>
        /// Called when the page appears: initialize modal and Bluetooth service.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _configModal = new ConfigurationModal(this);

#if IOS
            // Initialize Vigram Bluetooth service
            _bluetoothService = VigramSdk.BluetoothService();
#endif
        }

        #region Bluetooth Scanning

        /// <summary>
        /// Handles Start/Stop Scan button click.
        /// </summary>
        private void OnScanClicked(object? sender, EventArgs? e)
        {
            if (_bluetoothService == null) return;

            if (!_isScanning)
            {
                // Start scanning
                _devices.Clear();
                _bluetoothStatus.Text = "Scanning...";
                _scanButton.Text = "Stop Scan";
                _isScanning = true;

                _bluetoothService.StartScan(
                    onSuccess: () => _bluetoothStatus.Text = "Scanning...",
                    onError: error => _bluetoothStatus.Text = $"Scan failed: {error}"
                );

                // Subscribe to discovered devices
                _bluetoothService.ObserveDevices(
                    action: (uuid, name) =>
                    {
                        if (!_devices.Any(d => d.Key == uuid))
                            _devices.Add(new KeyValuePair<string, string>(uuid, name));
                    });
            }
            else
            {
                // Stop scanning
                _bluetoothService.StopScan();
                _bluetoothStatus.Text = "Scan stopped";
                _scanButton.Text = "Start Scan";
                _isScanning = false;
            }
        }

        /// <summary>
        /// Handles device selection from the list.
        /// Connects to the selected peripheral via Vigram SDK.
        /// </summary>
        private void OnDeviceTapped(object? sender, ItemTappedEventArgs e)
        {
            if (_bluetoothService == null || e.Item == null) return;

            var selected = (KeyValuePair<string, string>)e.Item;
            string deviceUuid = selected.Key;
            string deviceName = selected.Value;

            _bluetoothStatus.Text = $"Connecting to {deviceName} ({deviceUuid})...";

            _bluetoothService.Connect(deviceUuid,
            onSuccess: () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _connectedDevice = deviceUuid;
                    _bluetoothStatus.Text = $"✅ Connected to {deviceName}";
                    _scanSection.IsVisible = false;
                    _peripheralSection.IsVisible = true;

                    InitializePeripheralSection(deviceUuid);
                });
            },
            onError: message =>
            {
                _bluetoothStatus.Text = message;
            });
        }

        #endregion

        #region Peripheral Handling

        /// <summary>
        /// Initializes peripheral service and subscribes to state/config/NMEA updates.
        /// </summary>
        private void InitializePeripheralSection(string deviceUuid)
        {
#if IOS
            _peripheralService = VigramSdk.PeripheralService();
            _peripheralService.Start(deviceUuid);

            // Observe configuration state and show modal
            _peripheralService.ObserveConfigurationState((state, message) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    switch (state)
                    {
                        case ConfigurationState.InProgress:
                            _stateConfigLabel.Text = "Configuration state: progress...";
                            _configModal?.Show("Configuration in progress...");
                            break;
                        case ConfigurationState.Done:
                            _stateConfigLabel.Text = "Configuration state: Done";
                            _configModal?.UpdateMessage("Configuration completed successfully!");
                            Task.Delay(1000).ContinueWith(_ => _configModal?.Dismiss());
                            break;
                        case ConfigurationState.Failed:
                            _stateConfigLabel.Text = $"Configuration state: Failed: {message}";
                            _configModal?.UpdateMessage($"Configuration failed: {message}");
                            Task.Delay(2000).ContinueWith(_ => _configModal?.Dismiss());
                            break;
                        case ConfigurationState.PeripheralError:
                            _stateConfigLabel.Text = $"Configuration state: Peripheral error: {message}";
                            _configModal?.UpdateMessage($"Peripheral error: {message}");
                            Task.Delay(2000).ContinueWith(_ => _configModal?.Dismiss());
                            break;
                        default:
                            break;
                    }
                });
            });

            // Observe peripheral connection state
            _peripheralService.ObserveState(state =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    switch (state)
                    {
                        case CBPeripheralState.Unknown:
                            _stateLabel.Text = "State: Unknown";
                            break;
                        case CBPeripheralState.Disconnected:
                            _stateLabel.Text = "State: Disconnected";
                            _peripheralService?.Stop();
                            ResetToScanView();
                            break;
                        case CBPeripheralState.Connected:
                            _stateLabel.Text = "State: Connected";
                            _peripheralService.ObserveNmea(OnNmeaReceived);
                            break;
                    }
                });
            });
#endif
        }

        /// <summary>
        /// Handles incoming NMEA messages from the peripheral.
        /// Updates the UI accordingly.
        /// </summary>
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

        /// <summary>
        /// Requests battery status from peripheral.
        /// </summary>
        private void OnBatteryClicked(object? sender, EventArgs? e)
        {
            if (_peripheralService == null) return;

            _peripheralService.RequestBattery(battery =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _batteryLabel.Text = $"Battery: {battery}%";
                });
            });
        }

        /// <summary>
        /// Requests firmware version from peripheral.
        /// </summary>
        private void OnVersionClicked(object? sender, EventArgs? e)
        {
            if (_peripheralService == null) return;

            _peripheralService.RequestVersion(
                version =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _versionLabel.Text = $"Version:\nSoftware: {version.Soft}\nHardware: {version.Hard}";
                    });
                },
                error => _versionLabel.Text = error
            );
        }

        /// <summary>
        /// Resets the UI to show BLE scan section.
        /// </summary>
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

        /// <summary>
        /// Resets peripheral-related UI fields.
        /// </summary>
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
        #endregion

        #region NMEA Formatting Methods

        /// <summary>
        /// Formats GGA message data into a readable string for display.
        /// </summary>
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
                   $"Coordinate Longitude: {gga.CoordinateLongitude}";
        }

        /// <summary>
        /// Formats GST message data into a readable string for display.
        /// </summary>
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
                   $"Accuracy Vertical: {gst.AccuracyVertical}";
        }

        /// <summary>
        /// Formats TXT message data into a readable string for display.
        /// </summary>
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


        #region NTRIP UI & Logic

        /// <summary>
        /// Initializes NTRIP connection section in the UI, including host/port/user/pass entries,
        /// buttons for mountpoints, connect/reconnect/disconnect, and a state label.
        /// </summary>
        private void InitializeNtripSection(StackLayout parentLayout)
        {
#if IOS
            // Get instance of NTRIP service from Vigram SDK
            _ntripService = VigramSdk.NtripService();

            // Input fields for NTRIP connection parameters
            _hostEntry = new Entry { Placeholder = "Host", Keyboard = Keyboard.Url };
            _portEntry = new Entry { Placeholder = "Port", Keyboard = Keyboard.Numeric };
            _usernameEntry = new Entry { Placeholder = "Username" };
            _passwordEntry = new Entry { Placeholder = "Password" };

            // Button to fetch available mountpoints from NTRIP caster
            _getMountpointsButton = new Button { Text = "Get Mountpoints" };
            _getMountpointsButton.Clicked += OnGetMountpointsClicked;

            // Label to show selected mountpoint
            _selectedMountLabel = new Label { Text = "Selected Mount: -" };

            // Buttons to connect, reconnect, and disconnect from NTRIP
            _connectNtripButton = new Button { Text = "Connect to NTRIP" };
            _connectNtripButton.Clicked += OnConnectNtripClicked;

            _reconnectNtripButton = new Button { Text = "Reconnect" };
            _reconnectNtripButton.Clicked += (s, e) => _ntripService?.Reconnect();

            _disconnectNtripButton = new Button { Text = "Disconnect" };
            _disconnectNtripButton.Clicked += (s, e) => _ntripService?.Disconnect();

            // Label to display NTRIP connection state
            _ntripStateLabel = new Label { Text = "NTRIP State: -" };

            // Button to manage saved NTRIP configurations
            _manageConfigsButton = new Button
            {
                Text = "Manage NTRIP Configs",
                HorizontalOptions = LayoutOptions.Fill
            };
            _manageConfigsButton.Clicked += OnManageConfigsClicked;

            // Layout container for NTRIP section
            var ntripSection = new StackLayout
            {
                Padding = new Thickness(20, 10),
                Spacing = 8,
                Children =
        {
            new BoxView { HeightRequest = 1, Color = Colors.Gray },
            new Label
            {
                Text = "NTRIP Connection",
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                HorizontalOptions = LayoutOptions.Center
            },
            _hostEntry,
            _portEntry,
            _usernameEntry,
            _passwordEntry,
            _getMountpointsButton,
            _manageConfigsButton,
            _selectedMountLabel,
            _connectNtripButton,
            _reconnectNtripButton,
            _disconnectNtripButton,
            _ntripStateLabel
        }
            };

            parentLayout.Children.Add(ntripSection);
#endif
        }

        /// <summary>
        /// Handles "Get Mountpoints" button click.
        /// Requests available mountpoints from the NTRIP caster and shows a selection sheet.
        /// Updates selected mountpoint in UI and internal state.
        /// </summary>
        private async void OnGetMountpointsClicked(object? sender, EventArgs e)
        {
            if (_ntripService == null) return;

            // Build connection info from input fields
            _connectionInfo = new NtripConnectionInformation
            {
                Hostname = _hostEntry.Text ?? "",
                Port = int.TryParse(_portEntry.Text, out int p) ? p : 2101,
                Username = _usernameEntry.Text ?? "",
                Password = _passwordEntry.Text ?? ""
            };

            // Disable button while loading
            _getMountpointsButton.IsEnabled = false;
            _getMountpointsButton.Text = "Loading...";

            // Request mountpoints
            _ntripService.GetMountpoints(
                _connectionInfo,
                onSuccess: mounts =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        _getMountpointsButton.IsEnabled = true;
                        _getMountpointsButton.Text = "Get Mountpoints";

                        if (mounts == null || mounts.Count == 0)
                        {
                            await Application.Current?.MainPage?.DisplayAlert("NTRIP", "No mountpoints found.", "OK");
                            return;
                        }

                        // Let user select a mountpoint
                        string? selected = await Application.Current?.MainPage?.DisplayActionSheet(
                            "Select Mountpoint",
                            "Cancel",
                            null,
                            mounts.Select(m => m.Name).ToArray()
                        );

                        if (!string.IsNullOrEmpty(selected) && selected != "Cancel")
                        {
                            _selectedMountPoint = mounts.FirstOrDefault(m => m.Name == selected);
                            if (_selectedMountPoint != null)
                            {
                                _selectedMountLabel.Text = $"Selected Mount: {_selectedMountPoint.Name}";
                            }
                        }
                    });
                },
                onError: message =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        _getMountpointsButton.IsEnabled = true;
                        _getMountpointsButton.Text = "Get Mountpoints";

                        await Application.Current?.MainPage?.DisplayAlert("NTRIP Error", message ?? "Unknown error", "OK");
                    });
                }
            );
        }

        /// <summary>
        /// Handles "Connect to NTRIP" button click.
        /// Connects to the selected mountpoint and observes connection state.
        /// Saves the configuration for later use.
        /// </summary>
        private void OnConnectNtripClicked(object? sender, EventArgs e)
        {
            if (_ntripService == null || _selectedMountPoint == null) return;

            string mount = _selectedMountPoint.Name;

            // Save the configuration if it doesn't exist
            _ntripManager.AddIfNotExists(new NtripConnectionConfig
            {
                Hostname = _connectionInfo.Hostname,
                Port = _connectionInfo.Port,
                Username = _connectionInfo.Username,
                Password = _connectionInfo.Password,
                MountPoint = _selectedMountPoint.Name
            });

            // Start NTRIP task
            _ntripService.StartTask(_connectionInfo, mount,
            () =>
            {
                // Observe NTRIP state changes
                _ntripService.ObserveState((state, msg) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _ntripStateLabel.Text = $"NTRIP State: {state} ({msg})";
                    });
                });
            },
            error =>
            {
                _ntripStateLabel.Text = $"❌ Connection failed: {error ?? "Unknown error"}";
            });
        }

        /// <summary>
        /// Handles "Manage NTRIP Configs" button click.
        /// Shows saved configurations and allows the user to select one,
        /// updating the UI and internal state accordingly.
        /// </summary>
        private async void OnManageConfigsClicked(object? sender, EventArgs e)
        {
            await _ntripManager.ShowConfigSelector(this);
            var config = _ntripManager.SelectedConfig;
            if (config != null)
            {
                _hostEntry.Text = config.Hostname;
                _portEntry.Text = config.Port.ToString();
                _usernameEntry.Text = config.Username;
                _passwordEntry.Text = config.Password;
                _selectedMountLabel.Text = $"Selected Mount: {config.MountPoint}";
                _connectionInfo = new NtripConnectionInformation
                {
                    Hostname = config.Hostname,
                    Port = config.Port,
                    Username = config.Username,
                    Password = config.Password
                };
                _selectedMountPoint = new NtripMountPoint { Name = config.MountPoint };
            }
        }
        #endregion
    }
}