using flowpad.Helpers;
using flowpad.Services;
using flowpad.Services.Ink;
using flowpad.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Shell;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Flowpad.Services.Ink.UndoRedo;
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace flowpad.UserControls
{
    public sealed partial class InkCanvasUserControl : UserControl
    {
        private bool lassoSelectionButtonIsChecked;
        /* private bool touchInkingButtonIsChecked = true;
         private bool mouseInkingButtonIsChecked = true;
         private bool penInkingButtonIsChecked = true;*/
        private bool transformTextAndShapesButtonIsEnabled;
        private bool undoButtonIsEnabled;
        /*private bool redoButtonIsEnabled;
        private bool saveInkFileButtonIsEnabled;
        private bool clearAllButtonIsEnabled;*/
        private InkStrokesService strokeService;
        private InkLassoSelectionService lassoSelectionService;
        private InkNodeSelectionService nodeSelectionService;
        private InkPointerDeviceService pointerDeviceService;
        private InkUndoRedoService undoRedoService;
        private InkTransformService transformService;
        private InkFileService fileService;
        private InkZoomService zoomService;
        public string FileOpen;
        private ElementTheme _elementTheme = ThemeSelectorService.Theme;
        public bool LassoSelectionButtonIsChecked
        {
            get => lassoSelectionButtonIsChecked;
            set
            {
                Set(ref lassoSelectionButtonIsChecked, value);
                //ConfigLassoSelection(value);
            }
        }

        public bool TransformTextAndShapesButtonIsEnabled
        {
            get => transformTextAndShapesButtonIsEnabled;
            set => Set(ref transformTextAndShapesButtonIsEnabled, value);
        }




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

        public InkCanvasUserControl()
        {
            this.InitializeComponent();
            Loaded += (sender, eventArgs) =>
            {


                FindName("NavigationRibbon");
                FindName("canvasScroll");
                SetCanvasSize();
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                Window.Current.SetTitleBar(TitleGrid);
                NavigationRibbon.SelectedIndex = 1;
                strokeService = new InkStrokesService(inkCanvas.InkPresenter);
                var analyzer = new InkAsyncAnalyzer(inkCanvas, strokeService);
                var selectionRectangleService = new InkSelectionRectangleService(inkCanvas, selectionCanvas, strokeService);

                lassoSelectionService = new InkLassoSelectionService(inkCanvas, selectionCanvas, strokeService, selectionRectangleService);
                nodeSelectionService = new InkNodeSelectionService(inkCanvas, selectionCanvas, analyzer, strokeService, selectionRectangleService);
                pointerDeviceService = new InkPointerDeviceService(inkCanvas);
                undoRedoService = new InkUndoRedoService(inkCanvas, strokeService);
                transformService = new InkTransformService(drawingCanvas, strokeService);
                fileService = new InkFileService(inkCanvas, strokeService);
                zoomService = new InkZoomService(canvasScroll);

                strokeService.ClearStrokesEvent += (s, e) => RefreshEnabledButtons();
                undoRedoService.UndoEvent += (s, e) => RefreshEnabledButtons();
                undoRedoService.RedoEvent += (s, e) => RefreshEnabledButtons();
                undoRedoService.AddUndoOperationEvent += (s, e) => RefreshEnabledButtons();
                //pointerDeviceService.DetectPenEvent += (s, e) => TouchInkingButtonIsChecked = false;

            };
            if (ImageGridView != null)

            {



                ImageGridView.ItemClick += AdaptiveGridViewControl_ItemClick;



            }
        }



        private void ZoomIn_Click(object sender, RoutedEventArgs e) => zoomService?.ZoomIn();

        private void ZoomOut_Click(object sender, RoutedEventArgs e) => zoomService?.ZoomOut();

        private void ResetZoom_Click(object sender, RoutedEventArgs e) => zoomService?.ResetZoom();

        private void FitToScreen_Click(object sender, RoutedEventArgs e) => zoomService?.FitToScreen();
        private void OnInkToolbarLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is InkToolbar inkToolbar)
            {
                inkToolbar.TargetInkCanvas = inkCanvas;
                InkToolbarToolButton penButton = inkToolbar.GetToolButton(InkToolbarTool.BallpointPen);
                inkToolbar.ActiveTool = penButton;
            }
        }

        private void SetCanvasSize()
        {
            inkCanvas.Width = Math.Max(canvasScroll.ViewportWidth, 1000);
            inkCanvas.Height = Math.Max(canvasScroll.ViewportHeight, 1000);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            undoRedoService?.Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            // if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Any() == true)
            // {
            ClearSelection();
            undoRedoService?.Redo();
            //}

        }

        private async void LoadInkFile_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            var fileLoaded = await fileService.LoadInkAsync();

            if (fileLoaded)
            {
                transformService.ClearTextAndShapes();
                undoRedoService.Reset();
            }
        }

        private async void SaveInkFile_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            await fileService.SaveInkAsync();
        }

        private async void TransformTextAndShapes_Click(object sender, RoutedEventArgs e)
        {
            var result = await transformService.TransformTextAndShapesAsync();
            if (result.TextAndShapes.Any())
            {
                ClearSelection();
                undoRedoService.AddOperation(new TransformUndoRedoOperation(result, strokeService));
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {

            var strokes = strokeService?.GetStrokes().ToList();
            var textAndShapes = transformService?.GetTextAndShapes().ToList();
            ClearSelection();
            strokeService.ClearStrokes();
            transformService.ClearTextAndShapes();
            undoRedoService.AddOperation(new ClearStrokesAndShapesUndoRedoOperation(strokes, textAndShapes, strokeService, transformService));
        }

        private void RefreshEnabledButtons()
        {

            if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Any() == true)
            {

                undoButtonIsEnabled = true;
                //TransformTextAndShapesButtonIsEnabled = strokeService.GetStrokes().Any();

                //strokeService.GetStrokes().Any() || transformService.HasTextAndShapes();
            }

        }

        private void Colorpick_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }
        /* private void ConfigLassoSelection(object sender, RoutedEventArgs e)
         {
             if (1 == 1)
             {
                 lassoSelectionService?.StartLassoSelectionConfig();
             }
             else
             {
                 lassoSelectionService?.EndLassoSelectionConfig();
             }
         }*/

        private void ClearSelection()
        {
            nodeSelectionService.ClearSelection();
            lassoSelectionService.ClearSelection();
        }


        private void SetNavigationViewHeaderContext()
        {
            var headerContextBinding = new Binding
            {
                Source = this,
                Mode = BindingMode.OneWay,
            };

            //SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, headerContextBinding);
        }


        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            FindName("SettingsPanel");
            Initialize();
        }
        public void MyFancyPanel_BackdropTapped(object sender, EventArgs e)

        {

            UnloadObject(SettingsPanel);

        }
        public void MyFancyPanel_BackdropClicked(object sender, RoutedEventArgs e)

        {

            UnloadObject(SettingsPanel);

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

        private async void ThemeChanged_CheckedAsync(object sender, RoutedEventArgs e)
        {
            var param = (sender as RadioButton)?.CommandParameter;

            if (param != null)
            {
                await ThemeSelectorService.SetThemeAsync((ElementTheme)param);

            }
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
            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
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
        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async void Whatsnew_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WhatsNewDialog();
            await dialog.ShowAsync();
        }

        private void InkToolbarCustomToggleButtonPen_Click(object sender, RoutedEventArgs e)
        {
            if (InkSurfacePen.IsChecked == true)
            {
                inkCanvas.InkPresenter.InputDeviceTypes |= CoreInputDeviceTypes.Pen;
            }
            else
            {
                inkCanvas.InkPresenter.InputDeviceTypes &= ~CoreInputDeviceTypes.Pen;
            }
        }
        private void InkToolbarCustomToggleButtonTouch_Click(object sender, RoutedEventArgs e)
        {
            if (TouchInkingButton.IsChecked == true)
            {
                inkCanvas.InkPresenter.InputDeviceTypes |= CoreInputDeviceTypes.Touch;
            }
            else
            {
                inkCanvas.InkPresenter.InputDeviceTypes &= ~CoreInputDeviceTypes.Touch;
            }
        }
        private void InkToolbarCustomToggleButtonMouse_Click(object sender, RoutedEventArgs e)
        {
            if (MouseInkingButton.IsChecked == true)
            {
                inkCanvas.InkPresenter.InputDeviceTypes |= CoreInputDeviceTypes.Mouse;
            }
            else
            {
                inkCanvas.InkPresenter.InputDeviceTypes &= ~CoreInputDeviceTypes.Mouse;
            }
        }

        private async void SaveInkFileInApp_Click(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count == 0)
            {
                return;
            }
            await SaveFileDialogPrompt.ShowAsync();
        }

        private async void SaveFileDialogPrompt_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {


            try
            {
                if (await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings") == null)
                {
                    //Create folder
                    var appFolder = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFolderAsync("InkDrawings");
                }

                else
                {
                    var appFolder = await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings");
                    // var picker = new FolderPicker();
                    //  var pfolder = await picker.PickSingleFolderAsync();
                    StorageApplicationPermissions.FutureAccessList.Add(appFolder);

                    Windows.Storage.StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(appFolder.Path.ToString());
                    Windows.Storage.StorageFile file = await folder.CreateFileAsync(FileNameBox.Text + ".gif", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                    Windows.Storage.CachedFileManager.DeferUpdates(file);
                    IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                    using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                        await outputStream.FlushAsync();
                    }
                    stream.Dispose();

                    Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                    if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                    {
                        await SaveFileDialogSaved.ShowAsync(); // the notification will appear for 2 seconds 
                    }
                    else
                    {
                        await SaveFileDialogFailed.ShowAsync();
                    }
                    /* var file = await folder.CreateFileAsync(FileNameBox.Text);
                     using (var writer = await file.OpenStreamForWriteAsync())
                     {
                         await writer.WriteAsync(new byte[100], 0, 0);
                     }*/
                }
            }
            catch
            {
                var AppFolder = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFolderAsync("InkDrawings");
                var appFolder = await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings");
                // var picker = new FolderPicker();
                //  var pfolder = await picker.PickSingleFolderAsync();
                StorageApplicationPermissions.FutureAccessList.Add(appFolder);

                Windows.Storage.StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(appFolder.Path.ToString());
                Windows.Storage.StorageFile file = await folder.CreateFileAsync(FileNameBox.Text + ".gif", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                    await outputStream.FlushAsync();
                }
                stream.Dispose();

                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    await SaveFileDialogSaved.ShowAsync(); // the notification will appear for 2 seconds 
                }
                else
                {
                    await SaveFileDialogFailed.ShowAsync();
                }
                /* var file = await folder.CreateFileAsync(FileNameBox.Text);
                 using (var writer = await file.OpenStreamForWriteAsync())
                 {
                     await writer.WriteAsync(new byte[100], 0, 0);
                 }*/
            }

        }

        private async void SaveFileDialogPrompt_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ClearSelection();
            await fileService.SaveInkAsync();
        }

        private void MyColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();

            // Assign the selected color to a variable to use outside the popup.
            Windows.UI.Color mycolor = myColorPicker.Color;
            drawingAttributes.Color = mycolor;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

        }


        private async void Openlocation_ClickAsync(ContentDialog sender, ContentDialogButtonClickEventArgs e)
        {
            var appFolder = await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings");
            await Launcher.LaunchFolderAsync(appFolder);
        }

        List<Images> ImageCollection;
        private async void FilesDialog_Click(object sender, RoutedEventArgs e)
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
                        ImagePath = filesList[i].Path

                    });
                }
                ImageGridView.ItemsSource = ImageCollection;
                await OpenFileDialogPrompt.ShowAsync();
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
                        ImagePath = filesList[i].Path

                    });
                }
                ImageGridView.ItemsSource = ImageCollection;
                await OpenFileDialogPrompt.ShowAsync();
            }
        }
        private async void OpenFileDialogPrompt_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Windows.Storage.StorageFile file = await StorageFile.GetFileFromPathAsync(FileOpen);
            
                // Open a file stream for reading.
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                // Read from file.
             
                    using (var inputStream = stream.GetInputStreamAt(0))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                    }
                
        }

        private void OpenFileDialogPrompt_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {


        }
        private void AdaptiveGridViewControl_ItemClick(object sender, Windows.UI.Xaml.Controls.ItemClickEventArgs e)

        {

            if (e.ClickedItem != null)

            {
                OpenFileDialogPrompt.IsPrimaryButtonEnabled = true;
                OpenFileDialogPrompt.IsSecondaryButtonEnabled = true;
               var V = e.ClickedItem as Images;
                FileOpen = V.ImagePath;
            }

        }
        public class Images
        {
            public ImageSource ImageURL { get; set; }
            public string ImageText { get; set; }
            public string ImagePath { get; internal set; }
        }
    }
}

