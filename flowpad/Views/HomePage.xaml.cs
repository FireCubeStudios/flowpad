using flowpad.Helpers;
using flowpad.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Shell;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace flowpad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public class Images
        {
            public ImageSource ImageURL { get; set; }
            public string ImageText { get; set; }
            public string ImagePath { get; internal set; }
            public StorageFile File { get; set; }
        }
        private ElementTheme _elementTheme = ThemeSelectorService.Theme;
        List<Images> ImageCollection;

        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { Set(ref _elementTheme, value); }
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { Set(ref _versionDescription, value); }
        }

        public HomePage()
        {
            this.InitializeComponent();
            StartUp();
        }
        public async void StartUp()
        {
            try
            {
                ImageCollection = new List<Images>();
                // pick a folder
                var folder = await KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings");
                var filesList = await folder.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.DefaultQuery, new string[] { ".gif" })).GetFilesAsync();
                for (int i = 0; i < filesList.Count; i++)
                {
                    StorageFile imagefile = filesList[i];
                    BitmapImage bitmapimage = new BitmapImage();
                    using (IRandomAccessStream stream = await imagefile.OpenAsync(FileAccessMode.Read))
                    {
                        bitmapimage.SetSource(stream);
                    }

                    ImageCollection.Add(new Images()
                    {
                        ImageURL = bitmapimage,
                        ImageText = filesList[i].Name,
                        ImagePath = filesList[i].Path,
                        File = imagefile

                    });
                }
                ImageGridView.ItemsSource = ImageCollection;
            }
            catch
            {
                var AppFolder = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFolderAsync("InkDrawings");
                ImageCollection = new List<Images>();
                // pick a folder
                var folder = await KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings");
                var filesList = await folder.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.DefaultQuery, new string[] { ".gif" })).GetFilesAsync();
                for (int i = 0; i < filesList.Count; i++)
                {

                    StorageFile imagefile = filesList[i];
                    BitmapImage bitmapimage = new BitmapImage();
                    using (IRandomAccessStream stream = await imagefile.OpenAsync(FileAccessMode.Read))
                    {
                        bitmapimage.SetSource(stream);
                    }

                    ImageCollection.Add(new Images()
                    {
                        ImageURL = bitmapimage,
                        ImageText = filesList[i].Name,
                        ImagePath = filesList[i].Path,
                                           File = imagefile
                    });
                }
                ImageGridView.ItemsSource = ImageCollection;
            }
        }

        private void ClassicNavButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(InkPage));
        }
        private void WhiteboardNavButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(WhiteboardPage));
        }

        private async void OpenFileAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var frameworkelement = sender as FrameworkElement;
            var data = Frame.DataContext;
            Images Img = data as Images;
          await Launcher.LaunchUriAsync(new Uri(Img.ImagePath));
        }
        private async void Whatsnew_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WhatsNewDialog();
            await dialog.ShowAsync();
        }
        private async void Welcome_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FirstRunDialog();
            await dialog.ShowAsync();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            SettingsSplitView.IsPaneOpen = true;
            Initialize();
        }

        private void Initialize()
        {
            VersionDescription = GetVersionDescription();

            if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
            {
                this.Feedbackbutton.IsEnabled = true;
            }
        }

        private string GetVersionDescription()
        {
            var appName = "AppDisplayName".GetLocalized();
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

 


        /* private void Setttingset<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
         {
             if (Equals(storage, value))
             {
                 return;
             }

             storage = value;
             OnPropertyChanged(propertyName);
         }*/
        private async void FeedbackLink_Click(object sender, RoutedEventArgs e)
        {
            //This launcher is part of the Store Services SDK https://docs.microsoft.com/en-us/windows/uwp/monetize/microsoft-store-services-sdk
            await Launcher.LaunchUriAsync(new Uri("https://discord.gg/3WYcKat"));
        }
        private async void Rate_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(
    new Uri($"ms-windows-store://review/?PFN={Package.Current.Id.FamilyName}"));
        }
        private async void PinAppToTaskbar_Click(object sender, RoutedEventArgs e)
        {
            bool isPinningAllowed = TaskbarManager.GetDefault().IsPinningAllowed;
            if (isPinningAllowed)
            {
                if (ApiInformation.IsTypePresent("Windows.UI.Shell.TaskbarManager"))
                {
                    bool isPinned = await TaskbarManager.GetDefault().IsCurrentAppPinnedAsync();

                    if (isPinned)
                    {
                        await new MessageDialog("If not you can manually pin the app to the taskbar", "You already have the app pinned in your taskbar").ShowAsync();
                    }
                    else
                    {
                        bool IsPinned = await TaskbarManager.GetDefault().RequestPinCurrentAppAsync();
                    }
                }

                else
                {
                    await new MessageDialog("Update your device to the Fall creators update or higher to pin this app", "Update your device").ShowAsync();
                }
            }



            else
            {
                var t = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
                switch (t)
                {
                    case "Windows.Desktop":
                        await new MessageDialog("It seems you are using a computer. Group policy disabled pinning of app in taskbar", "Taskbar pin failed").ShowAsync();
                        break;
                    case "Windows.Mobile":
                        await new MessageDialog("It seems you are using a Windows 10 on ARM device or mobile device. Group policy disabled pinning of the app", "Taskbar pin failed").ShowAsync();
                        break;
                    case "Windows.IoT":
                        await new MessageDialog("It seems you are using a IoT device which doesn't support taskbar pin API", "Taskbar pin failed").ShowAsync();
                        break;
                    case "Windows.Team":
                        break;
                    case "Windows.Holographic":
                        await new MessageDialog("It seems you are using hololens. Hololens doesn't have a taskbar", "Taskbar pin failed").ShowAsync();
                        break;
                    case "Windows.Xbox":
                        await new MessageDialog("It seems you are using a xbox. Xbox doesn't have a taskbar", "Taskbar pin failed").ShowAsync();
                        break;
                    default:
                        await new MessageDialog("It seems you are using a " + t + " device. This device does not support taskbar API or Group policy disabled pinning of the app", "Taskbar pin failed").ShowAsync();
                        break;
                }
            }
        }
        private async void ThemeChanged_CheckedAsync(object sender, RoutedEventArgs e)
        {
            var param = (sender as RadioButton)?.CommandParameter;

            if (param != null) await ThemeSelectorService.SetThemeAsync((ElementTheme)param);
        }
        private async void LivePin(object sender, RoutedEventArgs e)
        {
            // Get your own app list entry
            // (which is always the first app list entry assuming you are not a multi-app package)
            AppListEntry entry = (await Package.Current.GetAppListEntriesAsync())[0];

            // Check if Start supports your app
            bool isSupported = StartScreenManager.GetDefault().SupportsAppListEntry(entry);
            if (isSupported)
            {
                if (ApiInformation.IsTypePresent("Windows.UI.StartScreen.StartScreenManager"))
                {
                    // Primary tile API's supported!
                    bool isPinned = await StartScreenManager.GetDefault().ContainsAppListEntryAsync(entry);
                    if (isPinned)
                    {
                        await new MessageDialog("If not you can manually put the live tile on to the StartScreen", "You already have the live tile in your StartScreen").ShowAsync();
                    }
                    else
                    {
                        bool IsPinned = await StartScreenManager.GetDefault().RequestAddAppListEntryAsync(entry);
                    }
                }
                else
                {
                    await new MessageDialog("You need to update your device to enable automatic pinning", "Update your device").ShowAsync();
                }
            }
            else
            {
                var t = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
                switch (t)
                {
                    case "Windows.IoT":
                        await new MessageDialog("It seems you are using a IoT device which doesn't support Primary tile API", "live tile failed").ShowAsync();
                        break;
                    case "Windows.Team":
                        break;
                    case "Windows.Holographic":
                        await new MessageDialog("It seems you are using hololens. Hololens doesn't support live tile", "live tile failed").ShowAsync();
                        break;
                    case "Windows.Xbox":
                        await new MessageDialog("It seems you are using a xbox. Xbox doesn't support live tile", "live tile failed").ShowAsync();
                        break;
                    default:
                        await new MessageDialog("It seems you are using a " + t + " device. This device does not support Primary tile API", "live tile failed").ShowAsync();
                        break;
                }
            }
        }
    }
}
