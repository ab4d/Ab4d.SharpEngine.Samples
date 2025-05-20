using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Diagnostics;
using Ab4d.SharpEngine.Wpf;

namespace Ab4d.SharpEngine.Samples.Wpf.Diagnostics
{
    /// <summary>
    /// Interaction logic for DiagnosticsWindow.xaml
    /// </summary>
    public partial class DiagnosticsWindow : Window
    {
        public const double InitialWindowWidth = 330;

        private DispatcherTimer? _updateStatisticsTimer;

        private CommonDiagnostics _commonDiagnostics;

        public string DumpFileName { get; set; }

        private DateTime _lastStatisticsUpdate;
        private bool _showRenderingStatistics = true;

        private LogMessagesWindow? _logMessagesWindow;
        private WpfBitmapIO _bitmapIO;

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

            _bitmapIO = new WpfBitmapIO();

            _commonDiagnostics = new CommonDiagnostics(_bitmapIO);

            _commonDiagnostics.NewLogMessageAddedAction = () => _logMessagesWindow?.UpdateLogMessages();

            _commonDiagnostics.DeviceInfoChangedAction = () => DeviceInfoTextBlock.Text = _commonDiagnostics.GetDeviceInfo();

            _commonDiagnostics.OnSceneRenderedAction = UpdateStatistics;

            _commonDiagnostics.WarningsCountChangedAction = UpdateWarningsCount;

            _commonDiagnostics.ShowMessageBoxAction = (message) => MessageBox.Show(message);


            this.Width = InitialWindowWidth;

            // Start as always on top Window
            this.Topmost = true;
            AlwaysOnTopCheckBox.IsChecked = true;

            // Only show "Full Logging" CheckBox when SharpEngine is compiled with full logging
#pragma warning disable CS0162 // Unreachable code detected
            if (Ab4d.SharpEngine.Utilities.Log.MinUsedLogLevel != LogLevels.Trace)
                ActionsRootMenuItem.Items.Remove(FullLoggingCheckBox);
#pragma warning restore CS0162 // Unreachable code detected

            if (!_commonDiagnostics.IsGltfExporterAvailable)
            {
                ExportToGltfMenuItem.IsEnabled = false;
                ExportToGltfMenuItem.ToolTip = "Add reference to the Ab4d.SharpEngine.glTF to enabled export to glTF";
                ToolTipService.SetShowOnDisabled(ExportToGltfMenuItem, true);
            }

            string dumpFolder;
            if (System.IO.Directory.Exists(@"C:\temp"))
                dumpFolder = @"C:\temp\";
            else
                dumpFolder = System.IO.Path.GetTempPath();

            DumpFileName = System.IO.Path.Combine(dumpFolder, "SharpEngineDump.txt");


            SharpEngineInfoTextBlock.Text = _commonDiagnostics.GetSharpEngineInfoText();

            // When the window is shown start showing statistics
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(StartShowingStatistics));

