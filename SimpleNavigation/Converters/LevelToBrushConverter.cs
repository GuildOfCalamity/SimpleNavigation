using System;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;

namespace SimpleNavigation;

public class LevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        SolidColorBrush scb = new SolidColorBrush(Colors.Gray);

        if (value == null || value.GetType() != typeof(InfoBarSeverity))
            return scb;

        switch ((InfoBarSeverity)value)
        {
            case InfoBarSeverity.Informational:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 87, 129, 198));
                break;
            case InfoBarSeverity.Success:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 200, 130));
                break;
            case InfoBarSeverity.Warning:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 198, 142, 87));
                break;
            case InfoBarSeverity.Error:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 198, 87, 88)); // Red
                break;
            default:
                scb = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 148, 148));
                break;
        }

        return scb;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}
