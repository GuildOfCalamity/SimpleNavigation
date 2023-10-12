using System;
using System.Runtime.InteropServices;

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

    [DllImport("user32.dll")]
    public static extern int FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);
    #endregion
}
