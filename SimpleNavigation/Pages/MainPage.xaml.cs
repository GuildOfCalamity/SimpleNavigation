using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using static System.Collections.Specialized.BitVector32;

namespace SimpleNavigation;

/// <summary>
/// Our main hub page for navigation and content.
/// </summary>
public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    #region [Properties]
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    }

    private string _imgPath = "/Assets/gear1.png";
    public string ImgPath
    {
        get => _imgPath;
        set
        {
            _imgPath = value;
            OnPropertyChanged();
        }
    }

    ObservableCollection<Message> _msgs = new();
    #endregion

    public MainPage()
    {
        this.InitializeComponent();

		// We can setup keyboard events in a number of ways…
		// 1) Add a generic handler event:
		this.AddHandler(KeyDownEvent, new KeyEventHandler(PressedKey), true);
        // 2) Add a keyboard accelerator event:
        this.ProcessKeyboardAccelerators += MainPage_ProcessKeyboardAccelerators;
        // 3) Build a keyboard accelerator:
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

        this.Loaded += MainPage_Loaded;

        #region [Setup page-wide messaging]
        HomePage.PostMessageEvent += MainPage_PostMessageEvent;
        ImagesPage.PostMessageEvent += MainPage_PostMessageEvent;
        NextPage.PostMessageEvent += MainPage_PostMessageEvent;
        SearchPage.PostMessageEvent += MainPage_PostMessageEvent;
        SettingsPage.PostMessageEvent += MainPage_PostMessageEvent;
        TestPage.PostMessageEvent += MainPage_PostMessageEvent;
        BluetoothPage.PostMessageEvent += MainPage_PostMessageEvent;
		#endregion
	}

    void MainPage_PostMessageEvent(object? sender, Message msg) => ShowMessage(msg.Content, msg.Severity);

    void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        // We're starting off by selecting the "Home" page.
        // We could let it ride on the MainPage, but we're using the
        // MainPage as a root/hub for any subsequent navigation events.
        SetStartPage(rbHome);

        if (App.AnimationsEffectsEnabled)
            StoryboardPath.Begin();

        // An InfoBar can also have content extend below the Message area.
        //InfoBarItemsRepeater.ItemsSource = _msgs;
    }

    void ShowMessage(string message, InfoBarSeverity severity)
    {
        infoBar.DispatcherQueue.TryEnqueue(() =>
        {
            infoBar.IsOpen = true;
            infoBar.Severity = severity;
            infoBar.Message = $"{message}";
            
            // If using the ItemsRepeater in the InfoBar.
            _msgs.Add(new Message { Content = $"{message}", Severity = severity });
        });
    }

    /// <summary>
    /// Updates the app theme.
    /// </summary>
    public async Task SetRequestedThemeAsync(ElementTheme theme)
    {
        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
            TitleBarHelper.UpdateTitleBar(theme);
            // Flyout themes do not seem to update immeadiately like other child objects
            // bound to the root element, so we'll manually force the theme change here.
            SettingsPanel.RequestedTheme = theme;
        }
        await Task.CompletedTask;
    }

    #region [Simple Page Navigation]
    void SetStartPage(RadioButton? rb)
    {
        if (rb == null)
        {
            SetCurrentPage(typeof(HomePage));
            return;
        }

		rb.IsChecked = true;
		RadioButton_Click(rb, new RoutedEventArgs());
	}

	void RadioButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
			if (MainFrame.Content != null && MainFrame.Content is Page pg)
               Debug.WriteLine($"BaseUri is '{pg.BaseUri}'");

            pageTitle.Text = $"{((RadioButton)sender).Content}".Replace("Page","");
            
            // This is unnecessary, but I'm adding this as a way to show how one
            // could pass more complex data objects between pages during navigation.
            App.State.LastUpdate = DateTime.Now;
            App.State.PageType = Type.GetType($"{((RadioButton)sender).Tag}");
            App.State.Title = pageTitle.Text;
            App.State.CurrentTheme = MainFrame.ActualTheme;

            // Navigate to the page with optional params.
            MainFrame.Navigate(App.State.PageType, App.State);
            
            // If you don't want to pass an object between pages, you could call the basic Navigate.
            //MainFrame.Navigate(Type.GetType($"{((RadioButton)sender).Tag}"));
           
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Optional helper method
    /// </summary>
    /// <param name="type">the page type to navigate to</param>
    public void SetCurrentPage(Type type)
    {
        try
        {
            MainFrame.Navigate(type);
        }
			catch (Exception ex)
			{
				Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
			}
		}

    void MainFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        Debug.WriteLine($"Failed to load page '{e.SourcePageType.FullName}'");
        App.ShowMessageBox("Navigation", $"Failed to load page '{e.SourcePageType.FullName}'", "OK", "Cancel", null, null);
    }

    void MainFrame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        Debug.WriteLine($"Navigating to page '{e.SourcePageType.FullName}'");
    }

    void MainFrame_Navigated(object sender, NavigationEventArgs e)
    {
        Debug.WriteLine($"Navigated to page '{e.SourcePageType.FullName}'");
    }

    void MainFrame_NavigationStopped(object sender, NavigationEventArgs e)
    {
        Debug.WriteLine($"Navigation stopped for page '{e.SourcePageType.FullName}'");
    }

		#endregion

	#region [Control Events]
    async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender != null && sender is HyperlinkButton hlb) 
        {
            await Extensions.LocateAndLaunchUrlFromString($"{hlb.Tag}");
        }
    }

    void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (sender != null && sender is RadioButton rb)
        {
            Debug.WriteLine($"User selected '{rb.Tag}'");
            ImgPath = $"/Assets/{rb.Tag}";
        }
    }

    async void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender != null && sender is RadioButton rb)
        {
            Debug.WriteLine($"User selected '{rb.Tag}'");
            if ($"{rb.Tag}".StartsWith("dark", StringComparison.OrdinalIgnoreCase))
            {
                await SetRequestedThemeAsync(ElementTheme.Dark);
            }
            else if ($"{rb.Tag}".StartsWith("light", StringComparison.OrdinalIgnoreCase))
            {
                await SetRequestedThemeAsync(ElementTheme.Light);
            }
            else
            {
                await SetRequestedThemeAsync(ElementTheme.Default);
            }
        }
    }
    #endregion

    #region [Keyboard Events]
    void PressedKey(object sender, KeyRoutedEventArgs e) => Debug.WriteLine($">> [{e.Key}] key was pressed <<");

    void MainPage_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        Debug.WriteLine($"[ProcessKeyboardAccelerators] {args.Modifiers} {args.Key}");
        if (args.Key == VirtualKey.B && (args.Modifiers == VirtualKeyModifiers.Menu || args.Modifiers == VirtualKeyModifiers.Control))
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
            
            args.Handled = true;
        }
        else if (args.Key == VirtualKey.D && (args.Modifiers == VirtualKeyModifiers.Menu || args.Modifiers == VirtualKeyModifiers.Control))
        {
            if (!App.DebugMode)
                App.DebugMode = true;
            else
                App.DebugMode = false;

            args.Handled = true;
        }
    }

    KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        KeyboardAccelerator? keyboardAccelerator = new() { Key = key };

        if (modifiers.HasValue)
            keyboardAccelerator.Modifiers = modifiers.Value;

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
        return keyboardAccelerator;
    }

    void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        Debug.WriteLine($"KeyboardAccelerator => {args.KeyboardAccelerator.Key}");
        if (MainFrame.CanGoBack)
            MainFrame.GoBack();
        args.Handled = true;
    }
    #endregion
}

