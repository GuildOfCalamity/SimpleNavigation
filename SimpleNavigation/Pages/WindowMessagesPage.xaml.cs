using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace SimpleNavigation;

/// <summary>
/// https://learn.microsoft.com/en-us/windows/win32/winmsg/window-notifications
/// </summary>
public sealed partial class WindowMessagesPage : Page, INotifyPropertyChanged
{
    #region [Properties]
    DispatcherTimer? timer = null;
    WindowsMessageLogger? WML = null;
    public ObservableCollection<MessageEventArgs> WindowMessages = new();
    ConcurrentQueue<MessageEventArgs> receivedMessages = new ConcurrentQueue<MessageEventArgs>();
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    }
    private int _selectedIdx = -1;
    public int SelectedIdx
    {
        get => _selectedIdx;
        set
        {
            if (_selectedIdx != value)
            {
                _selectedIdx = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;
    #endregion

    public WindowMessagesPage()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        this.InitializeComponent();
        this.Loaded += WindowMessagesPage_Loaded;
    }

    /// <summary>
    /// Handle any parameter passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is SystemState sys)
        {
            Debug.WriteLine($"You sent '{sys.Title}'");
            landing.Text = $"Move the mouse outside the window and then back inside.";
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

    void WindowMessagesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (WML == null && App.WindowHandle != IntPtr.Zero)
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(80);
            timer.Tick += QueueTimer_Tick;
            timer.Start();

            // Configure the window message listener.
            WML = new WindowsMessageLogger();
            WML.WndProcMsgReceived += WndProcMsgReceived;
            WML.StartLogging(App.WindowHandle);
        }
    }

    /// <summary>
    /// Messages can come fast-n-furious, so you would never want to directly bind
    /// the ObservableCollection to the event. We'll queue them and then distribute
    /// in a controlled tempo.
    /// </summary>
    void QueueTimer_Tick(object? sender, object e)
    {
        if (receivedMessages.Count > 0 && !App.IsClosing)
        {
            if (receivedMessages.TryDequeue(out var msg))
                WindowMessages.Add(msg);
        }
    }

    /// <summary>
    /// Our event from the <see cref="WindowsMessageLogger"/>.
    /// </summary>
    void WndProcMsgReceived(object? sender, MessageEventArgs e)
    {
        if (e != null)
           receivedMessages.Enqueue(e);
    }
}
