using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Controls;

namespace SimpleNavigation;

/// <summary>
/// A simple messaging struct for an <see cref="InfoBar"/> control.
/// This version should be used with notifying collections, such as <see cref="ObservableCollection{T}"/>.
/// </summary>
public class Message
{
    public string Content { get; set; } = string.Empty;
    public InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;
    public DateTime Time { get; set; } = DateTime.MinValue;

    /// <summary>LINQ helper.</summary>
    /// <example>_messages.Where(o => ApplyFilter(o, "some filter"))</example>
    public bool ApplyFilter(string filter)
    {
        return Content.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
    }
}


/// <summary>
/// A simple messaging struct for an <see cref="InfoBar"/> control.
/// This version should be used in generic collections, such as <see cref="List{T}"/>
/// </summary>
public class ObservableMessage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    
    private string _content = string.Empty;
    public string Content 
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    private InfoBarSeverity _severity = InfoBarSeverity.Informational;
    public InfoBarSeverity Severity 
    {
        get => _severity;
        set
        {
            if (_severity != value)
            {
                _severity = value;
                OnPropertyChanged();
            }
        }
    }

    private DateTime _time = DateTime.MinValue;
    public DateTime Time 
    {
        get => _time;
        set
        {
            if (_time != value)
            {
                _time = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>LINQ helper.</summary>
    /// <example>_messages.Where(o => ApplyFilter(o, "some filter"))</example>
    public bool ApplyFilter(string filter)
    {
        return Content.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
    }
}

