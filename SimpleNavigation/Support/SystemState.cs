using System;
using Microsoft.UI.Xaml;

namespace SimpleNavigation;

/// <summary>
/// A simple object model for passing information between pages during navigation.
/// </summary>
public class SystemState
{
    public string? Title { get; set; } = string.Empty;
    public Type? PageType { get; set; }
    public DateTime? LastUpdate { get; set; } = DateTime.Now;
    public ElementTheme? CurrentTheme { get; set; }
}
