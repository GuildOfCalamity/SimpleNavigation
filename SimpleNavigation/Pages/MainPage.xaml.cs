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

    /// <summary>
    /// A keyboard event that another page/model can subscribe to.
    /// </summary>
	public static event EventHandler<KeyboardInput>? MainPageKeyboardEvent;

	public MainPage()
    {
        this.InitializeComponent();
		this.Loaded += MainPage_Loaded;

		#region [Event Extras]
		// We can setup keyboard events in a number of ways…
		// 1) Add a generic handler event:
		this.AddHandler(KeyDownEvent, new KeyEventHandler(PressedKey), true);
        // 2) Add a keyboard accelerator event:
        this.ProcessKeyboardAccelerators += MainPage_ProcessKeyboardAccelerators;
        // 3) Build a keyboard accelerator:
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
        // Also, pointer events:
        this.AddHandler(PointerPressedEvent, new PointerEventHandler(PressedPointer), true);

		/*  [Other Delegates] https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.input?view=winrt-22621#delegates
            DoubleTappedEventHandler 	                Represents the method that will handle the DoubleTapped event.
            HoldingEventHandler 	                    Represents the method that will handle the Holding event.
            KeyEventHandler 	                        Represents the method that handles the KeyUp and KeyDown  events.
            ManipulationCompletedEventHandler 	        Represents the method that will handle ManipulationCompleted and related events.
            ManipulationDeltaEventHandler 	            Represents the method that will handle ManipulationDelta and related events.
            ManipulationInertiaStartingEventHandler 	Represents the method that will handle the ManipulationInertiaStarting event.
            ManipulationStartedEventHandler 	        Represents the method that will handle ManipulationStarted and related events.
            ManipulationStartingEventHandler 	        Represents the method that will handle the ManipulationStarting event.
            PointerEventHandler 	                    Represents the method that will handle pointer message events such as PointerPressed.
            RightTappedEventHandler                     Represents the method that will handle a RightTapped routed event.
            TappedEventHandler 	                        Represents the method that will handle the Tapped event.
         */

		//Extensions.DisplayRoutedEventsForFrameworkElement();

		// [Hierarchy] Page => UserControl => Control => FrameworkElement => UIElement => DependencyObject

		/** UIElement vs FrameworkElement Event Comparison **
		
        When using this.AddHandler() these are the additional events you can use:

           [UIElement]                 |  [FrameworkElement]
         ----------------------------------------------------------------
           AccessKeyDisplayDismissed   |  ActualThemeChanged
           AccessKeyDisplayRequested   |  DataContextChanged
           AccessKeyInvoked            |  EffectiveViewportChanged
           (n/a)                       |  LayoutUpdated
           (n/a)                       |  Loaded
           (n/a)                       |  Loading
           (n/a)                       |  SizeChanged
           (n/a)                       |  Unloaded
           (n/a)                       |  AccessKeyDisplayDismissed
           (n/a)                       |  AccessKeyDisplayRequested
           (n/a)                       |  AccessKeyInvoked
           BringIntoViewRequested      |  BringIntoViewRequested
           CharacterReceived           |  CharacterReceived
           ContextCanceled             |  ContextCanceled
           ContextRequested            |  ContextRequested
           DoubleTapped                |  DoubleTapped
           DragEnter                   |  DragEnter
           DragLeave                   |  DragLeave
           DragOver                    |  DragOver
           DragStarting                |  DragStarting
           Drop                        |  Drop
           DropCompleted               |  DropCompleted
           GettingFocus                |  GettingFocus
           GotFocus                    |  GotFocus
           Holding                     |  Holding
           KeyDown                     |  KeyDown
           KeyUp                       |  KeyUp
           LosingFocus                 |  LosingFocus
           LostFocus                   |  LostFocus
           ManipulationCompleted       |  ManipulationCompleted
           ManipulationDelta           |  ManipulationDelta
           ManipulationInertiaStarting |  ManipulationInertiaStarting
           ManipulationStarted         |  ManipulationStarted
           ManipulationStarting        |  ManipulationStarting
           NoFocusCandidateFound       |  NoFocusCandidateFound
           PointerCanceled             |  PointerCanceled
           PointerCaptureLost          |  PointerCaptureLost
           PointerEntered              |  PointerEntered
           PointerExited               |  PointerExited
           PointerMoved                |  PointerMoved
           PointerPressed              |  PointerPressed
           PointerReleased             |  PointerReleased
           PointerWheelChanged         |  PointerWheelChanged
           PreviewKeyDown              |  PreviewKeyDown
           PreviewKeyUp                |  PreviewKeyUp
           ProcessKeyboardAccelerators |  ProcessKeyboardAccelerators
           RightTapped                 |  RightTapped
           Tapped                      |  Tapped

           The parent type, Microsoft.UI.Xaml.Controls.Control, exposes 3 additional events:
            - FocusEngaged    
            - FocusDisengaged 
            - IsEnabledChanged

		 **************************************************/
		#endregion

        #region [Setup page-wide messaging]
        // These are only used for the InfoBar control.
        BluetoothPage.PostMessageEvent += MainPage_PostMessageEvent;
        HomePage.PostMessageEvent += MainPage_PostMessageEvent;
        ImagesPage.PostMessageEvent += MainPage_PostMessageEvent;
        LaunchPage.PostMessageEvent += MainPage_PostMessageEvent;
        NextPage.PostMessageEvent += MainPage_PostMessageEvent;
        PackagePage.PostMessageEvent += MainPage_PostMessageEvent;
        SearchPage.PostMessageEvent += MainPage_PostMessageEvent;
        SettingsPage.PostMessageEvent += MainPage_PostMessageEvent;
        TestPage.PostMessageEvent += MainPage_PostMessageEvent;
        #endregion

        #region [Superfluous]
        var test = typeof(RelayCommand).GetConstructors().FirstOrDefault(c => c.GetParameters().Length != 0);
        if (test != null)
            Debug.WriteLine($"'{nameof(RelayCommand)}' has a constructor which takes a parameter.");

        var mod = App.Attribs?.GetLoadedModules.FirstOrDefault();
        if (mod != null)
            Debug.WriteLine($"I can't live without this! 🡒 '{mod.FullyQualifiedName}'");
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
            // Flyout themes do not seem to update immediately like other child objects
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
            App.ShowMessageBox("Navigation", $"Failed to load page '{((RadioButton)sender).Tag}'", "OK", "Cancel", null, null);
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

	#region [Pointer Events]
	/// <summary>
	/// You will not see this event fire if clicking on a transparent background area.
	/// </summary>
	void PressedPointer(object sender, PointerRoutedEventArgs e) => Debug.WriteLine($"🡒 [{e.Pointer.PointerDeviceType}] was pressed <<");
	#endregion

	#region [Keyboard Events]
	void PressedKey(object sender, KeyRoutedEventArgs e) => Debug.WriteLine($"🡒 [{e.Key}] key was pressed <<");

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

		// Fire our custom event for any listeners.
        MainPageKeyboardEvent?.Invoke(this, new KeyboardInput
		{
            virtualKey = args.Key,
            virtualKeyModifiers = args.Modifiers,
            handled = args.Handled
		});
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
        Debug.WriteLine($"KeyboardAccelerator 🡒 {args.KeyboardAccelerator.Key}");
        if (MainFrame.CanGoBack)
            MainFrame.GoBack();
        args.Handled = true;
    }
    #endregion
}
