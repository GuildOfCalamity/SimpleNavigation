namespace SimpleNavigation;

public class Item
{
	public string Content { get; set; } = string.Empty;
	public string Size { get; set; } = string.Empty;
	public string Time { get; set; } = string.Empty;
	public System.IO.FileAttributes Attribs { get; set; } = System.IO.FileAttributes.Normal;
}
