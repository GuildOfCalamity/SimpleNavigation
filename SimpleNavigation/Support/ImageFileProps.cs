using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace SimpleNavigation;

public class ImageFileProps : INotifyPropertyChanged
{
    #region [Props]
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public StorageFile ImageFile { get; }
    public ImageProperties ImageProperties { get; }
    public string ImageName { get; }
    public string ImageFileType { get; }
    public string ImageDimensions => $"{ImageProperties.Width} x {ImageProperties.Height}";
    public string ImageTitle
    {
        get
        {
            try
            {
                // During a window maximize event an unhandled exception (KernelBase.dll) 0xC000027B: An application-internal exception has occurred (parameters: 0x000002135C3C4A10, 0x0000000000000005).
                // Which, in-turn, leads to an unhandled exception (KernelBase.dll) 0xC0000602: A fail fast exception occurred. Exception handlers will not be invoked and the process will be terminated immediately.
                return string.IsNullOrEmpty(ImageProperties.Title) ? ImageName : ImageProperties.Title;
            }
            catch (RuntimeWrappedException rwe) // catch any non-CLS exceptions
            {
                String? s = rwe.WrappedException as String;
                if (s != null)
                {
                    Debug.WriteLine($"ImageFileProps.get: {s}");
                    Debug.WriteLine($"Caller ⇨ {Extensions.GetStackTrace(new StackTrace())}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ImageFileProps.get: {ex.Message}");
                Debug.WriteLine($"Caller ⇨ {Extensions.GetStackTrace(new StackTrace())}");
            }
            return "Error";
        }
        set
        {
            if (ImageProperties.Title != value)
            {
                ImageProperties.Title = value;
                _ = ImageProperties.SavePropertiesAsync();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Used with the <see cref="Microsoft.UI.Xaml.Controls.RatingControl"/>.
    /// </summary>
    public int ImageRating
    {
        get => (int)ImageProperties.Rating;
        set
        {
            if (ImageProperties.Rating != value)
            {
                ImageProperties.Rating = (uint)value;
                _ = ImageProperties.SavePropertiesAsync();
                OnPropertyChanged();
            }
        }
    }
    #endregion

    public ImageFileProps(ImageProperties properties, StorageFile imageFile, string name, string type)
    {
        ImageProperties = properties;
        ImageName = name;
        ImageFileType = type;
        ImageFile = imageFile;
        var rating = (int)properties.Rating;
        ImageRating = rating == 0 ? Random.Shared.Next(1, 5) : rating;
    }

    public async Task<BitmapImage> GetImageSourceAsync()
    {
        using IRandomAccessStream fileStream = await ImageFile.OpenReadAsync();
        // Create a bitmap to be the image source.
        BitmapImage bitmapImage = new();
        bitmapImage.SetSource(fileStream);
        return bitmapImage;
    }

    public async Task<BitmapImage> GetImageThumbnailAsync()
    {
        StorageItemThumbnail thumbnail = await ImageFile.GetThumbnailAsync(ThumbnailMode.PicturesView);
        // Create a bitmap to be the image source.
        var bitmapImage = new BitmapImage();
        bitmapImage.SetSource(thumbnail);
        thumbnail.Dispose();
        return bitmapImage;
    }

}
