using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Search;
using Windows.Storage;
using System.Collections.ObjectModel;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ImagesPage : Page
{
    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    public ObservableCollection<ImageFileProps> Images { get; } = new();

    public ImagesPage()
    {
        this.InitializeComponent();
        _ = GetItemsAsync();
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

    async Task GetItemsAsync()
    {
#if IS_UNPACKAGED
        var imgFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        StorageFolder picturesFolder = await StorageFolder.GetFolderFromPathAsync(imgFolder);
#else
        StorageFolder appInstalledFolder = Package.Current.InstalledLocation;
        StorageFolder picturesFolder = await appInstalledFolder.GetFolderAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
#endif
        var result = picturesFolder.CreateFileQueryWithOptions(new QueryOptions());
        IReadOnlyList<StorageFile> imageFiles = await result.GetFilesAsync();
        foreach (StorageFile file in imageFiles)
        {
            Images.Add(await LoadImageInfoAsync(file));
        }
    }

    async Task<StorageFolder> GetKnownPicturesPath()
    {
        return await KnownFolders.GetFolderAsync(KnownFolderId.PicturesLibrary);
    }

    async Task<StorageFolder> GetKnownDocumentsPath()
    {
        return await KnownFolders.GetFolderAsync(KnownFolderId.DocumentsLibrary);
    }

    public async static Task<ImageFileProps> LoadImageInfoAsync(StorageFile file)
    {
        var properties = await file.Properties.GetImagePropertiesAsync();
        ImageFileProps info = new(properties, file, file.DisplayName, file.DisplayType);
        return info;
    }

    void ImageGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue)
        {
            var templateRoot = args.ItemContainer.ContentTemplateRoot as Grid;
            var image = templateRoot.FindName("ItemImage") as Image;
            image.Source = null;
        }

        if (args.Phase == 0)
        {
            args.RegisterUpdateCallback(ShowImage);
            args.Handled = true;
        }
    }

    async void ShowImage(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase == 1)
        {
            // It's phase 1, so show this item's image.
            var templateRoot = args.ItemContainer.ContentTemplateRoot as Grid;
            var image = templateRoot.FindName("ItemImage") as Image;
            var item = args.Item as ImageFileProps;
            if (item != null)
                image.Source = await item.GetImageThumbnailAsync();
        }
    }

    void ItemImage_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Image img)
        {
            hostGrid.Background = new ImageBrush
            {
                ImageSource = img.Source,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center,
                Stretch = Stretch.UniformToFill,
                Opacity = 0.3,
            };
        }
        Debug.WriteLine($"OriginalSource is '{e.OriginalSource}'");
    }

}
