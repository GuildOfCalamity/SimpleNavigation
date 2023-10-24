using Microsoft.UI.Xaml.Markup;

namespace SimpleNavigation;

/*
   Typical use-case...
   <Button x:Uid="MyButton"/>

   Using this markup extension...
   <Button Content="{local:ResourceString Name=ButtonText}"/>
*/

/// <summary>
/// This idea was copied from:
/// https://devblogs.microsoft.com/ifdef-windows/use-a-custom-resource-markup-extension-to-succeed-at-ui-string-globalization/
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class ResourceString : MarkupExtension
{
    public string Name { get; set; } = string.Empty;
    protected override object ProvideValue()
    {
        try
        {
            var value = AppResourceManager.GetInstance.GetString(Name);
            return value;
        }
        catch (System.Exception)
        {
            return string.Empty;
        }
    }
}