            this.Loaded += OnLoaded;
            this.Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UpdateEnabledMenuItems();
        }

        private void OnClosing(object? sender, CancelEventArgs cancelEventArgs)
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
        }

        private void UpdateEnabledMenuItems()
        {
            // On each new scene reset the StartStopCameraRotationMenuItem text
            StartStopCameraRotationMenuItem.Header = "Toggle camera rotation";

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

            AnalyerResultsTextBox.Visibility = Visibility.Collapsed;
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
            {
                CaptureButton.Visibility = Visibility.Visible;
            }
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

        private void CheckToUpdateStatisticsOrCameraInfo(object? sender, EventArgs eventArgs)
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

        private void StartStopCameraRotationMenuItem_OnClick(object sender, RoutedEventArgs args)
        {
            bool isCameraRotating = _commonDiagnostics.ToggleCameraRotation();

            if (isCameraRotating)
                StartStopCameraRotationMenuItem.Header = "Stop camera rotation";
            else
                StartStopCameraRotationMenuItem.Header = "Start camera rotation";
        }
        
        private void DumpSceneNodesMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetSceneNodesDumpString());
        }

        private void DumpRenderingLayersMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetRenderingLayersDumpString());
        }

        private void DumpRenderingStepsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetRenderingStepsDumpString());
        }

        private void DumpUsedMaterialsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetUsedMaterialsDumpString());
        }

        private void DumpMemoryMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetMemoryDumpString());
        }
        
        private void DumpResourcesMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetResourcesDumpString(groupByType: false));
        }
        
        private void DumpResourcesGroupByTypeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetResourcesDumpString(groupByType: true));
        }
        
        private void DumpResourcesForDelayedDisposalMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetResourcesForDelayedDisposalDumpString());
        }

        private void DumpEngineSettingsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetEngineSettingsDump());
        }

        private void DumpSystemInfoMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetSystemInfoDumpString());
        }
        
        private void ExportToGltfMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_commonDiagnostics.IsGltfExporterAvailable || SharpEngineSceneView == null)
                return;


            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.AddExtension = false;
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.OverwritePrompt = false;
            saveFileDialog.ValidateNames = false;

            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            saveFileDialog.FileName = "SharpEngineScene.gltf";
            saveFileDialog.DefaultExt = "gltf";
            saveFileDialog.Filter = "glTF file with embedded data and images (.gltf)|*.gltf|glTF binary file with embedded data and images (.glb)|*.glb";
            saveFileDialog.Title = "Export Scene to glTF File";

            if (saveFileDialog.ShowDialog() ?? false)
            {
                try
                {
                    _commonDiagnostics.ExportSceneToGltf(SharpEngineSceneView.Scene, SharpEngineSceneView.SceneView, saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving gltf file:\r\n" + ex.Message);
                }
            }
        }
                
        private void ExportToObjMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;


            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.AddExtension = false;
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.OverwritePrompt = false;
            saveFileDialog.ValidateNames = false;

            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            saveFileDialog.FileName = "SharpEngineScene.obj";
            saveFileDialog.DefaultExt = "obj";
            saveFileDialog.Filter = "Obj file (.obj)|*.obj";
            saveFileDialog.Title = "Export Scene to obj file";

            if (saveFileDialog.ShowDialog() ?? false)
            {
                try
                {
                    _commonDiagnostics.ExportSceneToObj(SharpEngineSceneView.Scene, saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving obj file:\r\n" + ex.Message);
                }
            }            
        }
        
        private void RenderToBitmapMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _commonDiagnostics.SaveRenderedSceneToDesktop();
        }

        private void ShowFullSceneDumpMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;

            var dumpText = _commonDiagnostics.GetFullSceneDumpString();

            // Start with empty DumpFile 
            System.IO.File.WriteAllText(DumpFileName, dumpText);
            StartProcess(DumpFileName);
        }
        
        private void DumpCameraDetailsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetCameraDetailsDumpString());
        }

        private void GarbageCollectMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _commonDiagnostics.GarbageCollect();
        }
        
        private void LogWarningsPanel_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_logMessagesWindow != null)
                return;

            _logMessagesWindow = new LogMessagesWindow();

            _logMessagesWindow.LogMessages = _commonDiagnostics.LogMessages;

            _logMessagesWindow.OnLogMessagesClearedAction = () => _commonDiagnostics.ClearLogMessages();

            _logMessagesWindow.Closing += delegate(object? o, CancelEventArgs args)
            {
                _logMessagesWindow = null;
            };

            _logMessagesWindow.Show();
        }

        private void ShowStatisticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartShowingStatistics();
            ShowButtons(showStopPerformanceAnalyzerButton: false, showShowStatisticsButton: false);
        }

        private void AlwaysOnTopCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            this.Topmost = (AlwaysOnTopCheckBox.IsChecked ?? false);

            // Close the menu
            ActionsRootMenuItem.IsSubmenuOpen = false;
        }
        
        private void FullLoggingCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (FullLoggingCheckBox.IsChecked ?? false)
            {
                Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.All;
                Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;
            }
            else
            {
                Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
            }
        }

        private void ShowButtons(bool showStopPerformanceAnalyzerButton, bool showShowStatisticsButton)
        {
            //StopPerformanceAnalyzerButton.Visibility = showStopPerformanceAnalyzerButton ? Visibility.Visible : Visibility.Collapsed;
            ShowStatisticsButton.Visibility          = showShowStatisticsButton ? Visibility.Visible : Visibility.Collapsed;

            ButtonsPanel.Visibility = showStopPerformanceAnalyzerButton || showShowStatisticsButton ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateWarningsCount(int warningsCount)
        {
            if (this.Dispatcher.CheckAccess())
            {
                WarningsCountTextBlock.Text = warningsCount.ToString();
                LogWarningsPanel.Visibility = warningsCount > 0 ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                this.Dispatcher.InvokeAsync(() =>
                {
                    WarningsCountTextBlock.Text = warningsCount.ToString();
                    LogWarningsPanel.Visibility = warningsCount > 0 ? Visibility.Visible : Visibility.Hidden;
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
                MessageBox.Show("Start the application from RenderDoc to be able to capture frames.");
                return;
            }

            SharpEngineSceneView.RenderScene(forceRender: true, forceUpdate: false);
        }
        
        private void StatisticsTypeRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // Store value in local field so we do not need to check the value ShowRenderingStatisticsRadioButton.IsChecked on each update (this is quite slow)
            _showRenderingStatistics = ShowRenderingStatisticsRadioButton.IsChecked ?? false;

            if (_showRenderingStatistics)
            {
                ResultsTitleTextBlock.Text = "Rendering statistics:";
                StatisticsTextBlock.ClearValue(FontFamilyProperty);
                StatisticsTextBlock.ClearValue(FontSizeProperty);

                UpdateStatistics();
            }
            else
            {
                ResultsTitleTextBlock.Text = "Camera info:";
                StatisticsTextBlock.FontFamily = new FontFamily("Courier New");
                StatisticsTextBlock.FontSize = 11;

                UpdateCameraInfo();
            }

            // Close the menu
            ActionsRootMenuItem.IsSubmenuOpen = false;
        }

        private void OnlineReferenceHelpMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            StartProcess("https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm");
        }
    }
}
