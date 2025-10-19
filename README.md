# VigramSDK
v1.0.0



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
auth.Initialize(success =>
{
    if (success)
        Console.WriteLine("Vigram SDK is ready");
    else
        Console.WriteLine("Authentication failed");
});
```

##Using the services
#### Bluetooth Service
```
var bluetooth = VigramSdk.BluetoothService();

// Start scanning devices
bluetooth.StartScan((uuid, name) =>
{
    Console.WriteLine($"Found device: {name} ({uuid})");
});

// Stop scanning
bluetooth.StopScan();
```
#### Peripheral Service
```
var peripheral = VigramSdk.PeripheralService();
peripheral.Start("DEVICE_UUID");

// Subscribe to connection state
peripheral.ObserveState(state =>
{
    if (state == CBPeripheralState.Connected)
        Console.WriteLine("Device connected");
    else if (state == CBPeripheralState.Disconnected)
        Console.WriteLine("Device disconnected");
});

// Request battery level
peripheral.RequestBattery(battery =>
{
    Console.WriteLine($"Battery: {battery}%");
});

// Request firmware version
peripheral.RequestVersion(version =>
{
    Console.WriteLine($"Firmware: {version}");
});
```



