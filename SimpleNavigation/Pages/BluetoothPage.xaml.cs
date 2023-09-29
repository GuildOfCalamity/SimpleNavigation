using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class BluetoothPage : Page, INotifyPropertyChanged
{
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
    public BluetoothPage()
    {
        this.InitializeComponent();
        this.Loaded += BluetoothPage_LoadedAsync;
    }

    async void BluetoothPage_LoadedAsync(object sender, RoutedEventArgs e)
    {
        IsBusy = true;
        Status = $"Gathering devices...";
        try
        {
            Items.Clear();
            await Task.Delay(1500);
            DeviceInformationCollection? devices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
            if (devices != null && devices.Count > 0)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    Items.Add(new BTDevice
                    {
                        Id = $"{devices[i].Id}",
                        Name = $"{devices[i].Name}",
                        IsPaired = $"IsPaired: {devices[i].Pairing.IsPaired}",
                        Kind = $"{devices[i].Kind}"
                    });
                }
            }
            else
            {
				Items.Add(new BTDevice
				{
					Id = $"",
					Name = $"No bluetooth devices were discovered",
					IsPaired = $"",
					Kind = $""
				});
			}
            Status = $"Gather complete";
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
}

public class BTDevice
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Kind { get; set; }
    public string IsPaired { get; set; }
}
