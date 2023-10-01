using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

	async void BluetoothPage_LoadedAsync(object sender, RoutedEventArgs e)
    {
        IsBusy = true;
        Status = $"Gathering devices…";
        try
        {
            Items.Clear();
            await Task.Delay(1250);
			DeviceInformationCollection? devices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
            if (devices != null && devices.Count > 0)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    Items.Add(new BTDevice
                    {
                        Id = $"{devices[i].Id}",
                        Name = $"{(string.IsNullOrEmpty(devices[i].Name) ? "No Name Available" : devices[i].Name)}",
                        IsPaired = $"IsPaired: {devices[i].Pairing.IsPaired}",
                        Kind = $"{devices[i].Kind}"
                    });
                }
            }
            else
            {
				Status = $"No paired devices found, searching further…";
				await Task.Delay(250);
				//string selector = BluetoothDevice.GetDeviceSelector(); // 'System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:="{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)'
				//DeviceInformationCollection? allDevices = await DeviceInformation.FindAllAsync(selector);
				string selector = "System.Devices.DevObjectType:=5";
				DeviceInformationCollection allDevices = await DeviceInformation.FindAllAsync(selector);
				if (allDevices != null && allDevices.Count > 0)
                {
					for (int i = 0; i < allDevices.Count; i++)
					{
						Items.Add(new BTDevice
						{
							Id = $"{allDevices[i].Id}",
							Name = $"{(string.IsNullOrEmpty(allDevices[i].Name) ? "No Name Available" : allDevices[i].Name)}",
							IsPaired = $"IsPaired: {allDevices[i].Pairing.IsPaired}",
							Kind = $"{allDevices[i].Kind}"
						});
					}
				}
				else
                {
                    Items.Add(new BTDevice
                    {
                        Id = $"",
                        Name = $"No Bluetooth devices were discovered",
                        IsPaired = $"",
                        Kind = $""
                    });
                }
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
