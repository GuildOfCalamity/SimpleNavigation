using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI;

namespace SimpleNavigation;

/// <summary>
/// We'll use <see cref="DependencyProperty"/>s instead of <see cref="System.ComponentModel.INotifyPropertyChanged"/> for this page.
/// </summary>
public sealed partial class SettingsPage : Page
{
    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    #region [Dependency Properties]
    // A basic TreeViewNode property.
    public static readonly DependencyProperty SelectedNodeProperty = DependencyProperty.Register(
        nameof(SelectedNode),
        typeof(TreeViewNode),
        typeof(SettingsPage),
        new PropertyMetadata(null, OnNodeChanged));
    public TreeViewNode SelectedNode
    {
        get { return (TreeViewNode)GetValue(SelectedNodeProperty); }
        set { SetValue(SelectedNodeProperty, value); }
    }

    // A basic IsChecked property.
    public static readonly DependencyProperty IsOptionCheckedProperty = DependencyProperty.Register(
        nameof(IsOptionChecked),
        typeof(bool),
        typeof(SettingsPage),
        new PropertyMetadata(false, OnIsOptionCheckedChanged));
    public bool IsOptionChecked
    {
        get { return (bool)GetValue(IsOptionCheckedProperty); }
        set { SetValue(IsOptionCheckedProperty, value); }
    }
    #endregion

    public SettingsPage()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        this.InitializeComponent();
        PopulateTree();
        this.Loaded += SettingsPage_Loaded;
    }

    void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        tbReferences.Text = string.Empty;
        Task.Run(async () =>
        {
            await Task.Delay(500);
            var data = Extensions.GatherReferenceAssemblies(true);
            tbReferences.DispatcherQueue.TryEnqueue(() => { tbReferences.Text = $"{data}"; });
            //await StoryboardPath.BeginAsync();
        });

        if (App.AnimationsEffectsEnabled)
            StoryboardPath.Begin();

        Debug.WriteLine($"Requested theme is '{App.ThemeRequested}'");
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

    #region [Control Events]
    void myColorButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        var border = (Border)sender.Content;
        var color = ((SolidColorBrush)border.Background).Color;
        landing.Text = $"You clicked {color}";
    }

    void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var rect = (Microsoft.UI.Xaml.Shapes.Rectangle)e.ClickedItem;
        var color = ((SolidColorBrush)rect.Fill).Color;
        CurrentColor.Background = new SolidColorBrush(color);
        
        var theme = (sender as GridView)?.ActualTheme ?? ElementTheme.Dark;
        if (theme == ElementTheme.Dark)
            settingsGrid.Background = DarkenColor(new SolidColorBrush(color));
        else
            settingsGrid.Background = LightenColor(new SolidColorBrush(color));

        landing.Text = $"Selected color is {color}";
        // Delay required to circumvent GridView bug: https://github.com/microsoft/microsoft-ui-xaml/issues/6350
        Task.Delay(20).ContinueWith(_ => myColorButton.Flyout.Hide(), TaskScheduler.FromCurrentSynchronizationContext());
    }
    #endregion

    #region [Dependency Callbacks]
    static void OnIsOptionCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        // Call the non-static version of this method so that we can
        // work with any local instanced variables. This is due to the
        // fact that Dependency callbacks can only be static.
        ((SettingsPage)d).OnIsCheckedChanged((bool)args.NewValue);
    }
    void OnIsCheckedChanged(bool val) => landing.Text = $"IsChecked = {val}";

    static void OnNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        // Call the non-static version of this method so that we can
        // work with any local instanced variables. This is due to the
        // fact that Dependency callbacks can only be static.
        ((SettingsPage)d).OnNodeChanged((TreeViewNode)args.NewValue);
    }
    void OnNodeChanged(TreeViewNode newNode)
    {
        landing.Text = $"Selected '{newNode.Content}' with {(newNode.Children.Count == 0 ? "no" : $"{newNode.Children.Count}")} children";

        if (newNode.Children.Count > 0)
        {
            foreach (var obj in newNode.Children)
            {
                Debug.WriteLine($" - Child '{obj.Content}' is {obj.Depth} away from the root");
            }
        }
    }
    #endregion

    #region [Helper Methods]
    SolidColorBrush DarkenColor(SolidColorBrush brsh, double amount = 0.5)
    {
        var color = brsh.Color;
        var factor = 1d - amount;

        //return Windows.UI.Color.FromArgb(color.A, (byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor)));
        return new SolidColorBrush(Windows.UI.Color.FromArgb(color.A, (byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor)));
    }

    SolidColorBrush LightenColor(SolidColorBrush brsh, double amount = 0.5)
    {
        var source = brsh.Color;
        var factor = 1d + amount;

        var red = (int)(source.R * factor);
        var green = (int)(source.G * factor);
        var blue = (int)(source.B * factor);

        // NOTE: If a single RGB value is passed, e.g. "Blue" (0,0,255)
        // then the other values will not make much impact, so we'll
        // ramp the Red/Green channels a little to offset a muted result.
        // This will provide a contrast enhancment but will not be true
        // to the original color passed.
        if (red == 0) { red = 0x30; }
        else if (red > 255) { red = 0xFF; }
        if (green == 0) { green = 0x30; }
        else if (green > 255) { green = 0xFF; }
        if (blue == 0) { blue = 0x30; }
        else if (blue > 255) { blue = 0xFF; }

        //return Windows.UI.Color.FromArgb((byte)255, (byte)red, (byte)green, (byte)blue);
        return new SolidColorBrush(Windows.UI.Color.FromArgb(source.A, (byte)(red), (byte)(green), (byte)(blue)));
    }

    void PopulateTree()
    {
        sampleTreeView.RootNodes.Clear();

        var rootNode = new TreeViewNode() { Content = "Settings" };
        
        var node1 = new TreeViewNode() { Content = $"1st node" };
        var subnode1 = new TreeViewNode() { Content = $"1st sub-node" };
        node1.Children.Add(subnode1);
        
        var node2 = new TreeViewNode() { Content = $"2nd node" };
        var subnode2 = new TreeViewNode() { Content = $"2nd sub-node" };
        node2.Children.Add(subnode2);

        var node3 = new TreeViewNode() { Content = $"3rd node" };
        var subnode3 = new TreeViewNode() { Content = $"3rd sub-node" };
        node3.Children.Add(subnode3);

        rootNode.Children.Add(node1);
        rootNode.Children.Add(node2);
        rootNode.Children.Add(node3);

        // Add accumulated nodes to the main tree root.
        sampleTreeView.RootNodes.Add(rootNode);
    }
    #endregion
}
