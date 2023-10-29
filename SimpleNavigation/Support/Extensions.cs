using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Management.Deployment;
using System.Collections;
using System.Threading;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using System.Collections.Specialized;

namespace SimpleNavigation
{
    public static class Extensions
    {
        /// <summary>
        /// InfoBadgeControl.Icon = (IconElement)new FontIcon() { Glyph = IntToUTF16(0xE701) };
        /// </summary>
        public static IconElement GetIcon(string imagePath)
        {
            return imagePath.ToLowerInvariant().EndsWith(".png") ?
                        (IconElement)new BitmapIcon() { UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute), ShowAsMonochrome = false } :
                        (IconElement)new FontIcon() { Glyph = imagePath };
        }

		/// <summary>
		/// string fin = IntToUTF16((int)FluentIcon.MapPin);
		/// https://stackoverflow.com/questions/71546789/the-u-escape-sequence-in-c-sharp
		/// </summary>
		public static string IntToUTF16(int value)
        {
            var builder = new StringBuilder();
            builder.Append((char)value);
            return builder.ToString();
        }

        /// <summary>
        /// string[] names = System.Enum.GetNames(typeof(FluentIcon));
        /// int[] values = EnumToIntArray{FluentIcon}(names);
        /// </summary>
        public static int[] EnumToIntArray<TEnum>(this string[] enumStrings) where TEnum : struct
        {
            List<int> values = new();
            foreach (string s in enumStrings)
            {
                if (Enum.TryParse(s, ignoreCase: true, out TEnum result))
                {
                    // Using the Convert class:
					int intVal = Convert.ToInt32(result);
                    
                    // Using an explicit cast:
					//int intVal = (int)(object)result;

					values.Add(intVal);
                }
            }
            return values.ToArray();
        }

        /// <summary>
        /// This is a slightly more generic version which omits the TEnum:struct signature.
        /// Loops through all enums of type T using <see cref="Enum.GetNames(Type)"/>.
        /// </summary>
        /// <returns>true if enum was parsed successfully, false otherwise</returns>
        /// <example>
        /// if ("CurrentCultureIgnoreCase".EnumTryParse(out StringComparison ccic)) { Debug.WriteLine("Converted Successfully"); }
        /// </example>
        public static bool EnumTryParse<T>(this string input, out T? outEnum)
        {
            foreach (string en in Enum.GetNames(typeof(T)))
            {
                if (en.Equals(input, StringComparison.CurrentCultureIgnoreCase))
                {
                    outEnum = (T?)Enum.Parse(typeof(T), input, true);
                    return true;
                }
            }
            outEnum = default;
            return false;
        }

        /// <summary>
        /// This is a slightly more generic version which omits the TEnum:struct signature.
        /// Returns the enum directly without using a TryParse style.
        /// </summary>
        /// <example>
        /// var ccic = "CurrentCultureIgnoreCase".ToEnum{StringComparison}();
        /// </example>
        public static TEnum? ToEnum<TEnum>(this string value)
        {
            if (string.IsNullOrEmpty(value)) 
                return default;
            
            var parsed = Enum.Parse(typeof(TEnum), value, true);

            if (parsed == null)
                return default;

            return (TEnum)parsed;
        }

        /// <summary>
        /// A random <see cref="Boolean"/> generator.
        /// </summary>
        public static bool CoinFlip() => (Random.Shared.Next(100) > 49) ? true : false;

        /// <summary>
        /// Determines if two <see cref="double"/>s are within a margin of each other.
        /// </summary>
        public static bool IsSimilarTo(this double first, double second, double margin) => Math.Abs(first - second) <= margin;

        /// <summary>
        /// Randomly shuffle <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;

