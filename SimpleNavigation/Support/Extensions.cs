using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace SimpleNavigation
{
    public static class Extensions
    {
        /// <summary>
        /// Fetch all <see cref="ProcessModule"/>s in the current running process.
        /// </summary>
        /// <param name="excludeWinSys">if true any file path starting with %windir% will be excluded from the results</param>
        public static string GatherReferenceAssemblies(bool excludeWinSys)
        {
            var modules = new StringBuilder();
            var winSys = Environment.GetFolderPath(Environment.SpecialFolder.Windows) ?? "N/A";
            var winProg = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) ?? "N/A";
            var self = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "EmptySelf";
            try
            {
                var process = Process.GetCurrentProcess();
                foreach (ProcessModule module in process.Modules)
                {
                    var fn = module.FileName ?? "Empty";
                    if (excludeWinSys && !fn.StartsWith(winSys, StringComparison.OrdinalIgnoreCase) && !fn.StartsWith(winProg, StringComparison.OrdinalIgnoreCase) && !fn.StartsWith(self, StringComparison.OrdinalIgnoreCase))
                        modules.AppendLine($"{Path.GetFileName(fn)} ({GetFileVersion(fn)})");
                    else if (!excludeWinSys)
                        modules.AppendLine($"{Path.GetFileName(fn)} ({GetFileVersion(fn)})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GatherReferencedAssemblies: {ex.Message}", nameof(Extensions));
            }
            return modules.ToString();
        }

        /// <summary>
        /// Brute force alpha removal of FileVersionInfo text
        /// is not always the best approach, e.g. the following:
        /// "3.0.0-zmain.2211 (DCPP(199ff10ec000000)(cloudtest).160101.0800)"
        /// ...converts to:
        /// "3.0.0.221119910000000.160101.0800"
        /// ...which is not accurate.
        /// </summary>
        /// <param name="fullPath">the entire path to the file</param>
        /// <returns>sanitized <see cref="Version"/></returns>
        public static Version GetFileVersion(string fullPath)
        {
            try
            {
                var ver = FileVersionInfo.GetVersionInfo(fullPath).FileVersion;
                if (string.IsNullOrEmpty(ver)) { return new Version(); }
                if (ver.HasSpace())
                {   // Some assemblies contain versions such as "10.0.22622.1030 (WinBuild.160101.0800)"
                    // This will cause the Version constructor to throw an exception, so just take the first piece.
                    var chunk = ver.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var firstPiece = Regex.Replace(chunk[0].Replace(',', '.'), "[^.0-9]", "");
                    return new Version(firstPiece);
                }
                string cleanVersion = Regex.Replace(ver, "[^.0-9]", "");
                return new Version(cleanVersion);
            }
            catch (Exception)
            {
                return new Version(); // 0.0
            }
        }

        public static bool HasSpace(this string str)
        {
            if (string.IsNullOrEmpty(str)) { return false; }
            return str.Any(x => char.IsSeparator(x));
        }

        /// <summary>
        /// Helper method that can be used to compare if two dictionaries are equal.
        /// This method uses SequenceEqual to compare the keys and values of the two dictionaries.
        /// </summary>
        /// <typeparam name="TKey">key type</typeparam>
        /// <typeparam name="TValue">value type</typeparam>
        /// <param name="dict1">first dictionary to compare</param>
        /// <param name="dict2">second dictionary to compare</param>
        /// <returns>true if both match, false otherwise</returns>
        public static bool AreEqual<TKey, TValue>(Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
        {
            if (dict1 == dict2) { return true; }
            if (dict1 == null || dict2 == null) { return false; }
            if (dict1.Count != dict2.Count) { return false; }

            var comparer = EqualityComparer<TValue>.Default;
            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out TValue value))
                    return false;
                if (!comparer.Equals(kvp.Value, value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Use this if you only have a root resource dictionary.
        /// var rdBrush = Extensions.GetResource{SolidColorBrush}("PrimaryBrush");
        /// </summary>
        public static T? GetResource<T>(string resourceName) where T : class
        {
            try
            {
                if (Application.Current.Resources.TryGetValue($"{resourceName}", out object value))
                    return (T)value;
                else
                    return default(T);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetResource: {ex.Message}", $"{nameof(Extensions)}");
                return null;
            }
        }

        /// <summary>
        /// Use this if you have merged theme resource dictionaries.
        /// var darkBrush = Extensions.GetThemeResource{SolidColorBrush}("PrimaryBrush", ElementTheme.Dark);
        /// var lightBrush = Extensions.GetThemeResource{SolidColorBrush}("PrimaryBrush", ElementTheme.Light);
        /// </summary>
        public static T? GetThemeResource<T>(string resourceName, ElementTheme? theme) where T : class
        {
            try
            {
                theme ??= ElementTheme.Default;

                var dictionaries = Application.Current.Resources.MergedDictionaries;
                foreach (var item in dictionaries)
                {
                    // A typical IList<ResourceDictionary> will contain:
                    //   - 'Default'
                    //   - 'Light'
                    //   - 'Dark'
                    //   - 'HighContrast'
                    foreach (var kv in item.ThemeDictionaries.Keys)
                    {
                        Debug.WriteLine($"ThemeDictionary is named '{kv}'", $"{nameof(Extensions)}");
                    }

                    // Do we have any themes in this resource dictionary?
                    if (item.ThemeDictionaries.Count > 0)
                    {
                        if (theme == ElementTheme.Dark)
                        {
                            if (item.ThemeDictionaries.TryGetValue("Dark", out var drd))
                            {
                                ResourceDictionary? dark = drd as ResourceDictionary;
                                if (dark != null)
                                {
                                    Debug.WriteLine($"Found dark theme resource dictionary", $"{nameof(Extensions)}");
                                    if (dark.TryGetValue($"{resourceName}", out var tmp))
                                        return (T)tmp;
                                    else
                                        Debug.WriteLine($"Could not find '{resourceName}'", $"{nameof(Extensions)}");
                                }
                            }
                            else { Debug.WriteLine($"{nameof(ElementTheme.Dark)} theme was not found", $"{nameof(Extensions)}"); }
                        }
                        else if (theme == ElementTheme.Light)
                        {
                            if (item.ThemeDictionaries.TryGetValue("Light", out var lrd))
                            {
                                ResourceDictionary? light = lrd as ResourceDictionary;
                                if (light != null)
                                {
                                    Debug.WriteLine($"Found light theme resource dictionary", $"{nameof(Extensions)}");
                                    if (light.TryGetValue($"{resourceName}", out var tmp))
                                        return (T)tmp;
                                    else
                                        Debug.WriteLine($"Could not find '{resourceName}'", $"{nameof(Extensions)}");
                                }
                            }
                            else { Debug.WriteLine($"{nameof(ElementTheme.Light)} theme was not found", $"{nameof(Extensions)}"); }
                        }
                        else if (theme == ElementTheme.Default)
                        {
                            if (item.ThemeDictionaries.TryGetValue("Default", out var drd))
                            {
                                ResourceDictionary? dflt = drd as ResourceDictionary;
                                if (dflt != null)
                                {
                                    Debug.WriteLine($"Found default theme resource dictionary", $"{nameof(Extensions)}");
                                    if (dflt.TryGetValue($"{resourceName}", out var tmp))
                                        return (T)tmp;
                                    else
                                        Debug.WriteLine($"Could not find '{resourceName}'", $"{nameof(Extensions)}");
                                }
                            }
                            else { Debug.WriteLine($"{nameof(ElementTheme.Default)} theme was not found", $"{nameof(Extensions)}"); }
                        }
                        else
                            Debug.WriteLine($"No theme to match", $"{nameof(Extensions)}");
                    }
                    else
                        Debug.WriteLine($"No theme dictionaries found", $"{nameof(Extensions)}");
                }

                return default(T);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetThemeResource: {ex.Message}", $"{nameof(Extensions)}");
                return null;
            }
        }

        public static async Task<byte[]> AsPng(this UIElement control)
        {
            // Get XAML Visual in BGRA8 format
            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(control, (int)control.ActualSize.X, (int)control.ActualSize.Y);

            // Encode as PNG
            var pixelBuffer = (await rtb.GetPixelsAsync()).ToArray();
            IRandomAccessStream mraStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, mraStream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)rtb.PixelWidth,
                (uint)rtb.PixelHeight,
                184,
                184,
                pixelBuffer);
            await encoder.FlushAsync();

            // Transform to byte array
            var bytes = new byte[mraStream.Size];
            await mraStream.ReadAsync(bytes.AsBuffer(), (uint)mraStream.Size, InputStreamOptions.None);

            return bytes;
        }

        public static string RemoveExtraSpaces(this string strText)
        {
            if (!string.IsNullOrEmpty(strText))
                strText = Regex.Replace(strText, @"\s+", " ");

            return strText;
        }

        /// <summary>
        /// ExampleTextSample => Example Text Sample
        /// </summary>
        /// <param name="input"></param>
        /// <returns>space delimited string</returns>
        public static string SeparateCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            StringBuilder result = new StringBuilder();
            result.Append(input[0]);

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                    result.Append(' ');

                result.Append(input[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// var stack = Extensions.GetStackTrace(new StackTrace());
        /// </summary>
        internal static string GetStackTrace(StackTrace st)
        {
            string rv = string.Empty;
            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                rv += sf.GetMethod() + " <== ";
            }
            return rv;
        }

        public static async Task LocateAndLaunchUrlFromString(string text)
        {
            List<string> urls = ExtractUrls(text);
            if (urls.Count > 0)
            {
                Uri uriResult = new Uri(urls[0]);
                await Windows.System.Launcher.LaunchUriAsync(uriResult);
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        public static List<string> ExtractUrls(string text)
        {
            List<string> urls = new List<string>();
            Regex urlRx = new Regex(@"((https?|ftp|file)\://|www\.)[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\\(\)\*\-\._~%]*)*", RegexOptions.IgnoreCase);
            MatchCollection matches = urlRx.Matches(text);
            foreach (Match match in matches) { urls.Add(match.Value); }
            return urls;
        }

        public static BitmapImage? GetImageFromAssets(this string assetName)
        {
            BitmapImage? img = null;

            try
            {
                Uri? uri = new Uri("ms-appx:///Assets/" + assetName.Replace("./", ""));
                img = new BitmapImage(uri);
                Debug.WriteLine($"Image resolved!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetImageFromAssets: {ex.Message}");
            }

            return img;
        }

        /// <summary>
        ///  var objects = IterateEnumValues{LogLevel}();
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static IEnumerable<T> IterateEnumValues<T>() where T : Enum
        {
            foreach (var value in Enum.GetValues(typeof(T)))
            {
                yield return (T)value;
            }
        }

        /// <summary>
        /// var names = IterateEnumNames{LogLevel}();
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static IEnumerable<string> IterateEnumNames<T>() where T : Enum
        {
            foreach (var name in Enum.GetNames(typeof(T)))
            {
                yield return $"{name}";
            }
        }

        /// <summary>
        /// Clamping function for any value of type <see cref="IComparable{T}"/>.
        /// </summary>
        /// <param name="val">initial value</param>
        /// <param name="min">lowest range</param>
        /// <param name="max">highest range</param>
        /// <returns>clamped value</returns>
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            return val.CompareTo(min) < 0 ? min : (val.CompareTo(max) > 0 ? max : val);
        }

        /// <summary>
        /// Scales a range of double. [baseMin to baseMax] will become [limitMin to limitMax]
        /// </summary>
        public static double Scale(this double valueIn, double baseMin, double baseMax, double limitMin, double limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;
        /// <summary>
        /// Scales a range of floats. [baseMin to baseMax] will become [limitMin to limitMax]
        /// </summary>
        public static float Scale(this float valueIn, float baseMin, float baseMax, float limitMin, float limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;
        /// <summary>
        /// Scales a range of integers. [baseMin to baseMax] will become [limitMin to limitMax]
        /// </summary>
        public static int Scale(this int valueIn, int baseMin, int baseMax, int limitMin, int limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;

        /// <summary>
        /// Linear interpolation for a range of doubles.
        /// </summary>
        public static double Lerp(this double start, double end, double amount = 0.5D) => start + (end - start) * amount;
        /// <summary>
        /// Linear interpolation for a range of floats.
        /// </summary>
        public static float Lerp(this float start, float end, float amount = 0.5F) => start + (end - start) * amount;

        public static bool RandomBoolean()
        {
            if (Random.Shared.Next(100) > 49)
                return true;

            return false;
        }

        /// <summary>
        /// Converts long file size into typical browser file size.
        /// </summary>
        public static string ToFileSize(this long size)
        {
            if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
            if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + "KB"; }
            if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F0") + "MB"; }
            if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F0") + "GB"; }
            if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F0") + "TB"; }
            if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F0") + "PB"; }
            return (size / Math.Pow(1024, 6)).ToString("F0") + "EB";
        }

		public static bool IsPathTooLong(string path)
		{
			try
			{
				var tmp = Path.GetFullPath(path);
				return false;
			}
			catch (UnauthorizedAccessException) { return false; }
			catch (DirectoryNotFoundException) { return false; }
			catch (PathTooLongException) { return true; }
		}

		/// <summary>
		/// Display a readable sentence as to when that time happened.
		/// e.g. "5 minutes ago" or "in 2 days"
		/// </summary>
		/// <param name="value"><see cref="DateTime"/>the past/future time to compare from now</param>
		/// <returns>human friendly format</returns>
		public static string ToReadableTime(this DateTime value, bool useUTC = false)
        {
            TimeSpan ts;
            if (useUTC)
                ts = new TimeSpan(DateTime.UtcNow.Ticks - value.Ticks);
            else
                ts = new TimeSpan(DateTime.Now.Ticks - value.Ticks);

            double delta = ts.TotalSeconds;
            if (delta < 0) // in the future
            {
                delta = Math.Abs(delta);
                if (delta < 60) { return Math.Abs(ts.Seconds) == 1 ? "in one second" : "in " + Math.Abs(ts.Seconds) + " seconds"; }
                if (delta < 120) { return "in a minute"; }
                if (delta < 3000) { return "in " + Math.Abs(ts.Minutes) + " minutes"; } // 50 * 60
                if (delta < 5400) { return "in an hour"; } // 90 * 60
                if (delta < 86400) { return "in " + Math.Abs(ts.Hours) + " hours"; } // 24 * 60 * 60
                if (delta < 172800) { return "tomorrow"; } // 48 * 60 * 60
                if (delta < 2592000) { return "in " + Math.Abs(ts.Days) + " days"; } // 30 * 24 * 60 * 60
                if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
                {
                    int months = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 30));
                    return months <= 1 ? "in one month" : "in " + months + " months";
                }
                int years = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 365));
                return years <= 1 ? "in one year" : "in " + years + " years";
            }
            else // in the past
            {
                if (delta < 60) { return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago"; }
                if (delta < 120) { return "a minute ago"; }
                if (delta < 3000) { return ts.Minutes + " minutes ago"; } // 50 * 60
                if (delta < 5400) { return "an hour ago"; } // 90 * 60
                if (delta < 86400) { return ts.Hours + " hours ago"; } // 24 * 60 * 60
                if (delta < 172800) { return "yesterday"; } // 48 * 60 * 60
                if (delta < 2592000) { return ts.Days + " days ago"; } // 30 * 24 * 60 * 60
                if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
                {
                    int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                    return months <= 1 ? "one month ago" : months + " months ago";
                }
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }

        /// <summary>
        /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
        /// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
        /// </summary>
        /// <param name="span"><see cref="TimeSpan"/></param>
        /// <param name="significantDigits">number of right side digits in output (precision)</param>
        /// <returns>human-friendly string</returns>
        public static string ToTimeString(this TimeSpan span, int significantDigits = 3)
        {
            var format = $"G{significantDigits}";
            return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
                    : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
                    : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
                    : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
                    : span.TotalDays.ToString(format) + " days")));
        }

        /// <summary>
        /// Gets the default member name that is used for an indexer (e.g. "Item").
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <returns>Default member name.</returns>
        public static string? GetDefaultMemberName(this Type type)
        {
            DefaultMemberAttribute? defaultMemberAttribute = type.GetTypeInfo().GetCustomAttributes().OfType<DefaultMemberAttribute>().FirstOrDefault();
            return defaultMemberAttribute == null ? null : defaultMemberAttribute.MemberName;
        }

        /// <summary>
        /// Gets a string value from a <see cref="StorageFile"/> located in the application local folder.
        /// </summary>
        /// <param name="fileName">
        /// The relative <see cref="string"/> file path.
        /// </param>
        /// <returns>
        /// The stored <see cref="string"/> value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Exception thrown if the <paramref name="fileName"/> is null or empty.
        /// </exception>
        public static async Task<string> ReadLocalFileAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (App.IsPackaged)
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.GetFileAsync(fileName);
                return await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            }
            else
            {
                using (TextReader reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, fileName)))
                {
                    return await reader.ReadToEndAsync(); // uses UTF8 by default
                }
            }
        }


        /// <summary>
        /// IEnumerable file reader.
        /// </summary>
        public static IEnumerable<string> ReadFileLines(string path)
        {
            string line = string.Empty;

            if (!File.Exists(path))
                yield return line;
            else
            {
                using (TextReader reader = File.OpenText(path))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }

        /// <summary>
        /// IAsyncEnumerable file reader.
        /// </summary>
        public static async IAsyncEnumerable<string> ReadFileLinesAsync(string path)
        {
            string line = string.Empty;

            if (!File.Exists(path))
                yield return line;
            else
            {
                using (TextReader reader = File.OpenText(path))
                {
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }

        /// <summary>
        /// Starts an animation and returns a <see cref="Task"/> that reports when it completes.
        /// </summary>
        /// <param name="storyboard">The target storyboard to start.</param>
        /// <returns>A <see cref="Task"/> that completes when <paramref name="storyboard"/> completes.</returns>
        public static Task BeginAsync(this Storyboard storyboard)
        {
            TaskCompletionSource<object?> taskCompletionSource = new TaskCompletionSource<object?>();

            void OnCompleted(object? sender, object e)
            {
                if (sender is Storyboard storyboard)
                    storyboard.Completed -= OnCompleted;

                taskCompletionSource.SetResult(null);
            }

            storyboard.Completed += OnCompleted;
            storyboard.Begin();

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// To get all buttons contained in a StackPanel:
        /// IEnumerable{Button} kids = GetChildren(rootStackPanel).Where(ctrl => ctrl is Button).Cast{Button}();
        /// </summary>
        /// <remarks>You must call this on a UI thread.</remarks>
        public static IEnumerable<UIElement> GetChildren(this UIElement parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                if (VisualTreeHelper.GetChild(parent, i) is UIElement child)
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Walks the visual tree to determine if a particular child is contained within a parent DependencyObject.
        /// </summary>
        /// <param name="element">Parent DependencyObject</param>
        /// <param name="child">Child DependencyObject</param>
        /// <returns>True if the parent element contains the child</returns>
        public static bool ContainsChild(this DependencyObject element, DependencyObject child)
        {
            if (element != null)
            {
                while (child != null)
                {
                    if (child == element)
                        return true;

                    // Walk up the visual tree.  If the root is hit, try using the framework element's
                    // parent.  This is done because Popups behave differently with respect to the visual tree,
                    // and it could have a parent even if the VisualTreeHelper doesn't find it.
                    DependencyObject parent = VisualTreeHelper.GetParent(child);
                    if (parent == null)
                    {
                        FrameworkElement? childElement = child as FrameworkElement;
                        if (childElement != null)
                        {
                            parent = childElement.Parent;
                        }
                    }
                    child = parent;
                }
            }
            return false;
        }

        /// <summary>
        /// Provides the distance in a <see cref="Point"/> from the passed in element to the element being called on.
        /// For instance, calling child.CoordinatesFrom(container) will return the position of the child within the container.
        /// Helper for <see cref="UIElement.TransformToVisual(UIElement)"/>.
        /// </summary>
        /// <param name="target">Element to measure distance.</param>
        /// <param name="parent">Starting parent element to provide coordinates from.</param>
        /// <returns><see cref="Point"/> containing difference in position of elements.</returns>
        public static Windows.Foundation.Point CoordinatesFrom(this UIElement target, UIElement parent)
        {
            return target.TransformToVisual(parent).TransformPoint(default(Windows.Foundation.Point));
        }

        /// <summary>
        /// Provides the distance in a <see cref="Point"/> to the passed in element from the element being called on.
        /// For instance, calling container.CoordinatesTo(child) will return the position of the child within the container.
        /// Helper for <see cref="UIElement.TransformToVisual(UIElement)"/>.
        /// </summary>
        /// <param name="parent">Starting parent element to provide coordinates from.</param>
        /// <param name="target">Element to measure distance to.</param>
        /// <returns><see cref="Point"/> containing difference in position of elements.</returns>
        public static Windows.Foundation.Point CoordinatesTo(this UIElement parent, UIElement target)
        {
            return target.TransformToVisual(parent).TransformPoint(default(Windows.Foundation.Point));
        }


        /// <summary>
        /// I created this to show what controls are members of <see cref="Microsoft.UI.Xaml.FrameworkElement"/>.
        /// </summary>
        public static void FindControlsInheritingFromFrameworkElement()
        {
            var controlAssembly = typeof(Microsoft.UI.Xaml.Controls.Control).GetTypeInfo().Assembly;
            var controlTypes = controlAssembly.GetTypes()
                .Where(type => type.Namespace == "Microsoft.UI.Xaml.Controls" &&
                typeof(Microsoft.UI.Xaml.FrameworkElement).IsAssignableFrom(type));

            foreach (var controlType in controlTypes)
            {
                Debug.WriteLine($"[FrameworkElement] {controlType.FullName}", $"ControlInheritingFrom");
            }
        }

        /// <summary>
        /// Returns a random selection from <see cref="Microsoft.UI.Colors"/>.
        /// </summary>
        /// <returns><see cref="Windows.UI.Color"/></returns>
        public static Windows.UI.Color GetRandomMicrosoftUIColor()
        {
            try
            {
                var colorType = typeof(Microsoft.UI.Colors);
                var colors = colorType.GetProperties()
                    .Where(p => p.PropertyType == typeof(Windows.UI.Color) && p.GetMethod != null && p.GetMethod.IsStatic && p.GetMethod.IsPublic)
                    .Select(p => (Windows.UI.Color?)p.GetValue(null) ?? Windows.UI.Color.FromArgb(255, 255, 0, 0))
                    .ToList();

                if (colors.Count > 0)
                {
                    var randomIndex = Random.Shared.Next(colors.Count);
                    var randomColor = colors[randomIndex];
                    return randomColor;
                }
                else
                {
                    return Microsoft.UI.Colors.Gray;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetRandomColor: {ex.Message}", $"{nameof(Extensions)}");
                return Microsoft.UI.Colors.Red;
            }
        }

        /// <summary>
        /// Creates a Color object from the hex color code and returns the result.
        /// </summary>
        /// <param name="hexColorCode"></param>
        /// <returns></returns>
        public static Windows.UI.Color? GetColorFromHexString(string hexColorCode)
        {
            if (string.IsNullOrEmpty(hexColorCode)) { return null; }

            try
            {
                byte a = 255; byte r = 0; byte g = 0; byte b = 0;

                if (hexColorCode.Length == 9)
                {
                    hexColorCode = hexColorCode.Substring(1, 8);
                }

                if (hexColorCode.Length == 8)
                {
                    a = Convert.ToByte(hexColorCode.Substring(0, 2), 16);
                    hexColorCode = hexColorCode.Substring(2, 6);
                }

                if (hexColorCode.Length == 6)
                {
                    r = Convert.ToByte(hexColorCode.Substring(0, 2), 16);
                    g = Convert.ToByte(hexColorCode.Substring(2, 2), 16);
                    b = Convert.ToByte(hexColorCode.Substring(4, 2), 16);
                }

                return Windows.UI.Color.FromArgb(a, r, g, b);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}", $"{nameof(Extensions)}");
                return null;
            }
        }

        /// <summary>
        /// Generates a 7 digit color string including the # sign.
        /// If the <see cref="ElementTheme"/> is dark then 0, 1 & 2 options are 
        /// removed so dark colors such as 000000/111111/222222 are not possible.
        /// If the <see cref="ElementTheme"/> is light then D, E & F options are 
        /// removed so light colors such as DDDDDD/EEEEEE/FFFFFF are not possible.
        /// </summary>
        public static string GetRandomColorString(ElementTheme? theme)
        {
            StringBuilder sb = new StringBuilder();
            string pwChars = "012346789ABCDEF";

            if (theme.HasValue && theme == ElementTheme.Dark)
                pwChars = "346789ABCDEF";
            else if (theme.HasValue && theme == ElementTheme.Light)
                pwChars = "012346789ABC";

            char[] charArray = pwChars.Distinct().ToArray();
            var result = new char[7];

            for (int x = 0; x < 6; x++)
                sb.Append(pwChars[Random.Shared.Next() % pwChars.Length]);

            return $"#{sb}";
        }

        /// <summary>
        /// Calculates the linear interpolated Color based on the given Color values.
        /// </summary>
        /// <param name="colorFrom">Source Color.</param>
        /// <param name="colorTo">Target Color.</param>
        /// <param name="amount">Weightage given to the target color.</param>
        /// <returns>Linear Interpolated Color.</returns>
        public static Windows.UI.Color Lerp(this Windows.UI.Color colorFrom, Windows.UI.Color colorTo, float amount)
        {
            // Convert colorFrom components to lerp-able floats
            float sa = colorFrom.A,
                sr = colorFrom.R,
                sg = colorFrom.G,
                sb = colorFrom.B;

            // Convert colorTo components to lerp-able floats
            float ea = colorTo.A,
                er = colorTo.R,
                eg = colorTo.G,
                eb = colorTo.B;

            // lerp the colors to get the difference
            byte a = (byte)Math.Max(0, Math.Min(255, sa.Lerp(ea, amount))),
                r = (byte)Math.Max(0, Math.Min(255, sr.Lerp(er, amount))),
                g = (byte)Math.Max(0, Math.Min(255, sg.Lerp(eg, amount))),
                b = (byte)Math.Max(0, Math.Min(255, sb.Lerp(eb, amount)));

            // return the new color
            return Windows.UI.Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Darkens the color by the given percentage using lerp.
        /// </summary>
        /// <param name="color">Source color.</param>
        /// <param name="amount">Percentage to darken. Value should be between 0 and 1.</param>
        /// <returns>Color</returns>
        public static Windows.UI.Color DarkerBy(this Windows.UI.Color color, float amount)
        {
            return color.Lerp(Microsoft.UI.Colors.Black, amount);
        }

        /// <summary>
        /// Lightens the color by the given percentage using lerp.
        /// </summary>
        /// <param name="color">Source color.</param>
        /// <param name="amount">Percentage to lighten. Value should be between 0 and 1.</param>
        /// <returns>Color</returns>
        public static Windows.UI.Color LighterBy(this Windows.UI.Color color, float amount)
        {
            return color.Lerp(Microsoft.UI.Colors.White, amount);
        }

        /// <summary>
        /// Multiply color bytes by <paramref name="factor"/>, default value is 1.5
        /// </summary>
        public static Windows.UI.Color LightenColor(this Windows.UI.Color source, float factor = 1.5F)
        {
            var red = (int)((float)source.R * factor);
            var green = (int)((float)source.G * factor);
            var blue = (int)((float)source.B * factor);

            if (red == 0) { red = 0x1F; }
            else if (red > 255) { red = 0xFF; }
            if (green == 0) { green = 0x1F; }
            else if (green > 255) { green = 0xFF; }
            if (blue == 0) { blue = 0x1F; }
            else if (blue > 255) { blue = 0xFF; }

            return Windows.UI.Color.FromArgb((byte)255, (byte)red, (byte)green, (byte)blue);
        }

        /// <summary>
        /// Divide color bytes by <paramref name="factor"/>, default value is 1.5
        /// </summary>
        public static Windows.UI.Color DarkenColor(this Windows.UI.Color source, float factor = 1.5F)
        {
            if (source.R == 0) { source.R = 2; }
            if (source.G == 0) { source.G = 2; }
            if (source.B == 0) { source.B = 2; }

            var red = (int)((float)source.R / factor);
            var green = (int)((float)source.G / factor);
            var blue = (int)((float)source.B / factor);

            return Windows.UI.Color.FromArgb((byte)255, (byte)red, (byte)green, (byte)blue);
        }

        /// <summary>
        /// Converts a <see cref="Color"/> to a hexadecimal string representation.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The hexadecimal string representation of the color.</returns>
        public static string ToHex(this Windows.UI.Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from a XAML color string.
        /// Any format used in XAML should work.
        /// </summary>
        /// <param name="colorString">The XAML color string.</param>
        /// <returns>The created <see cref="Color"/>.</returns>
        public static Windows.UI.Color? ToColor(this string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                throw new ArgumentException($"The parameter \"{nameof(colorString)}\" must not be null or empty.");

            if (colorString[0] == '#')
            {
                switch (colorString.Length)
                {
                    case 9:
                        {
                            var cuint = Convert.ToUInt32(colorString.Substring(1), 16);
                            var a = (byte)(cuint >> 24);
                            var r = (byte)((cuint >> 16) & 0xff);
                            var g = (byte)((cuint >> 8) & 0xff);
                            var b = (byte)(cuint & 0xff);
                            return Windows.UI.Color.FromArgb(a, r, g, b);
                        }
                    case 7:
                        {
                            var cuint = Convert.ToUInt32(colorString.Substring(1), 16);
                            var r = (byte)((cuint >> 16) & 0xff);
                            var g = (byte)((cuint >> 8) & 0xff);
                            var b = (byte)(cuint & 0xff);
                            return Windows.UI.Color.FromArgb(255, r, g, b);
                        }
                    case 5:
                        {
                            var cuint = Convert.ToUInt16(colorString.Substring(1), 16);
                            var a = (byte)(cuint >> 12);
                            var r = (byte)((cuint >> 8) & 0xf);
                            var g = (byte)((cuint >> 4) & 0xf);
                            var b = (byte)(cuint & 0xf);
                            a = (byte)(a << 4 | a);
                            r = (byte)(r << 4 | r);
                            g = (byte)(g << 4 | g);
                            b = (byte)(b << 4 | b);
                            return Windows.UI.Color.FromArgb(a, r, g, b);
                        }
                    case 4:
                        {
                            var cuint = Convert.ToUInt16(colorString.Substring(1), 16);
                            var r = (byte)((cuint >> 8) & 0xf);
                            var g = (byte)((cuint >> 4) & 0xf);
                            var b = (byte)(cuint & 0xf);
                            r = (byte)(r << 4 | r);
                            g = (byte)(g << 4 | g);
                            b = (byte)(b << 4 | b);
                            return Windows.UI.Color.FromArgb(255, r, g, b);
                        }
                    default: return ThrowFormatException();
                }
            }

            if (colorString.Length > 3 && colorString[0] == 's' && colorString[1] == 'c' && colorString[2] == '#')
            {
                var values = colorString.Split(',');

                if (values.Length == 4)
                {
                    var scA = double.Parse(values[0].Substring(3), CultureInfo.InvariantCulture);
                    var scR = double.Parse(values[1], CultureInfo.InvariantCulture);
                    var scG = double.Parse(values[2], CultureInfo.InvariantCulture);
                    var scB = double.Parse(values[3], CultureInfo.InvariantCulture);

                    return Windows.UI.Color.FromArgb((byte)(scA * 255), (byte)(scR * 255), (byte)(scG * 255), (byte)(scB * 255));
                }

                if (values.Length == 3)
                {
                    var scR = double.Parse(values[0].Substring(3), CultureInfo.InvariantCulture);
                    var scG = double.Parse(values[1], CultureInfo.InvariantCulture);
                    var scB = double.Parse(values[2], CultureInfo.InvariantCulture);

                    return Windows.UI.Color.FromArgb(255, (byte)(scR * 255), (byte)(scG * 255), (byte)(scB * 255));
                }

                return ThrowFormatException();
            }

            var prop = typeof(Microsoft.UI.Colors).GetTypeInfo().GetDeclaredProperty(colorString);

            if (prop != null)
                return (Windows.UI.Color?)prop.GetValue(null) ?? Windows.UI.Color.FromArgb(255, 198, 87, 88);

            return ThrowFormatException();

            static Windows.UI.Color ThrowFormatException() => throw new FormatException($"The parameter \"{nameof(colorString)}\" is not a recognized Color format.");
        }

        public static bool IsAppPackaged()
        {
            return IsMSIX;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
        public static bool IsMSIX
        {
            get
            {
                var length = 0;
                return GetCurrentPackageFullName(ref length, null) != 15700L;
            }
        }

        /// <summary>
        /// Get OS version by way of <see cref="Environment.OSVersion"/>.
        /// </summary>
        /// <returns>true if Win11 or higher, false otherwise</returns>
        public static bool IsWindows11OrGreater() => Environment.OSVersion.Version >= new Version(10, 0, 22000, 0);
    }
}
