using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Globalization;
using Microsoft.UI.Xaml;

namespace SimpleNavigation;

public static class Win32API
{
    #region [DLL Imports]
    public const int WM_SYSCOMMAND = 0x0112;
    public const int SC_CLOSE = 0xF060;
    public const int SC_SIZE = 0xF000;
    public const int SC_MOVE = 0xF010;
    public const int SC_MINIMIZE = 0xF020;
    public const int SC_MAXIMIZE = 0xF030;
    public const int SC_RESTORE = 0xF120;
    public const int GWLP_WNDPROC = -4;
    public const int GWL_EXSTYLE = -20;
    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    // Window styles
    public const int WS_EX_DLGMODALFRAME = 0x00000001;   // The window has a double border.
    public const int WS_EX_NOPARENTNOTIFY = 0x00000004;  // The child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
    public const int WS_EX_TOOLWINDOW = 0x00000080;      // The window is intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB.
    public const int WS_EX_WINDOWEDGE = 0x00000100;      // The window has a border with a raised edge.
    public const int WS_EX_ACCEPTFILES = 0x00000010;     // The window accepts drag-drop files.
    public const int WS_EX_CLIENTEDGE = 0x00000200;      // The window has a border with a sunken edge.
    public const int WS_EX_CONTEXTHELP = 0x00000400;     // The title bar of the window includes a question mark.
    public const int WS_EX_APPWINDOW = 0x00040000;       // Forces a top-level window onto the taskbar when the window is visible.
    public const int WS_EX_COMPOSITED = 0x02000000;      // Paints all descendants of a window in bottom-to-top painting order using double-buffering.
    public const int WS_EX_CONTROLPARENT = 0x00010000;   // The window itself contains child windows that should take part in dialog box navigation.
    public const int WS_EX_LAYERED = 0x00080000;         // The window is a layered window.
    public const int WS_EX_LAYOUTRTL = 0x00400000;       // If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the horizontal origin of the window is on the right edge.
    public const int WS_EX_STATICEDGE = 0x00020000;      // The window has a three-dimensional border style intended to be used for items that do not accept user input.
    public const int WS_EX_NOINHERITLAYOUT = 0x00100000; // The window does not pass its window layout to its child windows.
    public const int WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE); // The window is an overlapped window.


    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern int FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] // 64-bit
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]    // 32-bit
    public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("Shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

    [DllImport("User32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("kernel32.dll")]
    internal static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_INFO // only used for GetNativeSystemInfo calls
    {
        internal ushort wProcessorArchitecture;
        internal ushort wReserved;
        internal uint dwPageSize;
        internal IntPtr lpMinimumApplicationAddress;
        internal IntPtr lpMaximumApplicationAddress;
        internal IntPtr dwActiveProcessorMask;
        internal uint dwNumberOfProcessors;
        internal uint dwProcessorType;
        internal uint dwAllocationGranularity;
        internal ushort wProcessorLevel;
        internal ushort wProcessorRevision;
    }
    #endregion
}
