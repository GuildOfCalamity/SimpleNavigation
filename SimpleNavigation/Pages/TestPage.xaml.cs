using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestPage : Page, INotifyPropertyChanged
{
    #region [Properties]
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    }

    private Message? _selected = new Message { Content = "Informational Message Text", Severity = InfoBarSeverity.Informational };
    public Message? Selected
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                OnPropertyChanged();
            }
        }
    }
    public bool HasSelected => _selected != null && _selectedIdx > -1;

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

    private string? filter;
    private ObservableCollection<Message> _samples = new();
    public ObservableCollection<Message> Samples => string.IsNullOrEmpty(filter) 
        ? _samples 
        : new ObservableCollection<Message>(_samples.Where(i => ApplyFilter(i, filter)));
    #endregion

    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    public TestPage()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        this.InitializeComponent();
        this.Loaded += TestPage_Loaded;
        NoticeDialog.Opened += NoticeDialog_Opened;
    }

    #region [Events]
    /// <summary>
    /// Handle any parameter passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is SystemState sys)
        {
            // ⇦ ⇨ ⇧ ⇩  🡐 🡒 🡑 🡓  🡄 🡆 🡅 🡇  http://xahlee.info/comp/unicode_arrows.html
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

    void TestPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (Samples.Count == 0)
        {
            Task.Run(async () =>
            {
                foreach (var m in Extensions.GenerateMessages())
                {
                    await Task.Delay(30);
                    lvMessage.DispatcherQueue.TryEnqueue(() => { Samples.Add(m); });
                }
            });
        }
    }

    void TestButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.GoBack(new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
    }

    void MessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (Samples.Count == 0)
            return;

        // Cover the case where there is a selected item. There can be cases when the user clicks the delete button for an item where the selected index is different.
        //if (SelectedIdx > -1)
        //{
        //    Samples.RemoveAt(SelectedIdx);
        //}
        
        // Cover the cases where there is no selected item.
        if (sender is Button btn)
        {
            if (!string.IsNullOrEmpty($"{btn.Tag}") && Samples.Count > 0)
            {
                for (int i = 0; i < Samples.Count; i++)
                {
                    if (Samples[i].Content.Equals($"{btn.Tag}", StringComparison.OrdinalIgnoreCase))
                        Samples.RemoveAt(i);
                }
            }
        }
    }

    void ASB_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        filter = args.QueryText;

        // Our Message type does not inherit from ObservableObject, so we'll need to trigger a ListView refresh.
        lvMessage.DispatcherQueue.TryEnqueue(() =>
        {
            lvMessage.ItemsSource = null;
            lvMessage.ItemsSource = Samples;
            Selected = null;
        });
    }

    void MessageItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            if (string.IsNullOrEmpty(filter))
                VisualStateManager.GoToState(sender as Control, "HoverStackShown", true);
            else // the filtered set is a copy of the original
                VisualStateManager.GoToState(sender as Control, "HoverTimeOnlyShown", true);
        }
    }

    void MessageItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(sender as Control, "HoverStackHidden", true);
    }

    async void MessageItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var ctrl = sender as Control;
        if (ctrl != null)
        {
            var msg = ctrl.DataContext as Message;

            #region [Extract object using NoticeDialog's DataContext]
            //await Task.Run(async () =>
            //{
            //    // Allow time for the list selection to render.
            //    // If not the focus of the ContentDialog's TextBox may fail.
            //    // This is caused by the changing of the PointerPressed event to asynchronous.
            //    await Task.Delay(150);
            //    NoticeDialog.DispatcherQueue.TryEnqueue(async () =>
            //    {
            //        await OpenNoticeDialog(msg);
            //    });
            //});
            #endregion

            #region [Extract object using NoticeDialog's CommandParameter]
            await Task.Run(async () =>
            {
                // Allow time for the list selection to render.
                // If not the focus of the ContentDialog's TextBox may fail.
                // This is caused by the changing of the PointerPressed event to asynchronous.
                await Task.Delay(150);
                NoticeDialog.DispatcherQueue.TryEnqueue(async () =>
                {
                    await OpenNoticeDialogWithCommandParam(msg);
                });
            });
            #endregion
        }
    }
    #endregion

    #region [Helper Methods]
    private bool ApplyFilter(Message item, string filter)
    {
        return item.ApplyFilter(filter);
    }

    #endregion

    #region [Content Dialog]
    private Message? copyOfMessage = null;
    private ICommand DoneCommand => new RelayCommand(Update);
    private ICommand DoneCommandWithParam => new RelayCommand<Message>((modified) => 
    {
        if (modified == null || copyOfMessage == null)
            return;

        if (copyOfMessage.Content != modified.Content || copyOfMessage.Time != modified.Time)
        {
            // Our Message type does not inherit from ObservableObject, so we'll need to trigger a ListView refresh.
            lvMessage.DispatcherQueue.TryEnqueue(() =>
            {
                lvMessage.ItemsSource = null;
                lvMessage.ItemsSource = Samples;
                Selected = null;
            });
        }
    });

    async Task OpenNoticeDialog(Message? msg)
    {
        if (msg == null)
            return;

        copyOfMessage = new Message { Content = msg.Content, Severity = msg.Severity, Time = msg.Time };

        NoticeDialog.Title = "Edit Message";
        NoticeDialog.PrimaryButtonText = "Done";
        NoticeDialog.PrimaryButtonCommand = DoneCommand;
        NoticeDialog.CloseButtonText = "";
        NoticeDialog.DataContext = msg;
        await NoticeDialog.ShowAsync();
    }

    async Task OpenNoticeDialogWithCommandParam(Message? msg)
    {
        if (msg == null)
            return;

        copyOfMessage = new Message { Content = msg.Content, Severity = msg.Severity, Time = msg.Time };

        NoticeDialog.Title = "Edit Message";
        NoticeDialog.PrimaryButtonText = "Done";
        NoticeDialog.PrimaryButtonCommand = DoneCommandWithParam;
        NoticeDialog.PrimaryButtonCommandParameter = msg;
        NoticeDialog.CloseButtonText = "";
        NoticeDialog.DataContext = msg;
        await NoticeDialog.ShowAsync();
    }

    void NoticeDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        ndContent.Focus(FocusState.Programmatic);
        //if (ndContent.Text.Length > 0)
        //    ndContent.SelectAll();
    }

    private void Update()
    {
        var modified = NoticeDialog.DataContext as Message;

        if (modified == null || copyOfMessage == null)
            return;

        if (copyOfMessage.Content != modified.Content || copyOfMessage.Time != modified.Time)
        {
            // Our Message type does not inherit from ObservableObject, so we'll need to trigger a ListView refresh.
            lvMessage.DispatcherQueue.TryEnqueue(() =>
            {
                lvMessage.ItemsSource = null;
                lvMessage.ItemsSource = Samples;
                Selected = null;
            });
        }
    }
    #endregion
}
