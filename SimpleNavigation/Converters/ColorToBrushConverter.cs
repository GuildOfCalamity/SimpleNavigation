using System;
using System.Diagnostics;
using System.Drawing;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace SimpleNavigation;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var scb = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255,10,10));

        try
        {
            //Debug.WriteLine($"Converting color value {value} to brush.");
            scb = new SolidColorBrush((Windows.UI.Color)value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ColorToBrushConverter: {ex.Message}");
        }
        return scb;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}
