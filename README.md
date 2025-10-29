# VigramSDK
v1.1.0

###What’s New in This Version
- Added NTRIP Service for GNSS correction streams.
- Updated Bluetooth and Peripheral services with
- improved state and NMEA observation methods.
- Improved SDK initialization and device connection flow.


## Prepare the files

Copy all required files into the root folder of your project:

```
Libs/bridge.xcframework

Libs/VigramSDK.xcframework

Libs/VigramSDK.dll
```

---

## Add SDK to your project (.csproj)

Open your `.csproj` file and add the following items:

```
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0-ios'">
  <NativeReference Include="Libs/bridge.xcframework">
    <Kind>Framework</Kind>
    <ForceLoad>true</ForceLoad>
    <SmartLink>true</SmartLink>
    <FrameworksDirectory>Frameworks</FrameworksDirectory>
    <SupportedArchitectures>arm64;x86_64</SupportedArchitectures>
  </NativeReference>

  <NativeReference Include="Libs/VigramSDK.xcframework">
    <Kind>Framework</Kind>
    <ForceLoad>true</ForceLoad>
    <SmartLink>true</SmartLink>
    <FrameworksDirectory>Frameworks</FrameworksDirectory>
    <SupportedArchitectures>arm64;x86_64</SupportedArchitectures>
  </NativeReference>
</ItemGroup>

<ItemGroup>
  <Reference Include="VigramSDK">
    <HintPath>Libs/VigramSDK.dll</HintPath>
    <Private>true</Private>
  </Reference>
</ItemGroup>
```

## Initialize the SDK
Before using any services, initialize the SDK with your API token:

```
var auth = VigramSdk.AuthenticationService("YOUR_API_TOKEN");
auth.Initialize(
    onSuccess: ()=>
    {
    	Console.WriteLine("Vigram SDK ready");
    },
    onError: msg =>
    {
        Console.WriteLine($"Auth error: {msg}");
    }
);
```

##Using the services
#### Bluetooth Service
```
var bluetooth = VigramSdk.BluetoothService();

// Start scanning
bluetooth.StartScan(
    onSuccess: () => Console.WriteLine("Scanning..."),
    onError: msg => Console.WriteLine($"Scan failed: {msg}")
);

// Observe discovered devices
bluetooth.ObserveDevices((uuid, name) =>
{
    Console.WriteLine($"Found device: {name} ({uuid})");
});

// Stop scanning
bluetooth.StopScan();

// Connect to a device
bluetooth.Connect("DEVICE_UUID",
    onSuccess: () => Console.WriteLine("Connected"),
    onError: msg => Console.WriteLine($"Failed: {msg}")
);

```
#### Peripheral Service
```
var peripheral = VigramSdk.PeripheralService();
peripheral.Start("DEVICE_UUID");

// Observe connection state
peripheral.ObserveState(state =>
{
    Console.WriteLine($"Peripheral state: {state}");
});

// Observe configuration state
peripheral.ObserveConfigurationState((state, msg) =>
{
    Console.WriteLine($"Configuration: {state}, {msg}");
});

// Observe NMEA messages
peripheral.ObserveNmea(msg =>
{
    switch (msg)
    {
        case NmeaMessage.Gga gga:
            Console.WriteLine(gga.Data);
            break;
        case NmeaMessage.Gst gst:
            Console.WriteLine(gst.Data);
            break;
        case NmeaMessage.Txt txt:
            Console.WriteLine(txt.Data);
            break;
    }
});

// Request battery level
peripheral.RequestBattery(battery =>
{
    Console.WriteLine($"Battery: {battery}%");
});

// Request firmware version
peripheral.RequestVersion(version =>
{
    Console.WriteLine($"Software: {version.Soft}, Hardware: {version.Hard}");
});
```

#### NTRIP Service
```
var ntrip = VigramSdk.NtripService();

var connectionInfo = new NtripConnectionInformation
{
    Hostname = "ntrip.example.com",
    Port = 2101,
    Username = "user",
    Password = "pass"
};

// Get available mountpoints from the NTRIP caster
ntrip.GetMountpoints(connectionInfo,
    onSuccess: mounts =>
    {
        foreach (var m in mounts)
            Console.WriteLine($"Mountpoint: {m.Name}");
    },
    onError: msg => Console.WriteLine($"Error: {msg}")
);

// Connect to a selected mountpoint
ntrip.StartTask(connectionInfo, "MOUNT_NAME",
    onSuccess: () =>
    {
        // Observe NTRIP connection state
        ntrip.ObserveState((state, msg) =>
        {
            Console.WriteLine($"NTRIP state: {state} ({msg})");
        });
    },
    onError: msg => Console.WriteLine($"Connection failed: {msg}")
);

// Disconnect or reconnect
ntrip.Disconnect();
ntrip.Reconnect();

```

