using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LaunchPage : Page, INotifyPropertyChanged
{
    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    #region [Properties]
    string fileToLaunch = @"Assets\Background.png";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    }

    private string _uriCommand = "";
    public string UriCommand
    {
        get => _uriCommand;
        set
        {
            if (_uriCommand != value)
            {
                _uriCommand = value;
                OnPropertyChanged();
            }
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

    // I have condensed this list, but there are more. The sampling is sorted by category.
    // https://www.itechtics.com/windows-uri-commands/
    public List<string> UriCommands = new() {
        "ms-settings:",
        "ms-settings:display",
        "ms-settings:nightlight",
        "ms-settings:batterysaver",
        "ms-settings:display-advanced",
        "ms-settings-connectabledevices:devicediscovery",
        "ms-settings:display-advancedgraphics",
        "ms-settings:sound",
        "ms-settings:sound-devices",
        "ms-settings:apps-volume",
        "ms-settings:notifications",
        "ms-settings:quiethours",
        "ms-settings:quietmomentshome",
        "ms-settings:quietmomentsscheduled",
        "ms-settings:quietmomentspresentation",
        "ms-settings:quietmomentsgame",
        "ms-settings:powersleep",
        "ms-settings:batterysaver",
        "ms-settings:batterysaver-usagedetails",
        "ms-settings:batterysaver-settings",
        "ms-settings:storagesense",
        "ms-settings:storagepolicies",
        "ms-settings:savelocations",
        "ms-settings:tabletmode",
        "ms-settings:multitasking",
        "ms-settings:project",
        "ms-settings:crossdevice",
        "ms-settings:clipboard",
        "ms-settings:remotedesktop",
        "ms-settings:deviceencryption",
        "ms-settings:about",
        "ms-settings:bluetooth",
        "ms-settings:connecteddevices",
        "ms-settings:printers",
        "ms-settings:mousetouchpad",
        "ms-settings:devices-touchpad",
        "ms-settings:typing",
        "ms-settings:devicestyping-hwkbtextsuggestions",
        "ms-settings:wheel",
        "ms-settings:pen",
        "ms-settings:autoplay",
        "ms-settings:usb",
        "ms-settings:camera",
        "ms-settings:mobile-devices",
        "ms-availablenetworks:",
        "ms-settings:network-wifisettings",
        "ms-settings:network-wificalling",
        "ms-settings:network-ethernet",
        "ms-settings:network-dialup",
        "ms-settings:network-directaccess",
        "ms-settings:network-vpn",
        "ms-settings:network-airplanemode",
        "ms-settings:proximity",
        "ms-settings:network-mobilehotspot",
        "ms-settings:nfctransactions",
        "ms-settings:network-proxy",
        "ms-settings:personalization",
        "ms-settings:personalization-background",
        "ms-settings:personalization-colors",
        "ms-settings:colors",
        "ms-settings:lockscreen",
        "ms-settings:themes",
        "ms-settings:fonts",
        "ms-settings:personalization-start",
        "ms-settings:personalization-start-places",
        "ms-settings:taskbar",
        "ms-settings:personalization-glance",
        "ms-settings:personalization-navbar",
        "ms-settings:appsfeatures",
        "ms-settings:optionalfeatures",
        "ms-settings:defaultapps",
        "ms-settings:maps",
        "ms-settings:maps-downloadmaps",
        "ms-settings:appsforwebsites",
        "ms-settings:videoplayback",
        "ms-settings:startupapps",
        "ms-settings:yourinfo",
        "ms-settings:emailandaccounts",
        "ms-settings:signinoptions",
        "ms-settings:signinoptions-launchfaceenrollment",
        "ms-settings:signinoptions-launchfingerprintenrollment",
        "ms-settings:signinoptions-launchsecuritykeyenrollment",
        "ms-settings:signinoptions-dynamiclock",
        "ms-settings:workplace",
        "ms-settings:otherusers",
        "ms-settings:family-group",
        "ms-settings:assignedaccess",
        "ms-settings:sync",
        "ms-settings:dateandtime",
        "ms-settings:regionlanguage",
        "ms-settings:regionlanguage-setdisplaylanguage",
        "ms-settings:regionlanguage-adddisplaylanguage",
        "ms-settings:speech",
        "ms-settings:gaming-gamebar",
        "ms-settings:gaming-gamedvr",
        "ms-settings:gaming-gamemode",
        "ms-settings:gaming-trueplay",
        "ms-settings:easeofaccess-display",
        "ms-settings:easeofaccess-cursorandpointersize",
        "ms-settings:easeofaccess-MousePointer",
        "ms-settings:easeofaccess-cursor",
        "ms-settings:easeofaccess-magnifier",
        "ms-settings:easeofaccess-colorfilter",
        "ms-settings:easeofaccess-highcontrast",
        "ms-settings:easeofaccess-narrator",
        "ms-settings:easeofaccess-narrator-isautostartenabled",
        "ms-settings:easeofaccess-audio",
        "ms-settings:easeofaccess-closedcaptioning",
        "ms-settings:easeofaccess-speechrecognition",
        "ms-settings:easeofaccess-keyboard",
        "ms-settings:easeofaccess-mouse",
        "ms-settings:easeofaccess-eyecontrol",
        "ms-settings:easeofaccess-visualeffects",
        "ms-settings:privacy",
        "ms-settings:privacy-accessoryapps",
        "ms-settings:privacy-advertisingid",
        "ms-settings:privacy",
        "ms-settings:privacy-speech",
        "ms-settings:privacy-speechtyping",
        "ms-settings:privacy-feedback",
        "ms-settings:privacy-feedback-telemetryviewergroup",
        "ms-settings:privacy-activityhistory",
        "ms-settings:privacy-location",
        "ms-settings:privacy-webcam",
        "ms-settings:privacy-microphone",
        "ms-settings:privacy-voiceactivation",
        "ms-settings:privacy-notifications",
        "ms-settings:privacy-accountinfo",
        "ms-settings:privacy-contacts",
        "ms-settings:privacy-calendar",
        "ms-settings:privacy-phonecalls",
        "ms-settings:privacy-callhistory",
        "ms-settings:privacy-email",
        "ms-settings:privacy-eyetracker",
        "ms-settings:privacy-tasks",
        "ms-settings:privacy-messaging",
        "ms-settings:privacy-radios",
        "ms-settings:privacy-customdevices",
        "ms-settings:privacy-backgroundapps",
        "ms-settings:privacy-appdiagnostics",
        "ms-settings:privacy-automaticfiledownloads",
        "ms-settings:privacy-documents",
        "ms-settings:privacy-downloadsfolder",
        "ms-settings:privacy-pictures",
        "ms-settings:privacy-documents",
        "ms-settings:privacy-broadfilesystemaccess",
        "ms-settings:windowsupdate",
        "ms-settings:windowsupdate-action",
        "ms-settings:windowsupdate-optionalupdates",
        "ms-settings:windowsupdate-activehours",
        "ms-settings:windowsupdate-history",
        "ms-settings:windowsupdate-restartoptions",
        "ms-settings:windowsupdate-options",
        "ms-settings:delivery-optimization",
        "ms-settings:windowsdefender",
        "windowsdefender:",
        "ms-settings:backup",
        "ms-settings:troubleshoot",
        "ms-settings:recovery",
        "ms-settings:activation",
        "ms-settings:findmydevice",
        "ms-settings:developers",
        "ms-settings:windowsinsider",
        "ms-settings:holographic",
        "ms-settings:holographic-audio",
        "ms-settings:privacy-holographic-environment",
        "ms-settings:holographic-headset",
        "ms-settings:holographic-management",
        "ms-settings:surfacehub-accounts",
        "ms-settings:surfacehub-calling",
        "ms-settings:surfacehub-devicemanagenent",
        "ms-settings:surfacehub-sessioncleanup",
        "ms-settings:surfacehub-welcome",
    };
    #endregion

    public LaunchPage()
    {
        this.InitializeComponent();
        cbUris.ItemsSource = UriCommands;
        cbUris.SelectedIndex = 0;
        url.Text = "https://learn.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app#accounts";
        this.Loaded += (_, _) => 
        { 
            ttSettings.IsOpen = true;

            //using (var file = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), "Cleaned.txt")))
            //{
            //    foreach (var name in UriCommands)
            //    {
            //        file.WriteLine($"\"{name.Trim()}\",");
            //    }
            //    file.Flush();
            //}
        };
    }

    /// <summary>
    /// Handle any parameter passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is SystemState sys)
        {
            Debug.WriteLine($"You sent '{sys.Title}'");
            landing.Text = $"I'm on page {sys.Title}";
            PostMessageEvent?.Invoke(this, new Message
            {
                Content = $"OnNavigatedTo ⇨ {sys.Title}",
                Severity = InfoBarSeverity.Informational,
            });
        }
        else
        {
            Debug.WriteLine($"Parameter is not of type '{nameof(SystemState)}'");
            landing.Text = $"Parameter is not of type '{nameof(SystemState)}'";
        }
        base.OnNavigatedTo(e);
    }

	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app
	/// </summary>
	async void Random_Click(object sender, RoutedEventArgs e)
    {
        IsBusy = true;
        try
        {
			var str = UriCommands[Random.Shared.Next(0, UriCommands.Count)];
			var uri = new Uri(str);
			landing.Text = $"Trying '{uri}'";
            // Next, configure the warning prompt.
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            await Task.Delay(1500);
        }
        catch (Exception ex)
		{
			landing.Text = ex.Message;
		}

        IsBusy = false;
    }

	async void Selected_Click(object sender, RoutedEventArgs e)
	{
        IsBusy = true;
		try
		{
			if (string.IsNullOrEmpty(UriCommand))
			{
				landing.Text = $"Pick a uri first.";
				return;
			}
			var uri = new Uri(UriCommand);
			landing.Text = $"Trying '{uri}'";
			var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            await Task.Delay(1500);
        }
		catch (Exception ex)
		{
			landing.Text = ex.Message;
		}
        IsBusy = false;
    }

	void LaunchDisplaySettings()
	{
		var uri = new Uri("ms-settings:display");
		var success = Windows.System.Launcher.LaunchUriAsync(uri).AsTask().Result;
	}
	async Task LaunchDisplaySettingsAsync()
	{
		var uri = new Uri("ms-settings:display");
		var success = await Windows.System.Launcher.LaunchUriAsync(uri);
	}

    /// <summary>
    /// Launch a .png file that came with the package.
    /// Show an Open With dialog that lets the user chose the handler to use.
    /// You could also try <see cref="System.Diagnostics.Process.Start"/>.
    /// </summary>
    async void LaunchFileOpenWith(object sender, RoutedEventArgs e)
    {
        // First, get the image file from the package's image directory.
        var file = await GetFileToLaunch();

        // Calculate the position for the Open With dialog.
        // An alternative to using the point is to set the rect of the UI element that triggered the launch.
        Point openWithPosition = MainWindow.GetElementLocation(sender);
        
        // Next, configure the Open With dialog.
        var options = new LauncherOptions();
        options.DisplayApplicationPicker = true;
        options.UI.InvocationPoint = openWithPosition;
        options.UI.PreferredPlacement = Windows.UI.Popups.Placement.Below;
        options.TreatAsUntrusted = false; // Configures the warning prompt (setting this to true will show the "Did you mean to switch apps?" popup)

        // Finally, launch the file.
        bool success = await Launcher.LaunchFileAsync(file, options);
        if (success)
           landing.Text = $"'{file.Name}' file was launched.";
        else
           landing.Text = $"'{file.Name}' file failed to launch.";
    }

    /// <summary>
    /// Helper for returning a <see cref="StorageFile"/>.
    /// </summary>
    async Task<StorageFile> GetFileToLaunch()
    {
        if (App.IsPackaged)
        {
            // First, get the image file from the package's image directory.
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(fileToLaunch);
            //Can't launch files directly from install folder so copy it over to temporary folder first
            file = await file.CopyAsync(ApplicationData.Current.TemporaryFolder, "Background.png", NameCollisionOption.ReplaceExisting);
            return file;
        }
        else
        {
            var file = await StorageFile.GetFileFromPathAsync(Path.Combine(Directory.GetCurrentDirectory(), fileToLaunch));
            return file;
        }
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/uwp/launch-resume/launch-ringtone-picker
    /// </summary>
    async Task RetrieveTone(string token)
	{
		// Retrieve a tone token from Contact.RingToneToken and display its friendly name in the contact card.
		using (var connection = new Windows.ApplicationModel.AppService.AppServiceConnection())
		{
			connection.AppServiceName = "ms-tonepicker-nameprovider";
			connection.PackageFamilyName = "Microsoft.Tonepicker_8wekyb3d8bbwe";
			Windows.ApplicationModel.AppService.AppServiceConnectionStatus connectionStatus = await connection.OpenAsync();
			if (connectionStatus == Windows.ApplicationModel.AppService.AppServiceConnectionStatus.Success)
			{
				var message = new ValueSet()
				{
					{ "Action", "GetToneName" },
					{ "ToneToken", token }
				};
				Windows.ApplicationModel.AppService.AppServiceResponse response = await connection.SendMessageAsync(message);
				if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
				{
					Int32 resultCode = (Int32)response.Message["Result"];
					if (resultCode == 0)
					{
						// Get the tone's friendly name.
						string? name = response.Message["DisplayName"] as string;
					}
					else
					{
						// handle failure
						switch (resultCode)
						{
							case 7:
								// Incorrect parameter (for example, no ToneToken provided).
								break;
							case 9:
								// Error reading the name for the specified token.
								break;
							case 10:
								// Unable to find specified tone token.
								break;
						}

					}
				}
				else
				{
					// handle failure
				}
			}
		}
	}

	/// <summary>
	/// Traditional test.
	/// </summary>
	void OSK_Click(object sender, RoutedEventArgs e)
    {
        CloseKeyboard();

        var ctrl = e.OriginalSource as Control;
        if (ctrl != null)
        {
            var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            var path = Path.Combine(common, @"Microsoft Shared\ink\TabTip.exe");
            if (!File.Exists(path))
                path = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\osk.exe";

            System.Diagnostics.Process.Start(path);
            ctrl.Focus(FocusState.Programmatic);
        }
    }

	/// <summary>
	/// Traditional test.
	/// </summary>
	void Snip_Click(object sender, RoutedEventArgs e)
    {
        //check for snipping tool
        var uri1 = new Uri("ms-screenclip:");
        var available1 = Windows.System.Launcher.QueryUriSupportAsync(uri1, LaunchQuerySupportType.UriForResults, "Microsoft.ScreenSketch_8wekyb3d8bbwe").AsTask().Result;
        if (available1 == LaunchQuerySupportStatus.Available)
        {
            Debug.WriteLine($"{uri1} is available");
        }
        else
        {
            Debug.WriteLine(@$"{available1}. Try 'C:\WINDOWS\system32\SnippingTool.exe'");
        }

        //check for snipping tool
        var uri2 = new Uri("ms-ScreenSketch:");
        var available2 = Windows.System.Launcher.QueryUriSupportAsync(uri2, LaunchQuerySupportType.UriForResults, "Microsoft.ScreenSketch_8wekyb3d8bbwe").AsTask().Result;
        if (available2 == LaunchQuerySupportStatus.Available)
        {
            Debug.WriteLine($"{uri2} is available");
        }
        else
        {
            Debug.WriteLine(@$"{available2}. Try 'C:\WINDOWS\system32\SnippingTool.exe'");
        }

        // https://learn.microsoft.com/en-us/windows/uwp/launch-resume/launch-ringtone-picker
        var uri3 = new Uri("ms-tonepicker:");
        var available3 = Windows.System.Launcher.QueryUriSupportAsync(uri2, LaunchQuerySupportType.UriForResults, "Microsoft.Tonepicker_8wekyb3d8bbwe").AsTask().Result;
        if (available3 == LaunchQuerySupportStatus.Available)
        {
            Debug.WriteLine($"{uri3} is available");
            LauncherOptions options = new LauncherOptions();
            options.TargetApplicationPackageFamilyName = "Microsoft.Tonepicker_8wekyb3d8bbwe";
            ValueSet inputData = new ValueSet() {
                { "Action", "PickRingtone" },
                { "TypeFilter", "Ringtones" } // Show only ringtones
            };
            LaunchUriResult result = Launcher.LaunchUriForResultsAsync(new Uri("ms-tonepicker:"), options, inputData).AsTask().Result;
            if (result.Status == LaunchUriStatus.Success)
            {
                Int32 resultCode = (Int32)result.Result["Result"];
                /*
                    0-success.
                    1-cancelled by user.
                    2-Invalid file.
                    3-Invalid file content type.
                    4-file exceeds maximum ringtone size (1MB in Windows 10).
                    5-File exceeds 40 second length limit.
                    6-File is protected by digital rights management.
                    7-invalid parameters.
                */
                if (resultCode == 0)
                {
                    string? token = result.Result["ToneToken"] as string;
                    string? name = result.Result["DisplayName"] as string;
                }
                else
                {
                    switch (resultCode)
                    {
                        case 1:
                            // Cancelled by the user
                            break;
                        case 2:
                            // The specified file was invalid
                            break;
                        case 3:
                            // The specified file's content type is invalid
                            break;
                        case 4:
                            // The specified file was too big
                            break;
                        case 5:
                            // The specified file was too long
                            break;
                        case 6:
                            // The file was protected by DRM
                            break;
                        case 7:
                            // The specified parameter was incorrect
                            break;
                    }
                }
            }
        }
    }

    void CloseKeyboard()
    {
        int iHandle = FindWindow("IPTIP_Main_Window", "");
        if (iHandle > 0)
            SendMessage(iHandle, WM_SYSCOMMAND, SC_CLOSE, 0);
    }

    #region [AutoSuggest]
    void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        DisplaySuggestions(sender);
    }

    void AutoSuggestBox_TypingPaused(object sender, EventArgs e)
    {
        DisplaySuggestions(sender as AutoSuggestBox);
    }

    void DisplaySuggestions(AutoSuggestBox? sender)
    {
        if (sender == null)
            return;

        IsBusy = true;
        var suitableItems = new List<string>();
        var splitText = sender.Text.ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in UriCommands)
        {
            // LINQ "splitText.All(Func<string, bool>)"
            var found = splitText.All((key) => { return name.Contains(key, StringComparison.OrdinalIgnoreCase); });
            if (found)
                suitableItems.Add(name);
        }

        if (suitableItems.Count == 0)
            suitableItems.Add("No results found");

        sender.ItemsSource = suitableItems;
        IsBusy = false;
    }

    async void asbSetting_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var selected = $"{args.SelectedItem}";
        IsBusy = true;
        try
        {
            if (string.IsNullOrEmpty(selected))
            {
                landing.Text = $"Pick a valid setting.";
                return;
            }
            var uri = new Uri(selected);
            landing.Text = $"Trying '{uri}'";
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            await Task.Delay(1500);
        }
        catch (Exception ex)
        {
            landing.Text = ex.Message;
        }
        IsBusy = false;
    }
    #endregion

    #region [DLL Imports]
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_CLOSE = 0xF060;
    private const int SC_MINIMIZE = 0xF020;

    [DllImport("user32.dll")]
    public static extern int FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);
    #endregion
}
