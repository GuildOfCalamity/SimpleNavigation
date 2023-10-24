using Microsoft.Windows.ApplicationModel.Resources;

namespace SimpleNavigation;

/// <summary>
/// Helper class for the <see cref="ResourceManager"/>.
/// </summary>
public sealed class AppResourceManager
{
    #region [Props]
    static AppResourceManager? instance = null;
    static ResourceManager? _resourceManager = null;
    public static AppResourceManager GetInstance
    {
        get
        {
            if (instance == null)
                instance = new AppResourceManager();

            return instance;
        }
    }
    #endregion

    private AppResourceManager()
    {
        // Contrary to the UWP XAML apps, WinUI3 apps do not have a public static 
        // ResourceManager or ResourceLoader object. When localizing a control with x:UI,  
        // WinUI 3 creates a ResourceManager internally, but you can't get the instance -
        // you will need to create one. You can use the Singleton pattern to accomplish it.
        _resourceManager = new();
    }

    public string GetString(string name)
    {
        if (_resourceManager == null)
            return string.Empty;
         
        // The string resources from the 'resources.resw' file are stored in the subtree Resources 
        // in the 'resources.pri' file. However, MUX.ResourceManager scopes to the root of the tree,
        // so you need to search the Resources tree ('Resources/{yourElement}') to get the value.
        // Also, when you create a hierarchical entry (e.g. 'object.property') on the 'resources.resw'
        // file, the 'resources.pri' contains a subtree for all the properties of the objects. 
        // For instance, the 'InforBar.Title' value is stored in the Resources/InfoBar/Title.
        // As a consequence, the dot('.') operator (InfoBar.Title)must be replaced for a slash ('/')
        return _resourceManager.MainResourceMap.GetValue($"Resources/{name.Replace(".", "/")}").ValueAsString;
    }
}
