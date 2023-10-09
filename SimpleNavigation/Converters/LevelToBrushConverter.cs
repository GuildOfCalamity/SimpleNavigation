using System;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace SimpleNavigation;

public class LevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int alpha = 255;
        SolidColorBrush scb = new SolidColorBrush(Colors.Gray);

        if (value == null || value.GetType() != typeof(InfoBarSeverity))
            return scb;

        if (parameter != null && int.TryParse($"{parameter}", out alpha))
        {
            if (alpha > 255)
                alpha = 255;
        }

        switch ((InfoBarSeverity)value)
        {
            case InfoBarSeverity.Informational:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 87, 129, 198));
                break;
            case InfoBarSeverity.Success:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 0, 200, 130));
                break;
            case InfoBarSeverity.Warning:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 198, 142, 87));
                break;
            case InfoBarSeverity.Error:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 198, 87, 88));
                break;
            default:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 148, 148, 148));
                break;
        }

        return scb;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}

public class LevelToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int alpha = 255;
        SolidColorBrush scb = new SolidColorBrush(Colors.Gray);

        if (value == null || value.GetType() != typeof(InfoBarSeverity))
            return scb;

        if (parameter != null && int.TryParse($"{parameter}", out alpha))
        {
            if (alpha > 255)
                alpha = 255;
        }

        switch ((InfoBarSeverity)value)
        {
            case InfoBarSeverity.Informational:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 87/2, 129/2, 198/2));
                break;
            case InfoBarSeverity.Success:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 0, 200/2, 130/2));
                break;
            case InfoBarSeverity.Warning:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 198/2, 142/2, 87/2));
                break;
            case InfoBarSeverity.Error:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 198/2, 87/2, 88/2));
                break;
            default:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)alpha, 148/2, 148/2, 148/2));
                break;
        }

        return scb;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}
