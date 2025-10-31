using System.Runtime.InteropServices;
using UnityEngine;

namespace BasicAppUtility
{
    public static class BasicAppUtilities
    {
#region DLLstuff
    const int SWP_HIDEWINDOW = 0x80; //hide window flag.
    const int SWP_SHOWWINDOW = 0x40; //show window flag.
    const int SWP_NOMOVE = 0x0002; //don't move the window flag.
    const int SWP_NOSIZE = 0x0001; //don't resize the window flag.
    const uint WS_SIZEBOX = 0x00040000;
    const int GWL_STYLE = -16;
    const int WS_BORDER = 0x00800000; //window with border
    const int WS_DLGFRAME = 0x00400000; //window with double border but no title
    const int WS_CAPTION = WS_BORDER | WS_DLGFRAME; //window with a title bar
    const int WS_SYSMENU = 0x00080000;      //window with no borders etc.
    const int WS_MAXIMIZEBOX = 0x00010000;
    const int WS_MINIMIZEBOX = 0x00020000;  //window with minimizebox

    [DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();
 
    [DllImport("user32.dll")]
    static extern int FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(
        System.IntPtr hWnd, // window handle
        System.IntPtr hWndInsertAfter, // placement order of the window
        short X, // x position
        short Y, // y position
        short cx, // width
        short cy, // height
        uint uFlags // window flags.
    );

    [DllImport("user32.dll")]
    static extern System.IntPtr SetWindowLong(
         System.IntPtr hWnd, // window handle
         int nIndex,
         uint dwNewLong
    );

    [DllImport("user32.dll")]
    static extern System.IntPtr GetWindowLong(
        System.IntPtr hWnd,
        int nIndex
    );

    static System.IntPtr hWnd;
    static System.IntPtr HWND_TOP = new System.IntPtr(0);
    static System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
    static System.IntPtr HWND_NOTOPMOST = new System.IntPtr(-2);

    #endregion

        public static void ExitApplication()
        {
            #if UNITY_EDITOR
            // If in the Unity editor, stop playing.
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            // For a built application, quit the executable.
            Application.Quit();
            #endif
        }

        public static void SetWindowFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }
    }
}