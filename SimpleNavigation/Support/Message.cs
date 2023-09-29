using System;
using Microsoft.UI.Xaml.Controls;

namespace SimpleNavigation;

/// <summary>
/// A simple messaging struct for an <see cref="InfoBar"/> control.
/// </summary>
public class Message
{
    /// <summary>
    /// Text string
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// InfoBar level
    /// </summary>
    public InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;
}