/// <summary>
/// Support class for method invoking directly from the XAML.
/// This could be done using converters, but I like to show different techniques offering the same result.
/// </summary>
public static class AssemblyHelper
{
    /// <summary>
    /// Return the declaring type's version.
    /// </summary>
    /// <remarks>Includes string formatting.</remarks>
    public static string GetVersion()
    {
        var ver = App.GetCurrentAssemblyVersion();
        return $"Version {ver}";
    }

    /// <summary>
    /// Return the declaring type's namespace.
    /// </summary>
    public static string? GetNamespace()
    {
        var assembly = App.GetCurrentNamespace();
        return assembly ?? "WinUI3";
    }

    /// <summary>
    /// Return the declaring type's assembly name.
    /// </summary>
    public static string? GetAssemblyName()
    {
        var assembly = App.GetCurrentAssemblyName()?.Split(',')[0].SeparateCamelCase();
        return assembly ?? "WinUI3";
    }

    /// <summary>
    /// Returns <see cref="DateTime.Now"/> in a long format, e.g. "Wednesday, August 30, 2023"
    /// </summary>
    /// <remarks>Includes string formatting.</remarks>
    public static string GetFormattedDate()
    {
        return String.Format("{0:dddd, MMMM d, yyyy}", DateTime.Now);
    }
}

