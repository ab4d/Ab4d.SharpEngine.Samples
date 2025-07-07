using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Animations;
using Ab4d.SharpEngine.Samples.Common.Diagnostics;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Ab4d.SharpEngine.Samples.WinUI.Diagnostics
{
    /// <summary>
    /// Interaction logic for DiagnosticsWindow.xaml
    /// </summary>
    public partial class DiagnosticsWindow : Window
    {
        public const int InitialWindowWidth = 340;
        private bool _isInitialSizeSet;

        public bool SetWindowHeightToContent = true;
        public double MinWindowHeight = 600; // This is needed to correctly show the menu

        private DispatcherTimer? _updateStatisticsTimer;

        private CommonDiagnostics _commonDiagnostics;

        public string DumpFileName { get; set; }

        private DateTime _lastStatisticsUpdate;
        private bool _showRenderingStatistics = true;

        private LogMessagesWindow? _logMessagesWindow;


        public ISharpEngineSceneView? SharpEngineSceneView
        {
            get => _commonDiagnostics.SharpEngineSceneView;
            set
            {
                _commonDiagnostics.SharpEngineSceneView = value;

                if (value != null)
                    RegisterSceneView(value);
            }
        }


        public DiagnosticsWindow()
        {
            InitializeComponent();

            this.Title = "Diagnostics";
            WinUiUtils.SetWindowIcon(this, @"Assets\sharp-engine-logo.ico");
            WinUiUtils.ChangeCursor(LogWarningsPanel, InputSystemCursor.Create(InputSystemCursorShape.Hand));


            var bitmapIO = new WinUIBitmapIO();
            _commonDiagnostics = new CommonDiagnostics(bitmapIO);

            _commonDiagnostics.NewLogMessageAddedAction = () => _logMessagesWindow?.UpdateLogMessages();

            _commonDiagnostics.DeviceInfoChangedAction = () => DeviceInfoTextBlock.Text = _commonDiagnostics.GetDeviceInfo();

            _commonDiagnostics.WarningsCountChangedAction = UpdateWarningsCount;

            _commonDiagnostics.OnSceneRenderedAction = UpdateStatistics;


            // Only show "Full Logging" CheckBox when SharpEngine is compiled with full logging
            if (Ab4d.SharpEngine.Utilities.Log.MinUsedLogLevel != LogLevels.Trace)
            {
                ActionsRootMenuItem.Items.Remove(FullLoggingSeparator);
                ActionsRootMenuItem.Items.Remove(FullLoggingCheckBox);
            }

            if (!_commonDiagnostics.IsGltfExporterAvailable)
            {
                ExportToGltfMenuItem.IsEnabled = false;
                ToolTipService.SetToolTip(ExportToGltfMenuItem, "Add reference to the Ab4d.SharpEngine.glTF to enabled export to glTF");
            }

            string dumpFolder;
            if (System.IO.Directory.Exists(@"C:\temp"))
                dumpFolder = @"C:\temp\";
            else
                dumpFolder = System.IO.Path.GetTempPath();

            DumpFileName = System.IO.Path.Combine(dumpFolder, "SharpEngineDump.txt");


            SharpEngineInfoTextBlock.Text = _commonDiagnostics.GetSharpEngineInfoText();

            StartShowingStatistics();

            this.Closed += delegate(object sender, WindowEventArgs args)
            {
                if (_logMessagesWindow != null)
                {
                    try
                    {
                        _logMessagesWindow.Close();
                    }
                    catch
                    {
                        // Maybe the window was already closed
                    }

                    _logMessagesWindow = null;
                }

                _commonDiagnostics.UnregisterCurrentSceneView();
            };
        }
        
        private void UpdateEnabledMenuItems()
        {
            // On each new scene reset the StartStopCameraRotationMenuItem text
            StartStopCameraRotationMenuItem.Text = "Toggle camera rotation";

            if (Ab4d.SharpEngine.Utilities.Log.LogLevel == LogLevels.All)
                FullLoggingCheckBox.IsChecked = true;
        }
        
        private void StartShowingStatistics()
        {
            if (SharpEngineSceneView != null)
            {
                ResultsTitleTextBlock.Visibility = Visibility.Visible;
                ResultsTitleTextBlock.Text = _showRenderingStatistics ? "Rendering statistics:" : "Camera info:";
            }
            else
            {
                ResultsTitleTextBlock.Visibility = Visibility.Collapsed;
            }

            StatisticsTextBlock.Visibility = Visibility.Visible;

            _commonDiagnostics.StartShowingStatistics();
        }

        private void EndShowingStatistics()
        {
            StatisticsTextBlock.Visibility = Visibility.Collapsed;
            ResultsTitleTextBlock.Visibility = Visibility.Collapsed;

            DisposeUpdateStatisticsTimer();

            _commonDiagnostics.EndShowingStatistics();
        }

        private void RegisterSceneView(ISharpEngineSceneView? sharpEngineSceneView)
        {
            _commonDiagnostics.RegisterSceneView(sharpEngineSceneView);

            if (sharpEngineSceneView == null)
                DisposeUpdateStatisticsTimer();

            StartShowingStatistics();

            if (sharpEngineSceneView != null && 
                sharpEngineSceneView.SceneView.IsCollectingStatistics)
            {
                ResultsTitleTextBlock.Visibility = Visibility.Visible;
                UpdateStatistics();
            }

            UpdateEnabledMenuItems();
            CheckForCaptureSupport();
        }

        private void CheckForCaptureSupport()
        {
            if (SharpEngineSceneView == null)
                return;

            var isCaptureFrameAvailable = SharpEngineSceneView.SceneView.IsCaptureFrameAvailable();

            if (isCaptureFrameAvailable)
                CaptureButton.Visibility = Visibility.Visible;
        }

        private void SetupUpdateStatisticsTimer(double milliseconds)
        {
            if (_updateStatisticsTimer == null)
            {
                _updateStatisticsTimer = new DispatcherTimer();
                _updateStatisticsTimer.Tick += CheckToUpdateStatisticsOrCameraInfo;
            }

            _updateStatisticsTimer.Interval = TimeSpan.FromMilliseconds(milliseconds);
            _updateStatisticsTimer.Start();
        }

        private void StopUpdateStatisticsTimer()
        {
            if (_updateStatisticsTimer != null)
                _updateStatisticsTimer.Stop();
        }
        
        private void DisposeUpdateStatisticsTimer()
        {
            if (_updateStatisticsTimer != null)
            {
                _updateStatisticsTimer.Stop();
                _updateStatisticsTimer = null;
            }
        }

        private void CheckToUpdateStatisticsOrCameraInfo(object? sender, object? eventArgs)
        {
            var elapsed = (DateTime.Now - _lastStatisticsUpdate).TotalMilliseconds;

            if (_updateStatisticsTimer != null && elapsed > (_updateStatisticsTimer.Interval.TotalMilliseconds * 0.9))
            {
                if (_showRenderingStatistics)
                {
                    if (SharpEngineSceneView?.SceneView.Statistics != null)
                        UpdateStatistics();
                }
                else
                {
                    UpdateCameraInfo();
                }
            }
        }

        private void StartStopCameraRotationMenuItem_OnTapped(object sender, RoutedEventArgs args)
        {
            _commonDiagnostics.ToggleCameraRotation();
        }
        
        private void DumpSceneNodesMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetSceneNodesDumpString());
        }

        private void DumpRenderingLayersMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetRenderingLayersDumpString());
        }

        private void DumpRenderingStepsMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetRenderingStepsDumpString());
        }

        private void DumpUsedMaterialsMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetUsedMaterialsDumpString());
        }

        private void DumpMemoryMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetMemoryDumpString());
        }
        
        private void DumpResourcesMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetResourcesDumpString(groupByType: false));
        }
        
        private void DumpResourcesGroupByTypeMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetResourcesDumpString(groupByType: true));
        }
        
        private void DumpResourcesForDelayedDisposalMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetResourcesForDelayedDisposalDumpString());
        }

        private void DumpEngineSettingsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetEngineSettingsDump());
        }

        private void DumpSystemInfoMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetSystemInfoDumpString());
        }
        
        private void ExportToGltfMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;

            // TOOD: How to create SaveFileDialog in WinUI?

            string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngineScene.glb");

            try
            {
                _commonDiagnostics.ExportSceneToGltf(SharpEngineSceneView.Scene, SharpEngineSceneView.SceneView, fileName);
            }
            catch
            {
                // pass
            }
        }
        
        private void ExportToObjMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;

            // TOOD: How to create SaveFileDialog in WinUI?

            string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngineScene.glb");

            try
            {
                _commonDiagnostics.ExportSceneToObj(SharpEngineSceneView.Scene, fileName);
            }
            catch
            {
                // pass
            }
        }
        
        private void RenderToBitmapMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            _commonDiagnostics.SaveRenderedSceneToDesktop();
        }
        
        private void ShowFullSceneDumpMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;

            var dumpText = _commonDiagnostics.GetFullSceneDumpString();

            // Start with empty DumpFile 
            System.IO.File.WriteAllText(DumpFileName, dumpText);
            StartProcess(DumpFileName);
        }
        
        private void DumpCameraDetailsMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetCameraDetailsDumpString());
        }

        private void GarbageCollectMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            _commonDiagnostics.GarbageCollect();
        }
        
        private void ShowStatisticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartShowingStatistics();
            ShowButtons(showStopPerformanceAnalyzerButton: false, showShowStatisticsButton: false);
        }

        private void ShowButtons(bool showStopPerformanceAnalyzerButton, bool showShowStatisticsButton)
        {
            ShowStatisticsButton.Visibility = showShowStatisticsButton ? Visibility.Visible : Visibility.Collapsed;

            ButtonsPanel.Visibility = showStopPerformanceAnalyzerButton || showShowStatisticsButton ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateWarningsCount(int warningsCount)
        {
            if (this.DispatcherQueue.HasThreadAccess)
            {
                WarningsCountTextBlock.Text = warningsCount.ToString();
                LogWarningsPanel.Visibility = warningsCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => 
                {
                    WarningsCountTextBlock.Text = warningsCount.ToString();
                    LogWarningsPanel.Visibility = warningsCount > 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
        }

        private void UpdateStatistics()
        {
            if (SharpEngineSceneView == null || SharpEngineSceneView.SceneView.Statistics == null)
            {
                StatisticsTextBlock.Visibility = Visibility.Collapsed;
                return;
            }

            StopUpdateStatisticsTimer();


            var now = DateTime.Now;

            if (CommonDiagnostics.UpdateStatisticsInterval > 0 && _lastStatisticsUpdate != DateTime.MinValue)
            {
                double elapsed = (now - _lastStatisticsUpdate).TotalMilliseconds;

                if (elapsed < CommonDiagnostics.UpdateStatisticsInterval) // Check if the required elapsed time has already passed
                {
                    // We skip showing the result for this frame, but set up timer so if there will no 
                    // additional frame rendered then we will show this frame info after the timer kick in.
                    SetupUpdateStatisticsTimer(CommonDiagnostics.UpdateStatisticsInterval * 3);

                    return;
                }
            }


            if (_showRenderingStatistics)
            {
                var statisticsText = _commonDiagnostics.GetRenderingStatisticsText();
                StatisticsTextBlock.Text = statisticsText;
            }
            else
            {
                UpdateCameraInfo();
            }

            _lastStatisticsUpdate = now;

            ResultsTitleTextBlock.Visibility = Visibility.Visible;
            StatisticsTextBlock.Visibility = Visibility.Visible;

            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            if (!SetWindowHeightToContent && _isInitialSizeSet)
                return;

            StatisticsTextBlock.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            ResultsTitleTextBlock.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

            RootGrid.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            var rootGridHeight = RootGrid.DesiredSize.Height;

            var newWindowHeight = rootGridHeight;
            if (SharpEngineSceneView != null)
                newWindowHeight *= SharpEngineSceneView.SceneView.DpiScaleY;

            if (newWindowHeight < MinWindowHeight)
                newWindowHeight = MinWindowHeight;


            if (!_isInitialSizeSet)
            {
                int width = InitialWindowWidth;

                if (SharpEngineSceneView != null)
                    width = (int)(width * SharpEngineSceneView.SceneView.DpiScaleX);

                WinUiUtils.SetWindowClientSize(this, width, (int)newWindowHeight);
                _isInitialSizeSet = true;
            }
            else
            {
                var windowSize = WinUiUtils.GetWindowSize(this);
                if (Math.Abs(windowSize.Height - (int)newWindowHeight) > 10)
                    WinUiUtils.SetWindowClientHeight(this, (int)newWindowHeight);
            }
        }

        private void UpdateCameraInfo()
        {
            string cameraInfo;
            try
            {
                if (SharpEngineSceneView?.SceneView != null)
                    cameraInfo = SharpEngineSceneView.SceneView.GetCameraInfo(showMatrices: true);
                else
                    cameraInfo = "No SceneView";
            }
            catch (Exception ex)
            {
                cameraInfo = "Error getting camera info: " + ex.Message;
            }

            StatisticsTextBlock.Text = cameraInfo;

            UpdateWindowSize();
        }
        
        private void ShowInfoText(string infoText)
        {
            System.IO.File.WriteAllText(DumpFileName, infoText);
            StartProcess(DumpFileName);
        }

        private static void StartProcess(string fileName)
        {
            try
            {
                // For CORE3 project we need to set UseShellExecute to true,
                // otherwise a "The specified executable is not a valid application for this OS platform" exception is thrown.
                System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
            }
            catch
            {
                // pass
            }
        }
        
        private void CaptureButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null || !SharpEngineSceneView.SceneView.BackBuffersInitialized)
                return;

            bool isRenderDocAvailable = SharpEngineSceneView.SceneView.CaptureNextFrame();

            if (!isRenderDocAvailable)
            {
                System.Diagnostics.Debug.WriteLine("Start the application from RenderDoc to be able to capture a frame.");
                return;
            }

            SharpEngineSceneView.RenderScene(forceRender: true, forceUpdate: false);
        }
        
        private void ShowStatisticsOrCameraInfo(bool showStatistics)
        {
            _showRenderingStatistics = showStatistics;

            if (showStatistics)
            {
                ResultsTitleTextBlock.Text = "Rendering statistics:";
                StatisticsTextBlock.ClearValue(TextBlock.FontFamilyProperty);
                StatisticsTextBlock.ClearValue(TextBlock.FontSizeProperty);

                UpdateStatistics();
            }
            else
            {
                ResultsTitleTextBlock.Text = "Camera info:";
                StatisticsTextBlock.FontFamily = new FontFamily("Courier New");
                StatisticsTextBlock.FontSize = 11;

                UpdateCameraInfo();
            }
        }

        private void OnlineReferenceHelpMenuItem_OnTapped(object sender, RoutedEventArgs e)
        {
            StartProcess("https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm");
        }

        private void LogWarningsPanel_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_logMessagesWindow != null)
                return;

            _logMessagesWindow = new LogMessagesWindow();

            _logMessagesWindow.LogMessages = _commonDiagnostics.LogMessages;

            _logMessagesWindow.OnLogMessagesClearedAction = () => _commonDiagnostics.ClearLogMessages();

            _logMessagesWindow.Closed += delegate(object o, WindowEventArgs args)
            {
                _logMessagesWindow = null;
            };

            _logMessagesWindow.Activate();
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            MenuCanvas.ContextFlyout.ShowAt(MenuCanvas, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Auto});
        }

        private void ShowRenderingStatisticsRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            ShowCameraInfoRadioButton.IsChecked = false;
            ShowStatisticsOrCameraInfo(showStatistics: true);
        }

        private void ShowCameraInfoRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            ShowRenderingStatisticsRadioButton.IsChecked = false;
            ShowStatisticsOrCameraInfo(showStatistics: false);
        }

        private void FullLoggingCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            if (FullLoggingCheckBox.IsChecked)
            {
                Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.All;
                Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;
            }
            else
            {
                Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
            }
        }
    }
}
