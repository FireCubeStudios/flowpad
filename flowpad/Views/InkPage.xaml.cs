using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using flowpad.Services.Ink;
using Flowpad.Services.Ink.UndoRedo;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using flowpad.Helpers;
using flowpad.Services;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Graphics.Printing;
using MUXC = Microsoft.UI.Xaml.Controls;
using System.Numerics;
using System.Drawing;
using Microsoft.Graphics.Canvas.Effects;
using FanKit.Transformers;
using Windows.UI.Input.Inking.Core;
using Windows.Devices.Input;
using Windows.UI.Xaml.Input;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace flowpad.Views
{
    // For more information regarding Windows Ink documentation and samples see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/pages/ink.md
    public sealed partial class InkPage : Page
    {
       public static Frame InkFrame { get; set; }
        enum Mode
        {
            Freeform = 0,
            SnapX = 1,
            SnapY = 2
        }
        public bool IsCanvasRulerVisible = false;
        static readonly decimal MAX_SCALE_FACTOR = 8.0m;
        static readonly decimal MIN_SCALE_FACTOR = 0.5m;
        static readonly int BASE_GRID_SIZE = 20;
        public InkPage()
        {
            this.InitializeComponent();
            Loaded += (sender, eventArgs) =>
            {

       
                FindName("NavigationRibbon");
                FindName("canvasScroll");
                SetCanvasSize();
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                Window.Current.SetTitleBar(TitleGrid);

                var appViewTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                toolbar.TargetInkCanvas = inkCanvas;
                InkToolbarToolButton penButton = toolbar.GetToolButton(InkToolbarTool.BallpointPen);
                toolbar.ActiveTool = penButton;
                appViewTitleBar.ButtonBackgroundColor = Colors.Transparent;
                appViewTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                strokeService = new InkStrokesService(inkCanvas.InkPresenter);
                var analyzer = new InkAsyncAnalyzer(inkCanvas, strokeService);
                var selectionRectangleService = new InkSelectionRectangleService(inkCanvas, selectionCanvas, strokeService);
                lassoSelectionService = new InkLassoSelectionService(inkCanvas, selectionCanvas, strokeService, selectionRectangleService);
                copyPasteService = new InkCopyPasteService(strokeService);
                nodeSelectionService = new InkNodeSelectionService(inkCanvas, selectionCanvas, analyzer, strokeService, selectionRectangleService);
                pointerDeviceService = new InkPointerDeviceService(inkCanvas);
                undoRedoService = new InkUndoRedoService(inkCanvas, strokeService);
                transformService = new InkTransformService(drawingCanvas, strokeService);
                fileService = new InkFileService(inkCanvas, strokeService);
                zoomService = new InkZoomService(canvasScroll);
                this.currentScaleFactor = 1.0m;
                this.currentMode = Mode.Freeform;
                this.graphPaper.Visibility = Visibility.Collapsed;
                ThemeSwitch.IsOn = ThemeSelectorService.Theme != ElementTheme.Default && ThemeSelectorService.Theme == ElementTheme.Dark ? true : false;
                //   this.UpdateModeText();
              IsCanvasRulerVisible = (bool)CanvasRulerToggle.IsChecked;
                this.currentGridSize = BASE_GRID_SIZE;
                FindName("graphPaper");
                this.graphPaper.GridSize = this.currentGridSize;
                inkCanvas.InkPresenter.StrokeInput.StrokeStarted += FileSaveCheck_Click;
                //Canvas
                this.CanvasControl.SizeChanged += (s, e) =>
                {
                    if (e.NewSize == e.PreviousSize) return;

                    Windows.Foundation.Size size = e.NewSize;
                    this.CanvasTransformer.Width = (int)size.Width / 3;
                    this.CanvasTransformer.Height = (int)size.Height / 3;

                    this.CanvasTransformer.Size = size;
                    this.CanvasTransformer.ReloadMatrix();
                };
                this.CanvasControl.CreateResources += (sender, args) =>
                {
                    this.CanvasTransformer.Fit();
                    this.CanvasTransformer.Radian = 0;
                    this.CanvasTransformer.ReloadMatrix();
                    this.CanvasControl.Invalidate();
                };
                if (lassoSelectionButtonIsChecked == true)
                {
                    Copy.Visibility = Visibility.Visible;
                    Cut.Visibility = Visibility.Visible;
                    Paste.Visibility = Visibility.Visible;
                }
                else
                {
                    Copy.Visibility = Visibility.Collapsed;
                    Cut.Visibility = Visibility.Collapsed;
                    Paste.Visibility = Visibility.Collapsed;
                }
                this.wetUpdateSource = CoreWetStrokeUpdateSource.Create(this.inkCanvas.InkPresenter);
                this.wetUpdateSource.WetStrokeStarting += OnStrokeStarting;
                this.wetUpdateSource.WetStrokeContinuing += OnStrokeContinuing;
                this.CanvasControl.Draw += (sender, args) =>
                {
                   /* args.DrawingSession.DrawCrad(new ColorSourceEffect
                    {
                        Color = Windows.UI.Colors.White
                    }, this.CanvasTransformer);
                   */
                    args.DrawingSession.DrawAxis(this.CanvasTransformer);
                    args.DrawingSession.DrawRuler(this.CanvasTransformer);
                };


                //Single
                this.CanvasOperator.Single_Start += (point) =>
                {
                };
                this.CanvasOperator.Single_Delta += (point) =>
                {
                    this.CanvasTransformer.Position = point;

                    this.CanvasTransformer.ReloadMatrix();
                    this.CanvasControl.Invalidate();//Invalidate
                };
                this.CanvasOperator.Single_Complete += (point) =>
                {
                };

                //Right
                this.CanvasOperator.Right_Start += (point) =>
                {
                    this.CanvasTransformer.CacheMove(point);
                };
                this.CanvasOperator.Right_Delta += (point) =>
                {
                    this.CanvasTransformer.Move(point);
                    this.CanvasControl.Invalidate();
                };
                this.CanvasOperator.Right_Complete += (point) =>
                {
                    this.CanvasTransformer.Move(point);
                    this.CanvasControl.Invalidate();
                };


                //Double
                this.CanvasOperator.Double_Start += (center, space) =>
                {
                    this.CanvasTransformer.CachePinch(center, space);
                    this.CanvasControl.Invalidate();
                };
                this.CanvasOperator.Double_Delta += (center, space) =>
                {
                    this.CanvasTransformer.Pinch(center, space);
                    this.CanvasControl.Invalidate();
                };
                this.CanvasOperator.Double_Complete += (center, space) =>
                {
                    this.CanvasControl.Invalidate();
                };

                //Wheel
                this.CanvasOperator.Wheel_Changed += (point, space) =>
                {
                    if (space > 0)
                        this.CanvasTransformer.ZoomIn(point);
                    else
                        this.CanvasTransformer.ZoomOut(point);

                    this.CanvasControl.Invalidate();
                };

                CanvasTransformer.Scale = 1;
               CanvasTransformer.Radian = 0;
                CanvasTransformer.Fit();
                CanvasTransformer.ReloadMatrix();
                MouseInkingButton.IsChecked = true;
                TouchInkingButton.IsChecked = true;
                InkSurfacePen.IsChecked = true;
                strokeService.CopyStrokesEvent += (s, e) => RefreshEnabledButtons();
                strokeService.SelectStrokesEvent += (s, e) => RefreshEnabledButtons();
                strokeService.ClearStrokesEvent += (s, e) => RefreshEnabledButtons();
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


            Windows.UI.Core.Preview.SystemNavigationManagerPreview.GetForCurrentView().CloseRequested +=
             async (sender, args) =>
             {
                 try
                 {
                     args.Handled = true;
                     var Folder = await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("Shared");
                     await Folder.DeleteAsync();
                     try
                     {

                         IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();



                         if (currentStrokes.Count == 0 || FileSaved == true)
                         {
                             App.Current.Exit();
                         }
                         else
                         {


                             var result = await SaveInkConfirmDialog.ShowAsync();
                             if (result == ContentDialogResult.Primary)
                             {
                                 await SaveFileDialogPrompt.ShowAsync();
                             }
                             else if (result == ContentDialogResult.Secondary)
                             {
                                 SaveFileDialogPrompt.Hide();
                             }
                             else
                             {
                                 App.Current.Exit();
                             }

                         }
                     }
                     catch
                     {
                         App.Current.Exit();

                     }
                 }
                 catch
                 {
                     try
                     {

                         IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();



                         if (currentStrokes.Count == 0 || FileSaved == true)
                         {
                             App.Current.Exit();
                         }
                         else
                         {


                             var result = await SaveInkConfirmDialog.ShowAsync();
                             if (result == ContentDialogResult.Primary)
                             {
                                 await SaveFileDialogPrompt.ShowAsync();
                             }
                             else if (result == ContentDialogResult.Secondary)
                             {
                                 SaveFileDialogPrompt.Hide();
                             }
                             else
                             {
                                 App.Current.Exit();
                             }

                         }
                     }
                     catch
                     {
                         App.Current.Exit();

                     }
                 }
             };
          
        }
        /// <summary>
        /// event handler for MainPage.Loaded
        /// </summary>
  
        void OnStrokeStarting(CoreWetStrokeUpdateSource sender, CoreWetStrokeUpdateEventArgs args)
        {
            // as the stroke is starting, reset our member variables which store
            // which X or Y point we want to snap to.
            this.snapX = this.snapY = null;

            // I am assuming that we do get a first ink point.
            InkPoint firstPoint = args.NewInkPoints.First();

            // now decide whether we need to set up a snap point for the X value or
            // one for the Y value.
            if (this.currentMode == Mode.SnapX)
            {
                this.snapX = this.NearestGridSizeMultiple(firstPoint.Position.X);
            }
            else if (this.currentMode == Mode.SnapY)
            {
                this.snapY = this.NearestGridSizeMultiple(firstPoint.Position.Y);
            }
            this.SnapPoints(args.NewInkPoints);
        }
        double? snapX;
        double? snapY;


        void SnapPoints(IList<InkPoint> newInkPoints)
        {
            // do we need to do any snapping?
            if (this.currentMode != Mode.Freeform)
            {
                for (int i = 0; i < newInkPoints.Count; i++)
                {
                    if (this.snapX.HasValue)
                    {
                        // replace this point with the same point but with the X value snapped.
                        newInkPoints[i] = new InkPoint(
                          new Windows.Foundation.Point(this.snapX.Value, newInkPoints[i].Position.Y),
                          newInkPoints[i].Pressure);
                    }
                    else if (this.snapY.HasValue)
                    {
                        // replace this point with the same point but with the Y value snapped.
                        newInkPoints[i] = new InkPoint(
                          new Windows.Foundation.Point(newInkPoints[i].Position.X, this.snapY.Value),
                          newInkPoints[i].Pressure);
                    }
                }
            }
        }

        double NearestGridSizeMultiple(double value)
        {
            // Note. I have added a new member variable 'currentGridSize' which I keep
            // in sync with the GridSize of the GraphPaperUserControl.
            // This is because this code runs on a non-UI thread so it cannot simply
            // call into that property on the user control.

            var divisor = value / this.currentGridSize;
            var fractional = divisor - Math.Floor(divisor);

            if (fractional >= 0.5)
            {
                divisor = Math.Ceiling(divisor);
            }
            else
            {
                divisor = Math.Floor(divisor);
            }
            return (divisor * this.currentGridSize);
        }
        int currentGridSize;


        void OnStrokeContinuing(CoreWetStrokeUpdateSource sender, CoreWetStrokeUpdateEventArgs args)
        {
            this.SnapPoints(args.NewInkPoints);
        }

     //   void UpdateModeText() => this.txtMode.Text = this.currentMode.ToString();

        void OnInkCanvasManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var newScaleFactor = (decimal)e.Delta.Scale * this.currentScaleFactor;

            if ((newScaleFactor <= MAX_SCALE_FACTOR) && (newScaleFactor >= MIN_SCALE_FACTOR))
            {
                this.currentScaleFactor = newScaleFactor;

                var newGridSize = (int)(this.currentScaleFactor * BASE_GRID_SIZE);

                if (newGridSize != this.graphPaper.GridSize)
                {
                    this.graphPaper.GridSize = newGridSize;
                    this.currentGridSize = newGridSize;
                }
            }
        }
        void OnInkCanvasTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Touch)
            {
                // Apologies for doing such a horrible thing to an enum.
                this.currentMode =
                  (Mode)((((int)this.currentMode) + 1) % ((int)Mode.SnapY + 1));

            //    this.UpdateModeText();
            }
        }
        CoreWetStrokeUpdateSource wetUpdateSource;
        Mode currentMode;
        decimal currentScaleFactor;

  



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
        private InkCopyPasteService copyPasteService;
        private InkLassoSelectionService lassoSelectionService;
        private bool cutButtonIsEnabled;
        private bool copyButtonIsEnabled;
        private bool pasteButtonIsEnabled;
        private InkNodeSelectionService nodeSelectionService;
        private InkPointerDeviceService pointerDeviceService;
        private InkUndoRedoService undoRedoService;
        private InkTransformService transformService;
        private InkFileService fileService;
        private InkZoomService zoomService;
        public string FileOpen;
        private StorageFile imageFile;
        public Boolean FileSaved = false;
        private ElementTheme _elementTheme = ThemeSelectorService.Theme;
        public PrintHelper printHelper;




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

        public bool CutButtonIsEnabled
        {
            get => cutButtonIsEnabled;
            set => Set(ref cutButtonIsEnabled, value);
        }

        public bool CopyButtonIsEnabled
        {
            get => copyButtonIsEnabled;
            set => Set(ref copyButtonIsEnabled, value);
        }

        public bool PasteButtonIsEnabled
        {
            get => pasteButtonIsEnabled;
            set => Set(ref pasteButtonIsEnabled, value);
        }
        private void Copy_Click(object sender, RoutedEventArgs e) => copyPasteService?.Copy();

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            copyPasteService?.Cut();
            ClearSelection();
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            copyPasteService?.Paste();
            ClearSelection();
        }
        private void ConfigLassoSelection(bool enableLasso)
        {
            if (enableLasso)
            {
                lassoSelectionService?.StartLassoSelectionConfig();
            }
            else
            {
                lassoSelectionService?.EndLassoSelectionConfig();
            }
        }
        public bool LassoSelectionButtonIsChecked
        {
            get => lassoSelectionButtonIsChecked;
            set
            {
                Set(ref lassoSelectionButtonIsChecked, value);
                ConfigLassoSelection(value);
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
          inkCanvas.Width = Math.Max(canvasScroll.ViewportWidth, Window.Current.Bounds.Width - 160);
           inkCanvas.Height = Math.Max(canvasScroll.ViewportHeight, Window.Current.Bounds.Height - 113);
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

          //  UnloadObject(SettingsPanel);

        }
        public void MyFancyPanel_BackdropClicked(object sender, RoutedEventArgs e)

        {

           // UnloadObject(SettingsPanel);

        }

        private void Initialize()
        {
            VersionDescription = GetVersionDescription();

        
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

            if (param != null) await ThemeSelectorService.SetThemeAsync((ElementTheme)param);
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
                        SaveFileConfirmed.IsOpen = true; // the notification will appear for 2 seconds
                        FileSaved = true;
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
                    SaveFileConfirmed.IsOpen = true; // the notification will appear for 2 seconds
                    FileSaved = true;
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
            FileSaved = true;
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
            try
            {
                Windows.Storage.StorageFile file = await StorageFile.GetFileFromPathAsync(FileOpen);

                // Open a file stream for reading.
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                // Read from file.

                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                    FileSaved = true;
                }
            }
            catch
            {
                return;
            }
        }
        private async void SaveFileAsImage(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count == 0)
            {
                return;
            }
            else
            {
                string SelectedItem = "{x:Bind SelectedItem}";
                string textbog = "{x:Bind E.Text mode=OneWay}";
                string gob = (textbog + SelectedItem);
                //  StorageFolder storageFolder = KnownFolders.SavedPictures;
                // var file = await storageFolder.CreateFileAsync(gob, CreationCollisionOption.ReplaceExisting);

                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96);



                Windows.Storage.Pickers.FileSavePicker savePicker =
                          new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation =
                    Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add(".jpeg format", new List<string>() { ".jpeg" });
                savePicker.FileTypeChoices.Add(".Png format", new List<string>() { ".png" });
                savePicker.FileTypeChoices.Add(
                   ".Bmp format",
                   new List<string>() { ".bmp" });
                savePicker.FileTypeChoices.Add(
                   ".gif format",
                   new List<string>() { ".gif" });

                savePicker.FileTypeChoices.Add(
                                 ".jpegxr format",
                                 new List<string>() { ".jpegxr" });
                savePicker.FileTypeChoices.Add(
                 ".tiff format",
                 new List<string>() { ".tiff" });
                savePicker.DefaultFileExtension = ".jpeg";
                savePicker.SuggestedFileName = "Inkdrawing";

                // Show the file picker.
                Windows.Storage.StorageFile file =
                    await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    RenderTargetBitmap rtb = new RenderTargetBitmap();
                    await rtb.RenderAsync(inkCanvas);

                    var pixelBuffer = await rtb.GetPixelsAsync();
                    var pixels = pixelBuffer.ToArray();
                    var displayInformation = DisplayInformation.GetForCurrentView();

                    using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                             BitmapAlphaMode.Premultiplied,
                                             (uint)rtb.PixelWidth,
                                             (uint)rtb.PixelHeight,
                                             displayInformation.RawDpiX,
                                             displayInformation.RawDpiY,
                                             pixels);
                        await encoder.FlushAsync();

                        // await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Jpeg, 1f);

                    }

                }
                // save results
                FileSaved = true;
            }

        }

        private void OpenFileDialogPrompt_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {


        }
        public void SelectImageButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareTextHandler);
            ShareImageButton_Click();
        }
        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            if (this.imageFile != null)
            {
                DataRequest requestData = e.Request;
                requestData.Data.Properties.Title = ShareNameBox.Text;
                requestData.Data.Properties.Description = DescriptionBox.Text;
                List<IStorageItem> imageItems = new List<IStorageItem>();
                imageItems.Add(this.imageFile);
                requestData.Data.SetStorageItems(imageItems);
                RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(this.imageFile);
                requestData.Data.Properties.Thumbnail = imageStreamRef;
                requestData.Data.SetBitmap(imageStreamRef);
            }
        }
        private async void ShareImageButton_Click()
        {
            try
            {
                var appFolder = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFolderAsync("Shared");
                StorageApplicationPermissions.FutureAccessList.Add(appFolder);
                Windows.Storage.StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(appFolder.Path.ToString());
                Windows.Storage.StorageFile file = await folder.CreateFileAsync(ShareNameBox.Text + ".gif", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                    await outputStream.FlushAsync();
                }
                stream.Dispose();

                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                imageFile = await folder.GetFileAsync(ShareNameBox.Text + ".gif");
                ShowUIButton_Click();
            }
            catch
            {
                var Folder = await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("Shared");
                await Folder.DeleteAsync();
                var appFolder = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFolderAsync("Shared");
                StorageApplicationPermissions.FutureAccessList.Add(appFolder);
                Windows.Storage.StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(appFolder.Path.ToString());
                Windows.Storage.StorageFile file = await folder.CreateFileAsync(ShareNameBox.Text + ".gif", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                    await outputStream.FlushAsync();
                }
                stream.Dispose();

                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                imageFile = await folder.GetFileAsync(ShareNameBox.Text + ".gif");
                ShowUIButton_Click();
            }
        }
        private void ShowUIButton_Click()
        {
            DataTransferManager.ShowShareUI();
        }
        public async void ShowShareDialog(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count == 0)
            {
                return;
            }
            else
            {
                await ShareFileDialogPrompt.ShowAsync();
            }


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
        private void FileSaveCheck_Click(InkStrokeInput sender, PointerEventArgs args)
        {
            if (FileSaved == true)
            {
                FileSaved = false;
            }
            return;
        }
        public class Images
        {
            public ImageSource ImageURL { get; set; }
            public string ImageText { get; set; }
            public string ImagePath { get; internal set; }
        }
        private async void Printer_Click(object sender, RoutedEventArgs e)
        {

            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count == 0)
            {
                return;
            }
            // Create a new PrintHelperOptions instance
            var defaultPrintHelperOptions = new PrintHelperOptions();

            // Configure options that you want to be displayed on the print dialog
            defaultPrintHelperOptions.AddDisplayOption(StandardPrintTaskOptions.Orientation);
            defaultPrintHelperOptions.Orientation = PrintOrientation.Landscape;

            defaultPrintHelperOptions.AddDisplayOption(StandardPrintTaskOptions.Copies);
            defaultPrintHelperOptions.AddDisplayOption(StandardPrintTaskOptions.Orientation);
            defaultPrintHelperOptions.AddDisplayOption(StandardPrintTaskOptions.MediaSize);
            defaultPrintHelperOptions.AddDisplayOption(StandardPrintTaskOptions.Collation);
            defaultPrintHelperOptions.AddDisplayOption(StandardPrintTaskOptions.Duplex);
            // Create a new PrintHelper instance
            // "container" is a XAML panel that will be used to host printable control. 
            // It needs to be in your visual tree but can be hidden with Opacity = 0
            var inkStream = new InMemoryRandomAccessStream();
            await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(inkStream.GetOutputStreamAt(0));
            var inkBitmap = new BitmapImage();
            await inkBitmap.SetSourceAsync(inkStream);

            // You need to adjust Margin to layout the image properly in the print-page. 
            var inkBounds = inkCanvas.InkPresenter.StrokeContainer.BoundingRect;
            var inkMargin = new Thickness(inkBounds.Left, inkBounds.Top, inkCanvas.ActualWidth - inkBounds.Right, inkCanvas.ActualHeight - inkBounds.Bottom);

            // Prepare Viewbox+Image to be printed.
            var inkViewbox = new Viewbox()
            {
                Child = new Image()
                {
                    Source = inkBitmap,
                    Margin = inkMargin
                },
                Width = inkCanvas.ActualWidth,
                Height = inkCanvas.ActualHeight
            };

            printHelper = new PrintHelper(PanelC, defaultPrintHelperOptions);
            printHelper.AddFrameworkElementToPrint(inkViewbox);

            printHelper.OnPrintFailed += PrintHelper_OnPrintFailed;
            printHelper.OnPrintSucceeded += PrintHelper_OnPrintSucceeded;

            // Start printing process
            await printHelper.ShowPrintUIAsync("Flowpad");

            // Event handlers
            printHelper.Dispose();

        }
        private async void PrintHelper_OnPrintSucceeded()
        {
            printHelper.Dispose();
            var dialog = new MessageDialog("Printing successful.");
            await dialog.ShowAsync();
        }

        private async void PrintHelper_OnPrintFailed()
        {
            printHelper.Dispose();
            var dialog = new MessageDialog("Printing failed.");
            await dialog.ShowAsync();
        }

        private void InkCanvas_ContextRequested(UIElement sender, Windows.UI.Xaml.Input.ContextRequestedEventArgs args)
        {
            FlyoutShowOptions Option = new FlyoutShowOptions();
            Option.ShowMode = FlyoutShowMode.Transient;

            CommandBarFlyoutCommands.ShowAt(inkCanvas, Option);
        }

        private void InkSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AutoSaveTip.IsOpen = true;
        }

        private async void SaveFileConfirmed_ActionButtonClick(MUXC.TeachingTip sender, object args)
        {
            var appFolder = await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings");
            await Launcher.LaunchFolderAsync(appFolder);
        }

        private void TiltSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (inkCanvas.InkPresenter != null)

                {
                    var drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                    ToggleSwitch toggleSwitch = sender as ToggleSwitch;
                    if (toggleSwitch != null)
                    {
                        if (toggleSwitch.IsOn == true)
                        {
                            drawingAttributes.IgnoreTilt = false;
                        }
                        else
                        {
                            drawingAttributes.IgnoreTilt = true;
                        }
                        inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
                    }
                }
            }
            catch
            {

            }
        }
        private void PressureSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (inkCanvas.InkPresenter != null)

                {
                    var drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                    ToggleSwitch toggleSwitch = sender as ToggleSwitch;
                    if (toggleSwitch != null)
                    {
                        if (toggleSwitch.IsOn == true)
                        {
                            drawingAttributes.IgnorePressure = false;
                        }
                        else
                        {
                            drawingAttributes.IgnorePressure = true;

                        }
                        inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
                    }
                }
            }
            catch
            {

            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (inkCanvas.InkPresenter != null)

                {
                    var drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                    ComboBox comboBox = sender as ComboBox;
                    Windows.UI.Input.Inking.InkManager inkManager =
          new Windows.UI.Input.Inking.InkManager();
                    // Get the ComboBox selected item text
                    string indexTip = comboBox.SelectedItem.ToString();
                    switch (indexTip)
                    {
                        case "Circle":
                            drawingAttributes.PenTip = PenTipShape.Circle;
                            break;
                        case "Rectangle":
                            drawingAttributes.PenTip = PenTipShape.Rectangle;
                            break;
                    }
                    inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
                }
            }
            catch
            {

            }
        }

        private async void Home_Click(object sender, RoutedEventArgs e)
        {
          try
          {
              //  var Folder = await Windows.Storage.KnownFolders.PicturesLibrary.GetFolderAsync("InkDrawings");
                //await Folder.DeleteAsync();
               try
               {

                    IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();



                    if (currentStrokes.Count == 0 || FileSaved == true)
                    {
                      this.Frame.Navigate(typeof(HomePage));
                    }
                    else
                    {


                        var result = await SaveInkConfirmDialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            await SaveFileDialogPrompt.ShowAsync();
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            SaveFileDialogPrompt.Hide();
                        }
                        else
                        {
                          this.Frame.Navigate(typeof(HomePage));
                        }

                    }
                }
                catch
                {
                 this.Frame.Navigate(typeof(HomePage));

                }
            }
            catch
            {
                this.Frame.Navigate(typeof(HomePage));
            }
        }

        private void ColorPicker_ColorChangeCompleted(object sender, Windows.UI.Color value)
        {
            InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            drawingAttributes.Color = value;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
        }

        private void CanvasRulerToggle_Click(object sender, RoutedEventArgs e)
        {
            if (IsCanvasRulerVisible == true)
            {
                CanvasControl.Visibility = Visibility.Visible;
            }
            else
            {
                CanvasControl.Visibility = Visibility.Collapsed;
            }
        }

        private void OpenGridButton_Click(object sender, RoutedEventArgs e)
        {
            GridOptions.IsOpen = true;
        }

        private void GridLineSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                this.graphPaper.GridSize = (int)e.NewValue;
                this.currentGridSize = (int)e.NewValue;
            }
            catch
            {

            }
        }
     
        private void GridLinesSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.graphPaper.Visibility == Visibility.Collapsed)
            {
                this.graphPaper.Visibility = Visibility.Visible;
            }
            else
            {
                this.graphPaper.Visibility = Visibility.Collapsed;
                this.currentMode = Mode.Freeform;
            }
        }
    
        private void GLRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
            if (SY.IsChecked == true)
            {
                    this.currentMode = Mode.SnapY;
            }
            else if (SX.IsChecked == true)
            {
                this.currentMode = Mode.SnapX;
            }
            else
            {
                    this.currentMode = Mode.Freeform;
                }
        }
            catch
            {

            }
        }

        private void LassoToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if(lassoSelectionButtonIsChecked == true)
            {
                Copy.Visibility = Visibility.Visible;
                Cut.Visibility = Visibility.Visible;
                Paste.Visibility = Visibility.Visible;
            }
            else
            {
                Copy.Visibility = Visibility.Collapsed;
                Cut.Visibility = Visibility.Collapsed;
                Paste.Visibility = Visibility.Collapsed;
            }
        }

        private void ColorPicker_ColorChangeCompleted(ColorPicker sender, ColorChangedEventArgs args)
        {
            InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            drawingAttributes.Color = args.NewColor;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
        }

        private async void ThemeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (ThemeSwitch.IsOn)
            {
                await ThemeSelectorService.SetThemeAsync(ElementTheme.Dark);
            }
            else
            {
                await ThemeSelectorService.SetThemeAsync(ElementTheme.Light);
            }
        }

        private void CanvasDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inkCanvas.Width = WidthBox.Value;
            inkCanvas.Height = HeightBox.Value;
        }

        private async void SizeButton_Click(object sender, RoutedEventArgs e)
        {
            WidthBox.Value = inkCanvas.Width;
            HeightBox.Value = inkCanvas.Height;
            await CanvasDialog.ShowAsync();
        }
    }
}
