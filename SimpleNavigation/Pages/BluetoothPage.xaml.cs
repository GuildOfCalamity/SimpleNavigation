﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.UI.Core;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class BluetoothPage : Page, INotifyPropertyChanged
{
    #region [Properties]
    readonly static SlimLock _lock = SlimLock.Create();

    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    private bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    ObservableCollection<BTDevice> _items = new();
    public ObservableCollection<BTDevice> Items
    {
        get => _items;
        set
        {
            _items = value;
            OnPropertyChanged();
        }
    }
    #endregion

    public BluetoothPage()
    {
        this.InitializeComponent();
        this.Loaded += BluetoothPage_LoadedAsync;
    }

	/// <summary>
	/// Handle any parameter passed.
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		if (e.Parameter != null && e.Parameter is SystemState sys)
		{
			Debug.WriteLine($"You sent '{sys.Title}'");
			PostMessageEvent?.Invoke(this, new Message
			{
				Content = $"OnNavigatedTo ⇨ {sys.Title}",
				Severity = InfoBarSeverity.Informational,
			});
            // Test the event bus.
            sys.EventBus?.Publish("EventBusMessage", $"{DateTime.Now.ToLongTimeString()}");
        }
        else
		{
			Debug.WriteLine($"Parameter is not of type '{nameof(SystemState)}'");
		}
		base.OnNavigatedTo(e);
	}

    /// <summary>
    /// I went WAY overboard on this, but it became an interesting tangent.
    /// </summary>
	async void BluetoothPage_LoadedAsync(object sender, RoutedEventArgs e)
    {
        // We shouldn't start another search if the user
        // clicks away to another page and then back again.
        if (IsBusy || Items.Count > 0) 
            return;

        List<BTDevice> collection = new();
        IsBusy = true;
        Status = $"Gathering devices…";

        try
        {
            Items.Clear();
            await Task.Delay(1000);

            // We'll start two searches in parallel and update the UI accordingly.

            #region [short search]
            CancellationTokenSource cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Task tsk1 = Task.Run(async () =>
            {
                var devices = await GatherBasic();
                if (devices != null && devices.Count > 0)
                {
                    for (int i = 0; i < devices.Count; i++)
                    {
                        string? icoPath = "/Assets/Bluetooth.png";
                        try
                        {
                            // Get a reference to the DDORes.dll asset.
                            var props = devices[i].Properties;
                            icoPath = props["System.Devices.Icon"] as string;
                            if (!string.IsNullOrEmpty(icoPath))
                            {
                                // There are other ways of doing this.
                                var index = icoPath.Split(',')[1].Replace("-","");
                                icoPath = $"/Assets/ico{index}.ico";
                            }

                            try
                            {
                                // It's rare that we would access this resource at the same time from the other
                                // thread, but it's good practice to use a Monitor.Enter() or a locking object.
                                _lock.EnterWrite();
                                collection.Add(new BTDevice
                                {
                                    Id = $"{devices[i].Id}",
                                    Name = $"{(string.IsNullOrEmpty(devices[i].Name) ? "No Name Available" : devices[i].Name)}",
                                    IsPaired = $"IsPaired: {devices[i].Pairing.IsPaired}",
                                    Kind = $"{devices[i].Kind}",
                                    IconPath = $"{icoPath}"
                                });
                            }
                            finally
                            {
                                _lock.ExitWrite();
                            }
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine($"🡆 Failed to extract icon property index.");
                            icoPath = "/Assets/Bluetooth.png"; // default
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"🡆 No data from GatherBasic()");
                }
            }, cts1.Token).ContinueWith(ts =>
            {
                Debug.WriteLine($"🡆 1st task has '{ts.Status}'");
                // We should be on the main thread since we're specifying TaskScheduler.FromCurrentSynchronizationContext,
                // but just in case we'll use the DispatcherQueue.TryEnqueue() callback here.
                rootGrid.DispatcherQueue?.TryEnqueue(() => { Status = $"1st search {ts.Status}"; });
            }, TaskScheduler.FromCurrentSynchronizationContext());
            #endregion

            #region [long search]
            //Status = $"No paired/unpaired devices found, searching further…";
            CancellationTokenSource cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(220));
            Task tsk2 = Task.Run(async () =>
            {
                var devices = await GatherModerate();
                if (devices != null && devices.Count > 0)
                {
                    for (int i = 0; i < devices.Count; i++)
                    {
                        #region [List all KVPs associated with this device]
                        //var props = devices[i].Properties;
                        //Debug.WriteLine($"🡆 [{(string.IsNullOrEmpty(devices[i].Name) ? "No Name Available" : devices[i].Name)}] 🡄");
                        //foreach (var kvp in props)
                        //{
                        //    Debug.WriteLine($" 🡒 Key...: {kvp.Key}");
                        //    Debug.WriteLine($" 🡒 Value.: {(kvp.Value == null ? "null" : kvp.Value)}");
                        //}
                        #endregion

                        string? icoPath = "/Assets/Bluetooth.png";
                        try
                        {
							var props = devices[i].Properties;
							// Get a reference to the DDORes.dll asset.
							icoPath = props["System.Devices.Icon"] as string;
                            if (!string.IsNullOrEmpty(icoPath))
                            {
                                // There are other ways of doing this.
                                var index = icoPath.Split(',')[1].Replace("-", "");
                                icoPath = $"/Assets/ico{index}.ico";
                            }

                            var strID = $"{devices[i].Id}";
                            if (strID.Contains("bluetooth", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    // It's rare that we would access this resource at the same time from the other
                                    // thread, but it's good practice to use a Monitor.Enter() or a locking object.
                                    _lock.EnterWrite();
                                    collection.Add(new BTDevice
                                    {
                                        Id = strID,
                                        Name = $"{(string.IsNullOrEmpty(devices[i].Name) ? "No Name Available" : devices[i].Name)}",
                                        IsPaired = $"IsPaired: {devices[i].Pairing.IsPaired}",
                                        Kind = $"{devices[i].Kind}",
                                        IconPath = $"{icoPath}"
                                    });
                                }
                                finally
                                {
                                    _lock.ExitWrite();
                                }
                            }
                            else
                            {
                                // You can inspect the location \HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\SWD to see details about PnPs, SWDs and DAFWSDProviders (WSD: Web Services for Devices).
                                Debug.WriteLine($"🡆 Probably not an actual Bluetooth device ⇨ {strID}");
                            }
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine($"🡆 Failed to extract icon property index.");
                            icoPath = "/Assets/Bluetooth.png"; // default
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"🡆 No data from GatherModerate()");
                }
            }, cts2.Token).ContinueWith(ts =>
            {
                Debug.WriteLine($"🡆 2nd task has '{ts.Status}'");
                // We should be on the main thread since we're specifying TaskScheduler.FromCurrentSynchronizationContext,
                // but just in case we'll use the DispatcherQueue.TryEnqueue() callback here.
                rootGrid.DispatcherQueue?.TryEnqueue(() => { Status = $"2nd search {ts.Status}"; });
            }, TaskScheduler.FromCurrentSynchronizationContext());
            #endregion

            #region [handle results]
            try
            {
                // Wait for all tasks to finish.
                //await Task.WhenAll(tsk1, tsk2);

                await tsk1; // basic search (is normally quick)
                foreach (var bt in collection)
                {
                    if (Items.Count == 0)
                    {
                        rootGrid.DispatcherQueue?.TryEnqueue(() => { Items.Add(bt); });
                    }
                    else
                    {
                        var exists = Items.Select(b => b.Id).Where(b => b == bt.Id).Any();
                        if (!exists)
                            rootGrid.DispatcherQueue?.TryEnqueue(() => { Items.Add(bt); });
                    }
                }

                await tsk2; // moderate search (will take longer)
                foreach (var bt in collection)
                {
                    if (Items.Count == 0)
                    {
                        rootGrid.DispatcherQueue?.TryEnqueue(() => { Items.Add(bt); });
                    }
                    else
                    {
                        var exists = Items.Select(b => b.Id).Where(b => b == bt.Id).Any();
                        if (!exists)
                            rootGrid.DispatcherQueue?.TryEnqueue(() => { Items.Add(bt); });
                    }
                }

                // Final check so we can show the user something.
                if (collection.Count == 0)
                {
                    // To see what your base hardware is for wireless communication then run this script in PowerShell…
                    // PS> Get-ChildItem HKLM:\SYSTEM\CurrentControlSet\Enum\SWD\RADIO | foreach-object { $_ | Get-ItemProperty | Select-Object FriendlyName, LocationInformation }
                    rootGrid.DispatcherQueue?.TryEnqueue(() =>
                    {
                        Items.Add(new BTDevice
                        {
                            Id = $"",
                            Name = $"No Bluetooth devices were discovered",
                            IsPaired = $"",
                            Kind = $""
                        });
                    });
                }

                // We should be back on the UI thread at this point, so directly setting our properties is OK.
                Status = $"Gather complete";
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                Status = $"Search took too long";
                
                // See if we got anything else.
                foreach (var bt in collection)
                {
                    if (Items.Count == 0)
                    {
                        rootGrid.DispatcherQueue?.TryEnqueue(() => { Items.Add(bt); });
                    }
                    else
                    {
                        var exists = Items.Select(b => b.Id).Where(b => b == bt.Id).Any();
                        if (!exists)
                            rootGrid.DispatcherQueue?.TryEnqueue(() => { Items.Add(bt); });
                    }
                }
            }
            #endregion
        }
        catch (RuntimeWrappedException rwe) // catch any non-CLS exceptions
        {
            String? s = rwe.WrappedException as String;
            if (s != null)
                Debug.WriteLine($"BluetoothPage_LoadedAsync: {s}");
        }
        catch (Exception ex)
        {
			Status = $"{ex.Message}";
		}
		finally
        {
			IsBusy = false;
        }
	}

    /// <summary>
    /// Our short BluetoothLE search task.
    /// You could also use the <see cref="DeviceInformation.CreateWatcher()"/> to monitor for nearby devices.
    /// </summary>
    /// <returns><see cref="DeviceInformationCollection"/></returns>
    public async Task<DeviceInformationCollection?> GatherBasic()
    {
        try
        {
            DeviceInformationCollection? devices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
            return devices;
        }
        catch (RuntimeWrappedException rwe) // catch any non-CLS exceptions
        {
            String? s = rwe.WrappedException as String;
            if (s != null)
                Debug.WriteLine($"GatherBasic: {s}");
            return null;
        }
        catch (Exception ex) 
        {
            Debug.WriteLine($"GatherBasic: {ex.Message}");
            return null; 
        }
    }

    /// <summary>
    /// Our long BluetoothLE search task.
    /// You could also use the <see cref="DeviceInformation.CreateWatcher()"/> to monitor for nearby devices.
    /// </summary>
    /// <returns><see cref="DeviceInformationCollection"/></returns>
    public async Task<DeviceInformationCollection?> GatherModerate()
    {
        try
        {
            string selector = "System.Devices.DevObjectType:=5"; //  We'll only filter by the most basic category.
            DeviceInformationCollection? devices = await DeviceInformation.FindAllAsync(selector);
            return devices;
        }
        catch (RuntimeWrappedException rwe) // catch any non-CLS exceptions
        {
            String? s = rwe.WrappedException as String;
            if (s != null)
                Debug.WriteLine($"GatherModerate: {s}");
            return null;
        }
        catch (Exception ex) 
        {
            Debug.WriteLine($"GatherModerate: {ex.Message}");
            return null; 
        }
    }


    #region [Device discovery from UWP using a Watcher]
    private ObservableCollection<BluetoothLEDeviceDisplay> KnownDevices = new ObservableCollection<BluetoothLEDeviceDisplay>();
    private List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();
    private DeviceWatcher? deviceWatcher;

    /// <summary>
    /// Starts a device watcher that looks for all nearby Bluetooth devices (paired or unpaired). 
    /// Attaches event handlers to populate the device collection.
    /// </summary>
    private void StartBleDeviceWatcher()
    {
        // Additional properties we would like about the device.
        // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
        string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

        // BT_Code: Example showing paired and non-paired in a single query.
        string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

        deviceWatcher = DeviceInformation.CreateWatcher(
                    aqsAllBluetoothLEDevices,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);

        // Register event handlers before starting the watcher.
        deviceWatcher.Added += DeviceWatcher_Added;
        deviceWatcher.Updated += DeviceWatcher_Updated;
        deviceWatcher.Removed += DeviceWatcher_Removed;
        deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
        deviceWatcher.Stopped += DeviceWatcher_Stopped;

        // Start over with an empty collection.
        KnownDevices.Clear();

        // Start the watcher. Active enumeration is limited to approximately 30 seconds.
        // This limits power usage and reduces interference with other Bluetooth activities.
        // To monitor for the presence of Bluetooth LE devices for an extended period,
        // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
        // sample for an example.
        deviceWatcher.Start();
    }

    /// <summary>
    /// Stops watching for all nearby Bluetooth devices.
    /// </summary>
    void StopBleDeviceWatcher()
    {
        if (deviceWatcher != null)
        {
            // Unregister the event handlers.
            deviceWatcher.Added -= DeviceWatcher_Added;
            deviceWatcher.Updated -= DeviceWatcher_Updated;
            deviceWatcher.Removed -= DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped -= DeviceWatcher_Stopped;

            // Stop the watcher.
            deviceWatcher.Stop();
            deviceWatcher = null;
        }
    }

    BluetoothLEDeviceDisplay? FindBluetoothLEDeviceDisplay(string id)
    {
        foreach (BluetoothLEDeviceDisplay bleDeviceDisplay in KnownDevices)
        {
            if (bleDeviceDisplay.Id == id)
            {
                return bleDeviceDisplay;
            }
        }
        return null;
    }

    DeviceInformation? FindUnknownDevices(string id)
    {
        foreach (DeviceInformation bleDeviceInfo in UnknownDevices)
        {
            if (bleDeviceInfo.Id == id)
            {
                return bleDeviceInfo;
            }
        }
        return null;
    }

    async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
    {
        // We must update the collection on the UI thread because the collection is databound to a UI element.
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            lock (this)
            {
                Debug.WriteLine($"Added {deviceInfo.Id}{deviceInfo.Name}");

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    // Make sure device isn't already present in the list.
                    if (FindBluetoothLEDeviceDisplay(deviceInfo.Id) == null)
                    {
                        if (deviceInfo.Name != string.Empty)
                        {
                            // If device has a friendly name display it immediately.
                            KnownDevices.Add(new BluetoothLEDeviceDisplay(deviceInfo));
                        }
                        else
                        {
                            // Add it to a list in case the name gets updated later. 
                            UnknownDevices.Add(deviceInfo);
                        }
                    }

                }
            }
        });
    }

    async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
    {
        // We must update the collection on the UI thread because the collection is databound to a UI element.
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            lock (this)
            {
                Debug.WriteLine($"Updated {deviceInfoUpdate.Id} {deviceInfoUpdate.Kind}");

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    BluetoothLEDeviceDisplay? bleDeviceDisplay = FindBluetoothLEDeviceDisplay(deviceInfoUpdate.Id);
                    if (bleDeviceDisplay != null)
                    {
                        // Device is already being displayed - update UX.
                        bleDeviceDisplay.Update(deviceInfoUpdate);
                        return;
                    }

                    DeviceInformation? deviceInfo = FindUnknownDevices(deviceInfoUpdate.Id);
                    if (deviceInfo != null)
                    {
                        deviceInfo.Update(deviceInfoUpdate);
                        // If device has been updated with a friendly name it's no longer unknown.
                        if (deviceInfo.Name != String.Empty)
                        {
                            KnownDevices.Add(new BluetoothLEDeviceDisplay(deviceInfo));
                            UnknownDevices.Remove(deviceInfo);
                        }
                    }
                }
            }
        });
    }

    async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
    {
        // We must update the collection on the UI thread because the collection is databound to a UI element.
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            lock (this)
            {
                Debug.WriteLine($"Removed {deviceInfoUpdate.Id} {deviceInfoUpdate.Kind}");

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    // Find the corresponding DeviceInformation in the collection and remove it.
                    BluetoothLEDeviceDisplay? bleDeviceDisplay = FindBluetoothLEDeviceDisplay(deviceInfoUpdate.Id);
                    if (bleDeviceDisplay != null)
                    {
                        KnownDevices.Remove(bleDeviceDisplay);
                    }

                    DeviceInformation? deviceInfo = FindUnknownDevices(deviceInfoUpdate.Id);
                    if (deviceInfo != null)
                    {
                        UnknownDevices.Remove(deviceInfo);
                    }
                }
            }
        });
    }

    async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
    {
        // We must update the collection on the UI thread because the collection is databound to a UI element.
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {
                Debug.WriteLine($"{KnownDevices.Count} devices found. Enumeration completed.");
            }
        });
    }

    async void DeviceWatcher_Stopped(DeviceWatcher sender, object e)
    {
        // We must update the collection on the UI thread because the collection is databound to a UI element.
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {
                Debug.WriteLine($"No longer watching for devices: {sender.Status}");
            }
        });
    }
    #endregion
}

