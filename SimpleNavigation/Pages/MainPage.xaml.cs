using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace SimpleNavigation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _imgPath = "/Assets/gear1.png";
        public string ImgPath
        {
            get => _imgPath;
            set
            {
                _imgPath = value;
                OnPropertyChanged();
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.AddHandler(KeyDownEvent, new KeyEventHandler(PressedKey), true);

            // We starting off by selecting the "Home" page.
            // We could let it ride on the MainPage, but we're using the
            // MainPage as a root/hub for any subsequent navigation events.
            SetStartPage(rbHome);

            if (App.AnimationsEffectsEnabled)
                StoryboardPath.Begin();
        }

        #region [Simple Page Navigation]
        void SetStartPage(RadioButton? rb)
        {
            if (rb == null)
            {
                SetCurrentPage(typeof(HomePage));
                return;
            }

			rb.IsChecked = true;
			RadioButton_Click(rb, new RoutedEventArgs());
		}

		void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainFrame.Content != null && MainFrame.Content is Page pg)
                   Debug.WriteLine($"BaseUri is '{pg.BaseUri}'");

                pageTitle.Text = $"{((RadioButton)sender).Content}".Replace("Page","");
                
                // This is unnecessary, but I'm adding this as a way to show how one
                // could pass more complex data objects between pages during navigation.
                App.State.LastUpdate = DateTime.Now;
                App.State.PageType = Type.GetType($"{((RadioButton)sender).Tag}");
                App.State.Title = pageTitle.Text;
                App.State.CurrentTheme = MainFrame.ActualTheme;

                // Navigate to the page with optional params.
                MainFrame.Navigate(App.State.PageType, App.State);
                
                // If you don't want to pass an object between pages, you could call the basic Navigate.
                //MainFrame.Navigate(Type.GetType($"{((RadioButton)sender).Tag}"));
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Optional helper method
        /// </summary>
        /// <param name="type">the page type to navigate to</param>
        public void SetCurrentPage(Type type)
        {
            try
            {
                MainFrame.Navigate(type);
            }
			catch (Exception ex)
			{
				Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
			}
		}

        void MainFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Debug.WriteLine($"Failed to load page '{e.SourcePageType.FullName}'");
            App.ShowMessageBox("Navigation", $"Failed to load page '{e.SourcePageType.FullName}'", "OK", "Cancel", null, null);
        }

        void MainFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            Debug.WriteLine($"Navigating to page '{e.SourcePageType.FullName}'");
        }

        void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Debug.WriteLine($"Navigated to page '{e.SourcePageType.FullName}'");
        }

        void MainFrame_NavigationStopped(object sender, NavigationEventArgs e)
        {
            Debug.WriteLine($"Navigation stopped for page '{e.SourcePageType.FullName}'");
        }

		#endregion

		#region [Control Events]
		void PressedKey(object sender, KeyRoutedEventArgs e) => Debug.WriteLine($">> [{e.Key}] key was pressed <<");

        async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null && sender is HyperlinkButton hlb) 
            {
                await Extensions.LocateAndLaunchUrlFromString($"{hlb.Tag}");
            }
        }

        void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null && sender is RadioButton rb)
            {
                Debug.WriteLine($"User selected '{rb.Tag}'");
                ImgPath = $"/Assets/{rb.Tag}";
            }
        }
		#endregion
	}
}
