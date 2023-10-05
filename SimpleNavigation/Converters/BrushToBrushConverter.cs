using System;
using System.Diagnostics;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace SimpleNavigation;

public class BrushToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double amount = 0.2; // amount to darken (default by 20%)
        var scb = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255,10,10));

        if (parameter != null && double.TryParse($"{parameter}", out amount))
        {
            if (amount > 0.999)
                amount = 0.9;
        }

        try
        {
            if (value != null && value is SolidColorBrush scbrsh) 
            {
                var color = scbrsh.Color;
                var factor = 1.0 - amount;
                return new SolidColorBrush(Windows.UI.Color.FromArgb(color.A, (byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor)));
            }
            else if (value != null && value is Brush brsh)
            {
                var color = ((SolidColorBrush)brsh).Color;
                var factor = 1.0 - amount;
                return new SolidColorBrush(Windows.UI.Color.FromArgb(color.A, (byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor)));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BrushToBrushConverter: {ex.Message}");
        }

        return scb;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}
