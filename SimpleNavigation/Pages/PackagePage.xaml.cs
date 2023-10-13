using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SimpleNavigation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PackagePage : Page, INotifyPropertyChanged
    {
        System.Diagnostics.Process? _process = null;

        /// <summary>
        /// An event that the main page can subscribe to.
        /// </summary>
        public static event EventHandler<Message>? PostMessageEvent;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
        }

        private string? filter;
        private ObservableCollection<PackageDetail> _items = new();
        public ObservableCollection<PackageDetail> Items => string.IsNullOrEmpty(filter)
            ? _items
            : new ObservableCollection<PackageDetail>(_items.Where(o => ApplyFilter(o, filter)));

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value) // Prevent uneccessary triggers.
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public PackagePage()
        {
            this.InitializeComponent();
            this.Loaded += PackagePage_Loaded;
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

        async void PackagePage_Loaded(object sender, RoutedEventArgs e)
        {
            IsBusy = true;
            if (string.IsNullOrEmpty(filter))
            {
                landing.Text = $"Installed packages";
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var pkgs = await PackageDetailHelper.GatherAllPackagesForUserAsync(false, cts.Token);
                Items.Clear();
                foreach (var pkg in pkgs)
                {
                    if (!Items.Contains(pkg))
                        Items.Add(pkg);
                }
                landing.Text = $"{pkgs.Count} total packages";
            }
            IsBusy = false;
        }

        bool ApplyFilter(PackageDetail item, string filter)
        {
            return item.ApplyFilter(filter);
        }

        /// <summary>
        /// <see cref="ItemsView"/> event.
        /// </summary>
        void ItemsView_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
        {
            if (sender.SelectedItems != null && sender.SelectedItems.Count > 0)
            {
                Debug.WriteLine($"Selected items count: {sender.SelectedItems.Count}");
                foreach (var selected in sender.SelectedItems)
                {
                    if (selected is PackageDetail item)
                    {
                        OpenLocation(item.Location);
                    }
                }
            }
            else if (sender.SelectedItem != null && sender.SelectedItem is PackageDetail item)
            {
                OpenLocation(item.Location);
            }
        }


        /// <summary>
        /// <see cref="ListView"/> event.
        /// </summary>
        void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Debug.WriteLine($"🡒 Selected item count: {e.AddedItems.Count}");
                foreach (var obj in e.AddedItems)
                {
                    if (obj is PackageDetail item)
                    {
                        OpenLocation(item.Location);
                    }
                }
            }
        }

        /// <summary>
        /// An example of extracting the <see cref="ObservableCollection{T}"/> from an event.
        /// This is unnecessary as we already have access to the collection inside the class scope.
        /// </summary>
        void ListView_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var items = ((ListView)sender).ItemsSource as ObservableCollection<PackageDetail>;
            if (items != null)
                landing.Text = $"The control contains {items.Count} items.";
        }

        /// <summary>
        /// Calls Explorer to open and select the item.
        /// </summary>
        void OpenLocation(string? itemPath)
        {
            if (!string.IsNullOrEmpty(itemPath) && !itemPath.StartsWith("N/A"))
            {
                var test = System.IO.Path.GetDirectoryName(itemPath);
                if (!string.IsNullOrEmpty(test))
                {
                    try
                    {   // Close existing explorer.
                        if (_process != null)
                        {
                            if (_process.CloseMainWindow())
                                Debug.WriteLine($"Process window close command was successful.");
                            else
                                Debug.WriteLine($"Process window close command failed.");
                        }
                        // Open new explorer and select the file.
                        _process = System.Diagnostics.Process.Start($"explorer.exe", $"/select,\"{itemPath}\"");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"OpenLocation: {ex.Message}");
                    }
                }
            }
        }

        void ASB_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            filter = args.QueryText;

            // Our PackageDetail type does not inherit from ObservableObject, so we'll need to trigger a ItemsView refresh.
            ivItems.DispatcherQueue.TryEnqueue(() =>
            {
                ivItems.ItemsSource = null;
                ivItems.ItemsSource = Items;
            });

            landing.Text = $"{Items.Count} packages";
        }
    }
}