            for (int i = list.Count - 1; i > 1; i--)
            {
                int rnd = Random.Shared.Next(i + 1);
                T value = list[rnd];
                list[rnd] = list[i];
                list[i] = value;
            }
        }

        /// <summary>
        /// Basic key/pswd generator for unique IDs.
        /// This employs the standard MS key table which accounts
        /// for the 36 Latin letters and Arabic numerals used in
        /// most Western European languages...
        /// 24 chars are favored: 2346789 BCDFGHJKMPQRTVWXY
        /// 12 chars are avoided: 015 AEIOU LNSZ
        /// Only 2 chars are occasionally mistaken: 8 and B (depends on the font).
        /// The base of possible codes is large (about 3.2 * 10^34).
        /// </summary>
        public static string KeyGen(int pLength = 6, long pSeed = 0)
        {
            const string pwChars = "2346789BCDFGHJKMPQRTVWXY";
            if (pLength < 6)
                pLength = 6; // minimum of 6 characters

            char[] charArray = pwChars.Distinct().ToArray();

            if (pSeed == 0)
            {
                pSeed = DateTime.Now.Ticks;
                Thread.Sleep(1); // allow a tick to go by (if hammering)
            }

            var result = new char[pLength];
            var rng = new Random((int)pSeed);

            for (int x = 0; x < pLength; x++)
                result[x] = pwChars[rng.Next() % pwChars.Length];

            return (new string(result));
        }

        /// <summary>
        /// XML formatting helper.
        /// </summary>
        /// <param name="xml">input string</param>
        /// <returns>formatted string</returns>
        public static string PrettyXml(this string xml)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                var element = System.Xml.Linq.XElement.Parse(xml);
                var settings = new System.Xml.XmlWriterSettings();
                settings.OmitXmlDeclaration = true;
                settings.Indent = true;
                settings.NewLineOnAttributes = true;
                // XmlWriter offers a StringBuilder as an output.
                using (var xmlWriter = System.Xml.XmlWriter.Create(stringBuilder, settings))
                {
                    element.Save(xmlWriter);
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PrettyXml: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Debugging helper method.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>type name then base type for the object</returns>
        public static string NameOf(this object obj) => $"{obj.GetType().Name} => {obj.GetType().BaseType?.Name}";

        /// <summary>
        /// var vm = frame.GetPageViewModel();
        /// </summary>
        public static object? GetPageViewModel(this Frame frame)
        {
            return frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
        }

        /// <summary>
        /// Opens the specified file without blocking the current thread.
        /// </summary>
        /// <param name="filePath">full path including file name</param>
        /// <param name="arguments">optional arguments</param>
        public static void OpenFile(string filePath, string arguments = "")
        {
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    Task.Run(() => {
                        System.Diagnostics.ProcessStartInfo startInfo = new()
                        {
                            FileName = filePath,
                            Arguments = arguments,
                            UseShellExecute = true // <-- very important! 
                        };
                        System.Diagnostics.Process.Start(startInfo);
                    });
                }
                catch (Exception ex) { Debug.WriteLine($"OpenFile: {ex.Message}"); }
            }
            else
            {
                Debug.WriteLine($"OpenFile: '{System.IO.Path.GetFileName(filePath)}' does not exist.");
            }
        }

        #region [UI Helpers]
        public static IEnumerable<T> GetDescendantsOfType<T>(this DependencyObject start) where T : DependencyObject
        {
            return start.GetDescendants().OfType<T>();
        }

        public static IEnumerable<DependencyObject> GetDescendants(this DependencyObject start)
        {
            var queue = new Queue<DependencyObject>();
            var parentCount = VisualTreeHelper.GetChildrenCount(start);
            for (int i = 0; i < parentCount; i++)
            {
                var child = VisualTreeHelper.GetChild(start, i);
                yield return child;
                queue.Enqueue(child);
            }

            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                var childCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    yield return child;
                    queue.Enqueue(child);
                }
            }
        }

        public static UIElement? FindElementByName(this UIElement element, string name)
        {
            if (element.XamlRoot != null && element.XamlRoot.Content != null)
            {
                var ele = (element.XamlRoot.Content as FrameworkElement)?.FindName(name);
                if (ele != null)
                    return ele as UIElement;
            }
            return null;
        }

        public static IEnumerable<Type> GetHierarchyFromUIElement(this Type element)
        {
            if (element.GetTypeInfo().IsSubclassOf(typeof(UIElement)) != true)
            {
                yield break;
            }

            Type current = element;

            while (current != null && current != typeof(UIElement))
            {
                yield return current;
                current = current.GetTypeInfo().BaseType;
            }
        }

        public static void DisplayRoutedEventsForUIElement()
        {
            Type uiElementType = typeof(UIElement);
            var routedEvents = uiElementType.GetEvents();
            Debug.WriteLine($"[All RoutedEvents for UIElement]");
            foreach (var routedEvent in routedEvents)
            {
                if (routedEvent.EventHandlerType == typeof(RoutedEventHandler) ||
                    routedEvent.EventHandlerType == typeof(RoutedEvent) ||
                    routedEvent.EventHandlerType == typeof(EventHandler))
                {
                    Debug.WriteLine($" - '{routedEvent.Name}'");
                }
                else if (routedEvent.MemberType == MemberTypes.Event)
                {
                    Debug.WriteLine($" - '{routedEvent.Name}'");
                }
            }
        }

        public static void DisplayRoutedEventsForFrameworkElement()
        {
            Type fwElementType = typeof(FrameworkElement);
            var routedEvents = fwElementType.GetEvents();
            Debug.WriteLine($"[All RoutedEvents for FrameworkElement]");
            foreach (var routedEvent in routedEvents)
            {
                if (routedEvent.EventHandlerType == typeof(RoutedEventHandler) ||
                    routedEvent.EventHandlerType == typeof(RoutedEvent) ||
                    routedEvent.EventHandlerType == typeof(EventHandler))
                {
                    Debug.WriteLine($" - '{routedEvent.Name}'");
                }
                else if (routedEvent.MemberType == MemberTypes.Event)
                {
                    Debug.WriteLine($" - '{routedEvent.Name}'");
                }
            }
        }

        public static void DisplayRoutedEventsForControl()
        {
            Type ctlElementType = typeof(Microsoft.UI.Xaml.Controls.Control);
            var routedEvents = ctlElementType.GetEvents();
            Debug.WriteLine($"[All RoutedEvents for Control]");
            foreach (var routedEvent in routedEvents)
            {
                if (routedEvent.EventHandlerType == typeof(RoutedEventHandler) ||
                    routedEvent.EventHandlerType == typeof(RoutedEvent) ||
                    routedEvent.EventHandlerType == typeof(EventHandler))
                {
                    Debug.WriteLine($" - '{routedEvent.Name}'");
                }
                else if (routedEvent.MemberType == MemberTypes.Event)
                {
                    Debug.WriteLine($" - '{routedEvent.Name}'");
                }
            }
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
                Debug.WriteLine($"[FrameworkElement] {controlType.FullName}");
            }
        }
        #endregion

        #region [Window Helpers]
        /// <summary>
        /// Configures whether the window should always be displayed on top of other windows or not
        /// </summary>
        /// <remarks>The presenter must be an overlapped presenter.</remarks>
        /// <exception cref="NotSupportedException">Throw if the AppWindow Presenter isn't an overlapped presenter.</exception>
        /// <param name="enable">Whether to display on top</param>
        public static void SetIsAlwaysOnTop(bool enable) => UpdateOverlappedPresenter((c) => c.IsAlwaysOnTop = enable);

        /// <summary>
        /// Gets a value indicating whether this window is on top or not.
        /// </summary>
        /// <returns><c>True</c> if the overlapped presenter is on top, otherwise <c>false</c>.</returns>
        public static bool GetIsAlwaysOnTop() => GetOverlappedPresenterValue((c) => c?.IsAlwaysOnTop ?? false);

        /// <summary>
        /// Enables or disables the ability to resize the window.
        /// </summary>
        /// <remarks>The presenter must be an overlapped presenter.</remarks>
        /// <exception cref="NotSupportedException">Throw if the AppWindow Presenter isn't an overlapped presenter.</exception>
        /// <param name="enable"></param>
        public static void SetIsResizable(bool enable) => UpdateOverlappedPresenter((c) => c.IsResizable = enable);

        /// <summary>
        /// Gets a value indicating whether this resizable or not.
        /// </summary>
        /// <returns><c>True</c> if the overlapped presenter is resizeable, otherwise <c>false</c>.</returns>
        public static bool GetIsResizable() => GetOverlappedPresenterValue((c) => c?.IsResizable ?? false);

        /// <summary>
        /// </summary>
        /// <remarks>The presenter must be an overlapped presenter.</remarks>
        /// <exception cref="NotSupportedException">Throw if the AppWindow Presenter isn't an overlapped presenter.</exception>
        /// <param name="enable"><c>true</c> if this window should be maximizable.</param>
        public static void SetIsMaximizable(bool enable) => UpdateOverlappedPresenter((c) => c.IsMaximizable = enable);

        /// <summary>
        /// Gets a value indicating whether this window is maximizeable or not.
        /// </summary>
        /// <returns><c>True</c> if the overlapped presenter is on maximizable, otherwise <c>false</c>.</returns>
        public static bool GetIsMaximizable() => GetOverlappedPresenterValue((c) => c?.IsMaximizable ?? false);

        /// <summary>
        /// </summary>
        /// <remarks>The presenter must be an overlapped presenter.</remarks>
        /// <exception cref="NotSupportedException">Throw if the AppWindow Presenter isn't an overlapped presenter.</exception>
        /// <param name="enable"><c>true</c> if this window should be minimizable.</param>
        public static void SetIsMinimizable(bool enable) => UpdateOverlappedPresenter((c) => c.IsMinimizable = enable);

        /// <summary>
        /// Gets a value indicating whether this window is minimizeable or not.
        /// </summary>
        /// <returns><c>True</c> if the overlapped presenter is on minimizable, otherwise <c>false</c>.</returns>
        public static bool GetIsMinimizable() => GetOverlappedPresenterValue((c) => c?.IsMinimizable ?? false);

        /// <summary>
        /// Enables or disables showing the window in the task switchers.
        /// </summary>
        /// <param name="enable"><c>true</c> if this window should be shown in the task switchers, otherwise <c>false</c>.</param>
        public static void SetIsShownInSwitchers(bool enable)
        {
            if (App.MainWindow is null)
                throw new ArgumentNullException($"'{nameof(App.MainWindow)}' must be initialized before using this method.");

            App.MainWindow.GetAppWindow().IsShownInSwitchers = enable;
        }

        /// <summary>
        /// Sets the window presenter kind used.
        /// </summary>
        /// <param name="kind"><see cref="AppWindowPresenterKind"/></param>
        public static void SetWindowPresenter(AppWindowPresenterKind kind)
        {
            if (App.MainWindow is null)
                throw new ArgumentNullException($"'{nameof(App.MainWindow)}' must be initialized before using this method.");

            App.MainWindow.GetAppWindow().SetPresenter(kind);
        }

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        /// <remarks>The presenter must be an overlapped presenter.</remarks>
        /// <exception cref="NotSupportedException">Throw if the AppWindow Presenter isn't an overlapped presenter.</exception>
        public static void MaximizeWindow() => UpdateOverlappedPresenter((c) => c.Maximize());

        /// <summary>
        /// Minimizes the window and activates the next top-level window in the Z order.
        /// </summary>
        /// <remarks>The presenter must be an overlapped presenter.</remarks>
        /// <exception cref="NotSupportedException">Throw if the AppWindow Presenter isn't an overlapped presenter.</exception>
        public static void MinimizeWindow() => UpdateOverlappedPresenter((c) => c.Minimize());

        /// <summary>
        /// Activates and displays the window. If the window is minimized or 
        /// maximized, the system restores it to its original size and position.
        /// </summary>
        /// <remarks>The presenter must be an overlapped presenter.</remarks>
        /// <exception cref="NotSupportedException">Throw if the AppWindow Presenter isn't an overlapped presenter.</exception>
        public static void RestoreWindow() => UpdateOverlappedPresenter((c) => c.Restore());

        /// <summary>
        /// Gets the background color for the title bar and all its buttons and their states.
        /// </summary>
        /// <param name="window">window</param>
        /// <param name="color">color</param>
        public static void SetTitleBarBackgroundColors(this Microsoft.UI.Xaml.Window window, Windows.UI.Color color)
        {
            var appWindow = window.GetAppWindow();
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ButtonBackgroundColor = color;
                appWindow.TitleBar.BackgroundColor = color;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = color;
                appWindow.TitleBar.ButtonPressedBackgroundColor = color;
                appWindow.TitleBar.InactiveBackgroundColor = color;
            }
        }

        /// <summary>
        /// Helper method for SetIs___ methods.
        /// An overlapped window has a title bar and a border.
        /// An OVERLAPPED window is the same as a TILEDWINDOW.
        /// </summary>
        private static void UpdateOverlappedPresenter(Action<OverlappedPresenter> action)
        {
            if (App.MainWindow is null)
                throw new ArgumentNullException($"'{nameof(App.MainWindow)}' must be initialized before using this method.");

            var appWindow = App.MainWindow.GetAppWindow();
            if (appWindow.Presenter is OverlappedPresenter overlapped)
                action(overlapped);
            else
                throw new NotSupportedException($"Not supported with a {appWindow.Presenter.Kind} presenter.");
        }

        /// <summary>
        /// Helper method for GetIs___ methods.
        /// </summary>
        private static T GetOverlappedPresenterValue<T>(Func<OverlappedPresenter?, T> action)
        {
            if (App.MainWindow is null)
                throw new ArgumentNullException($"'{nameof(App.MainWindow)}' must be initialized before using this method.");

            var appWindow = App.MainWindow.GetAppWindow();
            return action(appWindow.Presenter as OverlappedPresenter);
        }

        /// <summary>
        /// This code example demonstrates how to retrieve an AppWindow from a WinUI3 window.
        /// The AppWindow class is available for any top-level HWND in your app.
        /// AppWindow is available only to desktop apps (both packaged and unpackaged), it's not available to UWP apps.
        /// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview
        /// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.create?view=windows-app-sdk-1.3
        /// </summary>
        public static Microsoft.UI.Windowing.AppWindow? GetAppWindow(this object window)
        {
            // Retrieve the window handle (HWND) of the current (XAML) WinUI3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            // Retrieve the WindowId that corresponds to hWnd.
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            // Lastly, retrieve the AppWindow for the current (XAML) WinUI3 window.
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        }

        /// <summary>
        /// Resizes a <see cref="Microsoft.UI.Xaml.Window window"/>.
        /// </summary>
        /// <param name="window"><see cref="Microsoft.UI.Xaml.Window window"/></param>
        /// <param name="width">desired width</param>
        /// <param name="height">desired height</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ResizeWindow(this Microsoft.UI.Xaml.Window window, int width, int height)
        {
            if (window is null)
                throw new ArgumentNullException($"'{nameof(window)}' must be initialized before using this method.");

            var appWindow = window.GetAppWindow();
            appWindow?.Resize(new Windows.Graphics.SizeInt32((width > 0) ? width : 100, (height > 0) ? height : 100));
        }

        /// <summary>
        /// Moves and resizes a <see cref="Microsoft.UI.Xaml.Window window"/>.
        /// </summary>
        /// <param name="window"><see cref="Microsoft.UI.Xaml.Window window"/></param>
        /// <param name="x">desired X coordinate</param>
        /// <param name="y">desired Y coordinate</param>
        /// <param name="width">desired width</param>
        /// <param name="height">desired height</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void MoveAndResizeWindow(this Microsoft.UI.Xaml.Window window, int x, int y, int width, int height)
        {
            if (window is null)
                throw new ArgumentNullException($"'{nameof(window)}' must be initialized before using this method.");

            var appWindow = window.GetAppWindow();
            appWindow?.Move(new Windows.Graphics.PointInt32(x, y));
            appWindow?.Resize(new Windows.Graphics.SizeInt32((width > 0) ? width : 100, (height > 0) ? height : 100));
        }

        /// <summary>
        /// Centers a <see cref="Microsoft.UI.Xaml.Window"/> based on the <see cref="Microsoft.UI.Windowing.DisplayArea"/>.
        /// </summary>
        public static void CenterWindow(this Microsoft.UI.Xaml.Window window)
        {
            try
            {
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                if (Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId) is Microsoft.UI.Windowing.AppWindow appWindow &&
                    Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest) is Microsoft.UI.Windowing.DisplayArea displayArea)
                {
                    Windows.Graphics.PointInt32 CenteredPosition = appWindow.Position;
                    CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                    CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                    appWindow.Move(CenteredPosition);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// The <see cref="Microsoft.UI.Windowing.DisplayArea"/> exposes properties such as:
        /// OuterBounds     (Rect32)
        /// WorkArea.Width  (int)
        /// WorkArea.Height (int)
        /// IsPrimary       (bool)
        /// DisplayId.Value (ulong)
        /// </summary>
        /// <param name="window"></param>
        /// <returns><see cref="DisplayArea"/></returns>
        public static Microsoft.UI.Windowing.DisplayArea? GetDisplayArea(this Microsoft.UI.Xaml.Window window)
        {
            try
            {
                System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                var da = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                return da;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetDisplayArea: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the native HWND pointer handle for the window
        /// </summary>
        /// <param name="window">The window to return the handle for</param>
        /// <returns>HWND handle</returns>
        public static IntPtr GetWindowHandle(this Microsoft.UI.Xaml.Window window)
        {
            return window is null ? throw new ArgumentNullException(nameof(window)) : WinRT.Interop.WindowNative.GetWindowHandle(window);
        }

        /// <summary>
        /// Gets the window HWND handle from a Window ID.
        /// </summary>
        /// <param name="windowId">Window ID to get handle from</param>
        /// <returns>Window HWND handle</returns>
        public static IntPtr GetWindowHandle(this WindowId windowId)
        {
            IntPtr hwnd;
            GetWindowHandleFromWindowId(windowId, out hwnd);
            return hwnd;
        }

        /// <summary>
        /// Gets the AppWindow from an HWND
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns>AppWindow</returns>
        public static AppWindow GetAppWindowFromWindowHandle(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hwnd));
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        [DllImport("Microsoft.Internal.FrameworkUdk.dll", EntryPoint = "Windowing_GetWindowHandleFromWindowId", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr GetWindowHandleFromWindowId(WindowId windowId, out IntPtr result);
        #endregion

        #region [Window Tracking]
        static private List<Window> _activeWindows = new List<Window>();
        /// <summary>
        /// From inside a Page or Control: var window = Extensions.GetWindowForElement(this);
        /// </summary>
        /// <param name="element">the <see cref="UIElement"/> of the control or page</param>
        /// <returns><see cref="Window"/></returns>
        public static Window? GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                        return window;
                }
            }
            return null;
        }
        public static void StoreWindow(Window window)
        {
            if (_activeWindows.Count > 0 && _activeWindows.Contains(window)) { return; }
            window.Closed += (sender, args) => { _activeWindows.Remove(window); };
            _activeWindows.Add(window);
        }
        #endregion

        #region [Data Helpers]
        public static List<Message> GenerateMessages(int amount = 20)
        {
            List<Message> messages = new();
            for (int i = 0; i < amount; i++)
            {
                // The average number of words in a sentence is typically between 15 and 20.
                messages.Add(new Message { Content = Extensions.GetRandomSentence(Random.Shared.Next(8, 20)), Severity = GetRandomSeverity(), Time = DateTime.Now.AddDays(-1 * Random.Shared.Next(1, 31)) });
            }
            return messages.OrderByDescending(o => o.Time).ToList();
        }

        public static InfoBarSeverity GetRandomSeverity()
        {
            switch (Random.Shared.Next(5))
            {
                case 0: return InfoBarSeverity.Error;
                case 1: return InfoBarSeverity.Warning;
                case 2: return InfoBarSeverity.Success;
                default: return InfoBarSeverity.Informational;
            }
        }

        public static bool CheckForCorruptData(List<byte> data)
        {
            if (data.Count == 0)
                return false;

            bool stxPresent = data.First() == 0x02;
            bool etxPresent = data.Last() == 0x03;
            bool anyOtherDataPresent = data.Distinct().Where(p => p != 0x00 && p != 0xFF).Count() > 0;
            if (!stxPresent && !etxPresent && !anyOtherDataPresent)
            {
                Debug.WriteLine("Corrupt data detected.");
                return true;
            }
            return false;
        }
        #endregion

        public static async Task<ulong> GetFolderSize(this Windows.Storage.StorageFolder folder)
        {
            ulong res = 0;
            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                Windows.Storage.FileProperties.BasicProperties properties = await file.GetBasicPropertiesAsync();
                res += properties.Size;
            }

            foreach (Windows.Storage.StorageFolder subFolder in await folder.GetFoldersAsync())
            {
                try
                {
                    res += await GetFolderSize(subFolder);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GetFolderSize: {ex.Message}", $"{nameof(Extensions)}");
                }
            }
            return res;
        }

        public static async Task<byte[]?> ReadBytesAsync(this Windows.Storage.StorageFile file)
        {
            if (file != null)
            {
                using IRandomAccessStream stream = await file.OpenReadAsync();
                using var reader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0));
                await reader.LoadAsync((uint)stream.Size);
                var bytes = new byte[stream.Size];
                reader.ReadBytes(bytes);
                return bytes;
            }
            return null;
        }

        public static async Task<byte[]?> ReadBytesAsync(this Windows.Storage.StorageFolder folder, string fileName)
        {
            var item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);
            if ((item != null) && item.IsOfType(Windows.Storage.StorageItemTypes.File))
            {
                var storageFile = await folder.GetFileAsync(fileName);
                var content = await storageFile.ReadBytesAsync();
                return content;
            }
            return null;
        }

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
                    if (excludeWinSys &&
                        !fn.StartsWith(winSys, StringComparison.OrdinalIgnoreCase) &&
                        !fn.StartsWith(winProg, StringComparison.OrdinalIgnoreCase) &&
                        !fn.EndsWith($"{self}.exe", StringComparison.OrdinalIgnoreCase))
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

        public static void CopyEmbeddedResourceToFile(this Assembly assembly, string embeddedResourcesPath, string folder, string resourceName)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using var image = assembly.GetManifestResourceStream(embeddedResourcesPath + resourceName);
            if (image == null)
                throw new ArgumentException("EmbeddedResource not found");
            var destPath = Path.Combine(folder, resourceName);
            using var dest = File.Create(destPath);
            image.CopyTo(dest);
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

        // Matches non-conforming unicode chars
        // https://mnaoumov.wordpress.com/2014/06/14/stripping-invalid-characters-from-utf-16-strings/
        private static readonly Regex _nonConformingUnicode = new Regex("([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])|(\ufffd)");

        /// <summary>
        /// Removes the diacritics character from the strings.
        /// </summary>
        /// <param name="text">The string to act on.</param>
        /// <returns>The string without diacritics character.</returns>
        public static string RemoveDiacritics(this string text)
        {
            string withDiactritics = _nonConformingUnicode
                .Replace(text, string.Empty)
                .Normalize(NormalizationForm.FormD);

            var withoutDiactritics = new StringBuilder();
            foreach (char c in withDiactritics)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    withoutDiactritics.Append(c);
                }
            }

            return withoutDiactritics.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Checks whether or not the specified string has diacritics in it.
        /// </summary>
        /// <param name="text">The string to check.</param>
        /// <returns>True if the string has diacritics, false otherwise.</returns>
        public static bool HasDiacritics(this string text)
        {
            return !string.Equals(text, text.RemoveDiacritics(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Counts the number of occurrences of [needle] in the string.
        /// </summary>
        /// <param name="value">The haystack to search in.</param>
        /// <param name="needle">The character to search for.</param>
        /// <returns>The number of occurrences of the [needle] character.</returns>
        public static int Count(this ReadOnlySpan<char> value, char needle)
        {
            var count = 0;
            var length = value.Length;
            for (var i = 0; i < length; i++)
            {
                if (value[i] == needle)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Helper method example using <see cref="StringBuilder"/>.
        /// </summary>
        /// <example>
        /// ExampleTextSample => Example Text Sample
        /// </example>
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
        /// Helper method example using <see cref="Regex"/>.
        /// </summary>
        /// <example>
        /// ExampleTextSample => Example Text Sample
        /// </example>
        /// <param name="input"></param>
        /// <returns>space delimited string</returns>
        public static string SplitCamelCase(this string input)
        {
            return Regex.Replace(Regex.Replace(
                    input,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                    ),
                    @"(\p{Ll})(\P{Ll})",
                    "$1 $2"
            );
        }

        /// <summary>
        /// Returns the field names and their types for a specific class.
        /// </summary>
        /// <param name="myType"></param>
        /// <example>
        /// var dict = ReflectFieldInfo(typeof(MainPage));
        /// </example>
        public static Dictionary<string, Type> ReflectFieldInfo(Type myType)
        {
            Dictionary<string, Type> results = new();
            FieldInfo[] myFieldInfo = myType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < myFieldInfo.Length; i++) { results[myFieldInfo[i].Name] = myFieldInfo[i].FieldType; }
            return results;
        }

        /// <summary>
        /// Determines whether the value is contained in the source collection.
        /// </summary>
        /// <param name="source">An instance of the <see cref="IEnumerable{String}"/> interface.</param>
        /// <param name="value">The value to look for in the collection.</param>
        /// <param name="stringComparison">The string comparison.</param>
        /// <returns>A value indicating whether the value is contained in the collection.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public static bool Contains(this IEnumerable<string> source, ReadOnlySpan<char> value, StringComparison stringComparison)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (source is IList<string> list)
            {
                int len = list.Count;
                for (int i = 0; i < len; i++)
                {
                    if (value.Equals(list[i], stringComparison))
                        return true;
                }

                return false;
            }

            foreach (string element in source)
            {
                if (value.Equals(element, stringComparison))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets an IEnumerable from a single item.
        /// </summary>
        /// <param name="item">The item to return.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns>The IEnumerable{T}.</returns>
        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
        {
            yield return item;
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

        /// <summary>
        /// This method will find all occurrences of a string pattern that starts with a double 
        /// quote, followed by any number of characters (non-greedy), and ends with a double 
        /// quote followed by zero or more spaces and a colon. This pattern matches the typical 
        /// format of keys in a JSON string.
        /// </summary>
        /// <param name="jsonString">JSON formatted text</param>
        /// <returns><see cref="List{T}"/> of each key</returns>
        public static List<string> ExtractKeys(string jsonString)
        {
            var keys = new List<string>();
            var matches = Regex.Matches(jsonString, "[,\\{]\"(.*?)\"\\s*:");
            foreach (Match match in matches) { keys.Add(match.Groups[1].Value); }
            return keys;
        }

        /// <summary>
        /// This method will find all occurrences of a string pattern that starts with a colon, 
        /// followed by zero or more spaces, followed by any number of characters (non-greedy), 
        /// and ends with a comma, closing brace, or closing bracket. This pattern matches the 
        /// typical format of values in a JSON string.
        /// </summary>
        /// <param name="jsonString">JSON formatted text</param>
        /// <returns><see cref="List{T}"/> of each value</returns>
        public static List<string> ExtractValues(string jsonString)
        {
            var values = new List<string>();
            var matches = Regex.Matches(jsonString, ":\\s*(.*?)(,|}|\\])");
            foreach (Match match in matches) { values.Add(match.Groups[1].Value.Trim()); }
            return values;
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

        public static void BindCenterPoint(this Microsoft.UI.Composition.Visual target)
        {
            var exp = target.Compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0f)");
            target.StartAnimation("CenterPoint", exp);
        }

        public static void BindSize(this Microsoft.UI.Composition.Visual target, Microsoft.UI.Composition.Visual source)
        {
            var exp = target.Compositor.CreateExpressionAnimation("host.Size");
            exp.SetReferenceParameter("host", source);
            target.StartAnimation("Size", exp);
        }

        public static Microsoft.UI.Composition.ImplicitAnimationCollection CreateImplicitAnimation(this Microsoft.UI.Composition.ImplicitAnimationCollection source, string Target, TimeSpan? Duration = null)
        {
            Microsoft.UI.Composition.KeyFrameAnimation animation = null;
            switch (Target.ToLower())
            {
                case "offset":
                case "scale":
                case "centerPoint":
                case "rotationAxis":
                    animation = source.Compositor.CreateVector3KeyFrameAnimation();
                    break;

                case "size":
                    animation = source.Compositor.CreateVector2KeyFrameAnimation();
                    break;

                case "opacity":
                case "blueRadius":
                case "rotationAngle":
                case "rotationAngleInDegrees":
                    animation = source.Compositor.CreateScalarKeyFrameAnimation();
                    break;

                case "color":
                    animation = source.Compositor.CreateColorKeyFrameAnimation();
                    break;
            }

            if (animation == null) throw new ArgumentNullException("Unknown Target");
            if (!Duration.HasValue) Duration = TimeSpan.FromSeconds(0.2d);
            animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
            animation.Duration = Duration.Value;
            animation.Target = Target;

            source[Target] = animation;
            return source;
        }

        /// <summary>
        /// Dictionary<char, int> charCount = GetCharacterCount("some input text string here");
        /// foreach (var kvp in charCount) { Debug.WriteLine($"Character: {kvp.Key}, Count: {kvp.Value}"); }
        /// </summary>
        /// <param name="input">the text string to analyze</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
        public static Dictionary<char, int> GetCharacterCount(this string input)
        {
            Dictionary<char, int> charCount = new();

            if (string.IsNullOrEmpty(input))
                return charCount;

            foreach (var ch in input)
            {
                if (charCount.ContainsKey(ch))
                    charCount[ch]++;
                else
                    charCount[ch] = 1;
            }

            return charCount;
        }

        /// <summary>
        /// Gets all the paragraphs in a given markdown document.
        /// </summary>
        /// <param name="text">The input markdown document.</param>
        /// <returns>The raw paragraphs from <paramref name="text"/>.</returns>
        public static IReadOnlyDictionary<string, string> GetParagraphs(this string text)
        {
            return Regex.Matches(text, @"(?<=\W)#+ ([^\n]+).+?(?=\W#|$)", RegexOptions.Singleline)
               .OfType<Match>()
               .ToDictionary(
                 m => Regex.Replace(m.Groups[1].Value.Trim().Replace("&lt;", "<"), @"\[([^]]+)\]\([^)]+\)", m => m.Groups[1].Value),
                 m => m.Groups[0].Value.Trim().Replace("&lt;", "<").Replace("[!WARNING]", "**WARNING:**").Replace("[!NOTE]", "**NOTE:**"));
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
        /// Uses an operator for the current and previous item.
        /// Needs only a single iteration to process pairs and produce an output.
        /// </summary>
        /// <example>
        /// var avg = collection.Pairwise((a, b) => (b.DateTime - a.DateTime)).Average(ts => ts.TotalMinutes);
        /// </example>
        public static IEnumerable<TResult> Pairwise<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));

            return _(); IEnumerable<TResult> _()
            {
                using var e = source.GetEnumerator();

                if (!e.MoveNext())
                    yield break;

                var previous = e.Current;
                while (e.MoveNext())
                {
                    yield return resultSelector(previous, e.Current);
                    previous = e.Current;
                }
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

        static bool IsValidPath(string path)
        {
            if ((File.GetAttributes(path) & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint)
            {
                Debug.WriteLine("'" + path + "' is a reparse point (skipped)");
                return false;
            }
            if (!IsReadable(path))
            {
                Debug.WriteLine("'" + path + "' *ACCESS DENIED* (skipped)");
                return false;
            }
            return true;
        }

        static bool IsReadable(string path)
        {
            try
            {
                var dn = Path.GetDirectoryName(path);
                string[] test = Directory.GetDirectories(dn, "*.*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException) { return false; }
            catch (PathTooLongException) { return false; }
            catch (IOException) { return false; }
            return true;
        }

        /// <summary>
        /// Determines the last <see cref="DriveType.Fixed"/> drive letter on a client machine.
        /// </summary>
        /// <returns>drive letter</returns>
        public static string GetLastFixedDrive()
        {
            char lastLetter = 'C';
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady && drive.AvailableFreeSpace > 1000000)
                {
                    if (drive.Name[0] > lastLetter)
                        lastLetter = drive.Name[0];
                }
            }
            return $"{lastLetter}:";
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
        /// <returns></returns>
        public static string ToTimeString(this TimeSpan? span, int significantDigits = 3)
        {
            var format = $"G{significantDigits}";
            return span?.TotalMilliseconds < 1000 ? span?.TotalMilliseconds.ToString(format) + " milliseconds"
                    : (span?.TotalSeconds < 60 ? span?.TotalSeconds.ToString(format) + " seconds"
                    : (span?.TotalMinutes < 60 ? span?.TotalMinutes.ToString(format) + " minutes"
                    : (span?.TotalHours < 24 ? span?.TotalHours.ToString(format) + " hours"
                    : span?.TotalDays.ToString(format) + " days")));
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
            string? line = string.Empty;

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
            string? line = string.Empty;

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
        /// De-dupe file reader using a <see cref="HashSet{T}"/>.
        /// </summary>
        public static HashSet<string> ReadLines(string path)
        {
            if (!File.Exists(path))
                return new();

            return new HashSet<string>(File.ReadAllLines(path), StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Reads all lines in the <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The <see cref="Stream" /> to read from.</param>
        /// <returns>All lines in the stream.</returns>
        public static string[] ReadAllLines(this Stream stream) => ReadAllLines(stream, Encoding.UTF8);

        /// <summary>
        /// Reads all lines in the <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The <see cref="Stream" /> to read from.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>All lines in the stream.</returns>
        public static string[] ReadAllLines(this Stream stream, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                return ReadAllLines(reader).ToArray();
            }
        }

        /// <summary>
        /// Reads all lines in the <see cref="TextReader" />.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader" /> to read from.</param>
        /// <returns>All lines in the stream.</returns>
        public static IEnumerable<string> ReadAllLines(this TextReader reader)
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        /// <summary>
        /// Reads all lines in the <see cref="TextReader" />.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader" /> to read from.</param>
        /// <returns>All lines in the stream.</returns>
        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this TextReader reader)
        {
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                yield return line;
            }
        }

        /// <summary>
        /// De-dupe file writer using a <see cref="HashSet{T}"/>.
        /// </summary>
        public static bool WriteLines(string path, IEnumerable<string> lines)
        {
            var output = new HashSet<string>(lines, StringComparer.InvariantCultureIgnoreCase);
            using (TextWriter writer = File.CreateText(path))
            {
                foreach (var line in output) { writer.WriteLine(line); }
            }

            return true;
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
        /// Returns a random selection from <see cref="Microsoft.UI.Colors"/>.
        /// We are interested in the runtime <see cref="System.Reflection.PropertyInfo"/>
        /// from the <see cref="Microsoft.UI.Colors"/> sealed class. We will only add a
        /// property to our collection if it is of type <see cref="Windows.UI.Color"/>.
        /// </summary>
        /// <returns><see cref="List{T}"/></returns>
        public static List<Windows.UI.Color> GetWinUIColorList()
        {
            List<Windows.UI.Color> colors = new();

            foreach (var color in typeof(Microsoft.UI.Colors).GetRuntimeProperties())
            {
                // We must check the property type before assuming the explicit cast with GetValue.
                if (color != null && color.PropertyType == typeof(Windows.UI.Color))
                {
                    try
                    {
                        var clr = (Windows.UI.Color?)color.GetValue(null);
                        if (clr != null)
                        {
#pragma warning disable CS8629
                            if (clr?.A == 0 || (clr?.R == 0 && clr?.G == 0 && clr?.B == 0))
                                Debug.WriteLine($"Skipping this color (transparent)");
                            else if (clr?.A != 0 && (clr?.R <= 40 && clr?.G <= 40 && clr?.B <= 40))
                                Debug.WriteLine($"Skipping this color (too dark)");
                            else
                                colors.Add((Windows.UI.Color)clr);
#pragma warning restore CS8629
                        }
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine($"Failed to get the value for '{color.Name}'");
                    }
                }
                else
                {
                    Debug.WriteLine($"I was looking for type {nameof(Windows.UI.Color)} but found {color?.PropertyType} instead.");
                }
            }

            return colors;
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
        /// Generates a 7 digit color string (including the pound sign).
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
            float sa = colorFrom.A, sr = colorFrom.R, sg = colorFrom.G, sb = colorFrom.B;

            // Convert colorTo components to lerp-able floats
            float ea = colorTo.A, er = colorTo.R, eg = colorTo.G, eb = colorTo.B;

            // lerp the colors to get the difference
            byte a = (byte)Math.Max(0, Math.Min(255, sa.Lerp(ea, amount))),
                r = (byte)Math.Max(0, Math.Min(255, sr.Lerp(er, amount))),
                g = (byte)Math.Max(0, Math.Min(255, sg.Lerp(eg, amount))),
                b = (byte)Math.Max(0, Math.Min(255, sb.Lerp(eb, amount)));

            // return the new color
            return Windows.UI.Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Checks each <see cref="Windows.UI.Color"/>'s RGB value to determine if they are similar.
        /// </summary>
        /// <remarks>The alpha channel will also be evaluated.</remarks>
        public static bool IsSimilarTo(this Windows.UI.Color first, Windows.UI.Color second, byte margin)
        {
            return Math.Abs(first.A - second.A) <= margin &&
                   Math.Abs(first.R - second.R) <= margin &&
                   Math.Abs(first.G - second.G) <= margin &&
                   Math.Abs(first.B - second.B) <= margin;
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
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
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

        /// <summary>
        /// Get OS version by way of <see cref="Windows.System.Profile.AnalyticsInfo"/>.
        /// </summary>
        /// <returns><see cref="Version"/></returns>
        public static Version GetWindowsVersionUsingAnalyticsInfo()
        {
            try
            {
                ulong version = ulong.Parse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
                var Major = (ushort)((version & 0xFFFF000000000000L) >> 48);
                var Minor = (ushort)((version & 0x0000FFFF00000000L) >> 32);
                var Build = (ushort)((version & 0x00000000FFFF0000L) >> 16);
                var Revision = (ushort)(version & 0x000000000000FFFFL);

                return new Version(Major, Minor, Build, Revision);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetWindowsVersionUsingAnalyticsInfo: {ex.Message}", $"{nameof(Extensions)}");
                return new Version(); // 0.0
            }
        }

        public static string GetRandomSentence(int wordCount)
        {
            string[] table = { "a", "server", "or", "workstation", "PC", "is", "technological", "technology", "power",
            "system", "used", "for", "diagnosing", "and", "analyzing", "data", "for", "reporting", "to", "on",
            "user", "monitor", "display", "interaction", "electric", "batteries", "along", "with", "some", "over",
            "under", "memory", "once", "in", "while", "special", "object", "can be", "found", "inside", "the",
            "HD", "SSD", "USB", "CDROM", "NVMe", "GPU", "RAM", "NIC", "RAID", "SQL", "API", "XML", "JSON", "website",
            "at", "cluster", "fiber-optic", "floppy-disk", "media", "storage", "Windows", "operating", "root",
            "database", "access", "denied", "granted", "file", "files", "folder", "folders", "directory", "path",
            "registry", "policy", "wire", "wires", "serial", "parallel", "bus", "fast", "slow", "speed", "bits",
            "bytes", "voltage", "current", "resistance", "wattage", "circuit", "inspection", "measurement", "diagram",
            "specifications", "robotics", "telecommunication", "applied", "internet", "science", "code", "password",
            "username", "wireless", "digital", "headset", "programming", "framework", "enabled", "disabled", "timer",
            "information", "keyboard", "mouse", "printer", "peripheral", "binary", "hexadecimal", "network", "router",
            "mainframe", "host", "client", "software", "version", "format", "upload", "download", "login", "logout",
            "embedded", "barcode", "driver", "image", "document", "flow", "layout", "uses", "configuration" };

            string word = string.Empty;
            StringBuilder builder = new StringBuilder();
            // Select a random word from the array until word count is satisfied.
            for (int i = 0; i < wordCount; i++)
            {
                string tmp = table[Random.Shared.Next(table.Length)];

                if (wordCount < table.Length)
                    while (word.Equals(tmp) || builder.ToString().Contains(tmp)) { tmp = table[Random.Shared.Next(table.Length)]; }
                else
                    while (word.Equals(tmp)) { tmp = table[Random.Shared.Next(table.Length)]; }

                builder.Append(tmp).Append(' ');
                word = tmp;
            }
            string sentence = builder.ToString().Trim() + ". ";
            // Set the first letter of the first word in the sentenece to uppercase.
            sentence = char.ToUpper(sentence[0]) + sentence.Substring(1);
            return sentence;
        }

        public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var joined = new[] { list1, list2 }.Where(x => x != null).SelectMany(x => x);
            return joined ?? Enumerable.Empty<T>();
        }
        public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3)
        {
            var joined = new[] { list1, list2, list3 }.Where(x => x != null).SelectMany(x => x);
            return joined ?? Enumerable.Empty<T>();
        }
        public static IEnumerable<T> JoinMany<T>(params IEnumerable<T>[] array)
        {
            var final = array.Where(x => x != null).SelectMany(x => x);
            return final ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Splits a <see cref="Dictionary{TKey, TValue}"/> into two equal halves.
        /// </summary>
        /// <param name="dictionary"><see cref="Dictionary{TKey, TValue}"/></param>
        /// <returns>tuple</returns>
        public static (Dictionary<string, string> firstHalf, Dictionary<string, string> secondHalf) SplitDictionary(this Dictionary<string, string> dictionary)
        {
            int count = dictionary.Count;

            if (count <= 1)
            {
                // Return the entire dictionary as the first half and an empty dictionary as the second half.
                return (dictionary, new Dictionary<string, string>());
            }

            int halfCount = count / 2;
            var firstHalf = dictionary.Take(halfCount).ToDictionary(kv => kv.Key, kv => kv.Value);
            var secondHalf = dictionary.Skip(halfCount).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Adjust the second half if the count is odd.
            if (count % 2 != 0)
                secondHalf = dictionary.Skip(halfCount + 1).ToDictionary(kv => kv.Key, kv => kv.Value);

            return (firstHalf, secondHalf);
        }

#pragma warning disable 8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        /// <summary>
        /// Helper for <see cref="System.Collections.Generic.SortedList{TKey, TValue}"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sortedList"></param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
        public static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(this System.Collections.Generic.SortedList<TKey, TValue> sortedList)
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> pair in sortedList)
            {
                dictionary.Add(pair.Key, pair.Value);
            }
            return dictionary;
        }

        /// <summary>
        /// Helper for <see cref="System.Collections.SortedList"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sortedList"></param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
        public static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(this System.Collections.SortedList sortedList)
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
            foreach (DictionaryEntry pair in sortedList)
            {
                dictionary.Add((TKey)pair.Key, (TValue)pair.Value);
            }
            return dictionary;
        }

        /// <summary>
        /// Helper for <see cref="System.Collections.Hashtable"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="hashList"></param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
        public static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(this System.Collections.Hashtable hashList)
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
            foreach (DictionaryEntry pair in hashList)
            {
                dictionary.Add((TKey)pair.Key, (TValue)pair.Value);
            }
            return dictionary;
        }
#pragma warning restore 8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                try { action(i); }
                catch (Exception ex) { Debug.WriteLine($"{ex.GetType()}: {ex.Message}"); }
            }
        }

        public static T Retry<T>(this Func<T> operation, int attempts)
        {
            while (true)
            {
                try
                {
                    attempts--;
                    return operation();
                }
                catch (Exception ex) when (attempts > 0)
                {
                    Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}", $"{nameof(Extensions)}");
                    Thread.Sleep(2000);
                }
            }
        }

        public static Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            return Implementation(items);

            static async Task<List<T>> Implementation(IAsyncEnumerable<T> items)
            {
                var rv = new List<T>();
                await foreach (var item in items)
                {
                    rv.Add(item);
                }
                return rv;
            }
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? toAdd)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (toAdd != null)
            {
                foreach (var item in toAdd)
                    collection.Add(item);
            }
        }

        public static void RemoveFirst<T>(this IList<T> collection, Func<T, bool> predicate)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    collection.RemoveAt(i);
                    break;
                }
            }
        }

        public static NameValueCollection? ExtractJsonObjectFromQueryString(string uriQuery, string root = "auth", string key = "tokenId")
        {
            NameValueCollection? vals = null;

            try
            {
                vals = System.Web.HttpUtility.ParseQueryString(uriQuery);
            }
            catch (Exception ex) { Debug.WriteLine($"ExtractJsonObjectFromQueryString: {ex.Message}"); }

            try
            {
                if (vals != null && vals[root] is string node)
                {
                    var jsonObject = System.Text.Json.Nodes.JsonObject.Parse(node) as System.Text.Json.Nodes.JsonObject;
                    if (jsonObject is not null)
                    {
                        NameValueCollection vals2 = new NameValueCollection(jsonObject.Count);

                        if (jsonObject.ContainsKey(key) && jsonObject[key] is System.Text.Json.Nodes.JsonValue jvalue && jvalue.TryGetValue<string>(out string? value))
                            vals2.Add(key, value);

                        return vals2;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"ExtractJsonObjectFromQueryString: {ex.Message}"); }

            return null;

            // If you wanted to return or create a JsonObject...
            var jObj = new System.Text.Json.Nodes.JsonObject
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
                { "....", "......" },
            };
        }

        /// <summary>
        /// Similar to <see cref="Windows.System.Launcher.LaunchUriAsync(Uri)"/>
        /// </summary>
        /// <param name="uriFilePath"></param>
        public static void RunFileUsingProtocolHandler(Uri uriFilePath)
        {
            if (uriFilePath is null)
                return;

            RunFileUsingProtocolHandler($"{uriFilePath}");
        }

        /// <summary>
        /// Similar to <see cref="Windows.System.Launcher.LaunchUriAsync(Uri)"/>
        /// </summary>
        /// <param name="uriFilePath"></param>
        public static async Task RunFileUsingProtocolHandlerAsync(Uri uriFilePath)
        {
            if (uriFilePath is null)
                return;

            await RunFileUsingProtocolHandlerAsync($"{uriFilePath}");
        }

        /// <summary>
        /// Similar to <see cref="Windows.System.Launcher.LaunchUriAsync(Uri)"/>
        /// https://learn.microsoft.com/en-us/windows/win32/search/-search-3x-wds-extidx-prot-implementing
        /// </summary>
        /// <param name="uriFilePath"></param>
        public static void RunFileUsingProtocolHandler(string uriFilePath)
        {
            if (string.IsNullOrEmpty(uriFilePath))
                return;

            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "rundll32.exe";
                // Passing is limited to 2083 characters (including "https://")
                process.StartInfo.Arguments = $"url.dll,FileProtocolHandler {uriFilePath}";
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
            catch (Exception ex) { Debug.WriteLine($"RunFileUsingProtocolHandler: {ex.Message}"); }
        }

        /// <summary>
        /// Similar to <see cref="Windows.System.Launcher.LaunchUriAsync(Uri)"/>
        /// https://learn.microsoft.com/en-us/windows/win32/search/-search-3x-wds-extidx-prot-implementing
        /// </summary>
        /// <param name="uriFilePath"></param>
        public static async Task RunFileUsingProtocolHandlerAsync(string uriFilePath)
        {
            if (string.IsNullOrEmpty(uriFilePath))
                return;

            await Task.Run(() =>
            {
                try
                {
                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "rundll32.exe";
                    // Passing is limited to 2083 characters (including "https://")
                    process.StartInfo.Arguments = $"url.dll,FileProtocolHandler {uriFilePath}";
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
                }
                catch (Exception ex) { Debug.WriteLine($"RunFileUsingProtocolHandler: {ex.Message}"); }
            });
        }
    }
}
