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
using WinRT;

namespace SimpleNavigation
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        #region [Thick/Thin Acrylic]
        bool useThinAcrylic = false;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController? m_acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration? m_configurationSource;
        #endregion

        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            this.Title = AssemblyHelper.GetAssemblyName();
            ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

            #region [SystemBackdrop was added starting with WinAppSDK 1.3.230502+]
            if (Extensions.IsWindows11OrGreater())
                SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            else
            {
                // Default acrylic
                SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                #region [Thick/Thin Acrylic]
                // Hooking up the policy object.
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                m_acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();
                m_acrylicController.Kind = useThinAcrylic ? Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicKind.Thin : Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicKind.Base;
                // Enable the system backdrop
                m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                #endregion
            }
            #endregion
        }

        void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            Debug.WriteLine($"{nameof(MainWindow)} activated at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
            if (m_configurationSource != null)
                m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        void Window_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
        {
            Debug.WriteLine($"{nameof(MainWindow)} visibility changed at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        }

        void Window_Closed(object sender, WindowEventArgs args)
        {
            Debug.WriteLine($"{nameof(MainWindow)} closed at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
        }

        void Window_ThemeChanged(FrameworkElement sender, object args) => SetConfigurationSourceTheme();

        void SetConfigurationSourceTheme()
        {
            if (m_configurationSource == null)
                return;

            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark:
                    m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark;
                    break;
                case ElementTheme.Light:
                    m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light;
                    break;
                case ElementTheme.Default:
                    m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default;
                    break;
            }
        }
    }
}
