using System;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Controls;

namespace SimpleNavigation;

/// <summary>
/// A simple messaging struct for an <see cref="InfoBar"/> control.
/// </summary>
public class Message
{
    public string Content { get; set; } = string.Empty;
    public InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;
    public DateTime Time { get; set; } = DateTime.Now;

    public bool ApplyFilter(string filter)
    {
        return Content.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
    }

}
