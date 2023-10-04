using System;
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
using Microsoft.UI.Xaml.Navigation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class BluetoothPage : Page, INotifyPropertyChanged
{
    #region [Properties]
    readonly object _locker = new();

    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
			// ⇦ ⇨ ⇧ ⇩  🡐 🡒 🡑 🡓  🡄 🡆 🡅 🡇  http://xahlee.info/comp/unicode_arrows.html
			Debug.WriteLine($"You sent '{sys.Title}'");
			PostMessageEvent?.Invoke(this, new Message
			{
				Content = $"OnNavigatedTo ⇨ {sys.Title}",
				Severity = InfoBarSeverity.Informational,
			});
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
                                var index = icoPath.Split(',')[1].Replace("-","");
                                icoPath = $"/Assets/ico{index}.ico";
                            }

                            // It's rare that we would access this resource at the same time from the other
                            // thread, but it's good practice to use a Monitor.Enter() or a locking object.
                            lock (_locker)
                            {
                                collection.Add(new BTDevice
                                {
                                    Id = $"{devices[i].Id}",
                                    Name = $"{(string.IsNullOrEmpty(devices[i].Name) ? "No Name Available" : devices[i].Name)}",
                                    IsPaired = $"IsPaired: {devices[i].Pairing.IsPaired}",
                                    Kind = $"{devices[i].Kind}",
                                    IconPath = $"{icoPath}"
                                });
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
                rootGrid.DispatcherQueue.TryEnqueue(() => { Status = $"1st search {ts.Status}"; });
            });
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
                                var index = icoPath.Split(',')[1].Replace("-", "");
                                icoPath = $"/Assets/ico{index}.ico";
                            }

                            var strID = $"{devices[i].Id}";
                            if (strID.Contains("bluetooth", StringComparison.OrdinalIgnoreCase))
                            {
                                lock (_locker)
                                {
                                    collection.Add(new BTDevice
                                    {
                                        Id = strID,
                                        Name = $"{(string.IsNullOrEmpty(devices[i].Name) ? "No Name Available" : devices[i].Name)}",
                                        IsPaired = $"IsPaired: {devices[i].Pairing.IsPaired}",
                                        Kind = $"{devices[i].Kind}",
                                        IconPath = $"{icoPath}"
                                    });
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
                rootGrid.DispatcherQueue.TryEnqueue(() => { Status = $"2nd search {ts.Status}"; });
            });
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
                        rootGrid.DispatcherQueue.TryEnqueue(() => { Items.Add(bt); });
                    }
                    else
                    {
                        var exists = Items.Select(b => b.Id).Where(b => b == bt.Id).Any();
                        if (!exists)
                            rootGrid.DispatcherQueue.TryEnqueue(() => { Items.Add(bt); });
                    }
                }

                await tsk2; // moderate search (will take longer)
                foreach (var bt in collection)
                {
                    if (Items.Count == 0)
                    {
                        rootGrid.DispatcherQueue.TryEnqueue(() => { Items.Add(bt); });
                    }
                    else
                    {
                        var exists = Items.Select(b => b.Id).Where(b => b == bt.Id).Any();
                        if (!exists)
                            rootGrid.DispatcherQueue.TryEnqueue(() => { Items.Add(bt); });
                    }
                }

                // Final check so we can show the user something.
                if (collection.Count == 0)
                {
                    // If you want to see what your base hardware is for wireless communication then run this script in PowerShell…
                    // PS> Get-ChildItem HKLM:\SYSTEM\CurrentControlSet\Enum\SWD\RADIO | foreach-object { $_ | Get-ItemProperty | Select-Object FriendlyName, LocationInformation }
                    rootGrid.DispatcherQueue.TryEnqueue(() =>
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
                        rootGrid.DispatcherQueue.TryEnqueue(() => { Items.Add(bt); });
                    }
                    else
                    {
                        var exists = Items.Select(b => b.Id).Where(b => b == bt.Id).Any();
                        if (!exists)
                            rootGrid.DispatcherQueue.TryEnqueue(() => { Items.Add(bt); });
                    }
                }
            }
            #endregion
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
    /// Our short BT search task.
    /// </summary>
    /// <returns><see cref="DeviceInformationCollection"/></returns>
    public async Task<DeviceInformationCollection?> GatherBasic()
    {
        try
        {
            DeviceInformationCollection? devices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
            return devices;
        }
        catch (Exception ex) 
        {
            Debug.WriteLine($"GatherBasic: {ex.Message}");
            return null; 
        }
    }

    /// <summary>
    /// Our long BT search task.
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
        catch (Exception ex) 
        {
            Debug.WriteLine($"GatherModerate: {ex.Message}");
            return null; 
        }
    }
}