/// <summary>
/// Display class used to represent a BluetoothLEDevice in the device list.
/// </summary>
public class BluetoothLEDeviceDisplay : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public DeviceInformation DeviceInformation { get; private set; }
    public string Id => DeviceInformation.Id;
    public string Name => DeviceInformation.Name;
    public bool IsPaired => DeviceInformation.Pairing.IsPaired;
    public bool IsConnected => (bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true;
    public bool IsConnectable => (bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;
    public IReadOnlyDictionary<string, object> Properties => DeviceInformation.Properties;
    public BitmapImage GlyphBitmapImage { get; private set; }

    public BluetoothLEDeviceDisplay(DeviceInformation deviceInfoIn)
    {
        DeviceInformation = deviceInfoIn;
        UpdateGlyphBitmapImage();
    }

    public void Update(DeviceInformationUpdate deviceInfoUpdate)
    {
        DeviceInformation.Update(deviceInfoUpdate);

        OnPropertyChanged("Id");
        OnPropertyChanged("Name");
        OnPropertyChanged("DeviceInformation");
        OnPropertyChanged("IsPaired");
        OnPropertyChanged("IsConnected");
        OnPropertyChanged("Properties");
        OnPropertyChanged("IsConnectable");

        UpdateGlyphBitmapImage();
    }

    private async void UpdateGlyphBitmapImage()
    {
        DeviceThumbnail deviceThumbnail = await DeviceInformation.GetGlyphThumbnailAsync();
        var glyphBitmapImage = new BitmapImage();
        await glyphBitmapImage.SetSourceAsync(deviceThumbnail);
        GlyphBitmapImage = glyphBitmapImage;
        OnPropertyChanged("GlyphBitmapImage");
    }

}
