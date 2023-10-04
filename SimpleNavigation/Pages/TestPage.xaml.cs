using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestPage : Page, INotifyPropertyChanged
{
    #region [Properties]
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
    public ObservableCollection<Message> Samples => filter is null ? _samples : new ObservableCollection<Message>(_samples.Where(i => ApplyFilter(i, filter)));
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
        for (int i = 0; i < 31; i++)
        {
            Samples.Add(new Message { Content = Extensions.GetRandomSentence(Random.Shared.Next(8,18)), Severity = GetRandomSeverity(), Time = DateTime.Now.AddDays(-1 * Random.Shared.Next(1, 31)) });
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

    void ListViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            VisualStateManager.GoToState(sender as Control, "HoverButtonsShown", true);
        }
    }

    void ListViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(sender as Control, "HoverButtonsHidden", true);
    }
    #endregion

    #region [Methods]
    private bool ApplyFilter(Message item, string filter)
    {
        return item.ApplyFilter(filter);
    }

    private InfoBarSeverity GetRandomSeverity()
    {
        switch (Random.Shared.Next(5))
        {
            case 0: return InfoBarSeverity.Error;
            case 1: return InfoBarSeverity.Warning;
            case 2: return InfoBarSeverity.Success;
            default: return InfoBarSeverity.Informational;
        }
    }
    #endregion
}
