using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace SimpleNavigation
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            this.Title = AssemblyHelper.GetAssemblyName();

            #region [SystemBackdrop was added starting with WinAppSDK 1.3.230502+]
            if (Extensions.IsWindows11OrGreater())
                SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            else
                SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
            #endregion

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                Debug.WriteLine($"⇩⇩⇩ [Referenced Assemblies] ⇩⇩⇩");
                Debug.WriteLine($"{Extensions.GatherReferenceAssemblies(true)}");
                Debug.WriteLine($"⇧⇧⇧ [Referenced Assemblies] ⇧⇧⇧");
            });
        }

        void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            Debug.WriteLine($"{nameof(MainWindow)} activated at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        }

        void Window_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
        {
            Debug.WriteLine($"{nameof(MainWindow)} visibility changed at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        }

        void Window_Closed(object sender, WindowEventArgs args)
        {
            Debug.WriteLine($"{nameof(MainWindow)} closed at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        }
    }
}
