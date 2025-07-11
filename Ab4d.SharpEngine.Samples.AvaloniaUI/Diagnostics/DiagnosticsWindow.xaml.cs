﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Diagnostics
{
    /// <summary>
    /// Interaction logic for DiagnosticsWindow.xaml
    /// </summary>
    public partial class DiagnosticsWindow : Window
    {
        public const double InitialWindowWidth = 310;

        private DispatcherTimer? _updateStatisticsTimer;

        private CommonDiagnostics _commonDiagnostics;

        public string DumpFileName { get; set; }

        private DateTime _lastStatisticsUpdate;
        private bool _showRenderingStatistics = true;

        private bool _isChangingRadioButtons;

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

            var bitmapIO = new SkiaSharpBitmapIO();
            _commonDiagnostics = new CommonDiagnostics(bitmapIO);

            _commonDiagnostics.WarningsCountChangedAction = (warningsCount) =>
            {
                WarningsCountTextBlock.Text = warningsCount.ToString();
                LogWarningsPanel.IsVisible = warningsCount > 0;
            };

            _commonDiagnostics.NewLogMessageAddedAction = () => _logMessagesWindow?.UpdateLogMessages();

            _commonDiagnostics.DeviceInfoChangedAction = () => DeviceInfoTextBlock.Text = _commonDiagnostics.GetDeviceInfo();

            _commonDiagnostics.OnSceneRenderedAction = UpdateStatistics;

            _commonDiagnostics.WarningsCountChangedAction = UpdateWarningsCount;

            //_commonDiagnostics.ShowMessageBoxAction = (message) => MessageBox.Show(message);


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
                ToolTip.SetTip(ExportToGltfMenuItem, "Add reference to the Ab4d.SharpEngine.glTF to enabled export to glTF");
                
                // The following is available in Avalonia v11.1.0-rc1
                //ToolTip.SetShowOnDisabled(ExportToGltfMenuItem, true);
            }


            string dumpFolder;
            if (System.IO.Directory.Exists(@"C:\temp"))
                dumpFolder = @"C:\temp\";
            else
                dumpFolder = System.IO.Path.GetTempPath();

            DumpFileName = System.IO.Path.Combine(dumpFolder, "SharpEngineDump.txt");


            SharpEngineInfoTextBlock.Text = _commonDiagnostics.GetSharpEngineInfoText();

            // When the window is shown start showing statistics
            Dispatcher.UIThread.Post(() => StartShowingStatistics(), DispatcherPriority.Background);

            this.Loaded += OnLoaded;
            this.Closing += OnClosing;
        }

        private void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
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
                ResultsTitleTextBlock.IsVisible = true;
                ResultsTitleTextBlock.Text = _showRenderingStatistics ? "Rendering statistics:" : "Camera info:";
            }
            else
            {
                ResultsTitleTextBlock.IsVisible = false;
            }

            AnalyerResultsTextBox.IsVisible = false;
            StatisticsTextBlock.IsVisible = true;

            _commonDiagnostics.StartShowingStatistics();
        }

        private void EndShowingStatistics()
        {
            StatisticsTextBlock.IsVisible = false;
            ResultsTitleTextBlock.IsVisible = false;

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
                ResultsTitleTextBlock.IsVisible = true;
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
                CaptureButton.IsVisible = true;
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
            _commonDiagnostics.ToggleCameraRotation();
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
        
        private void ShowFullSceneDumpMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;

            var dumpText = _commonDiagnostics.GetFullSceneDumpString();

            // Start with empty DumpFile 
            System.IO.File.WriteAllText(DumpFileName, dumpText);
            StartProcess(DumpFileName);
        }
        
        private async void ExportToGltfMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_commonDiagnostics.IsGltfExporterAvailable || SharpEngineSceneView == null)
                return;

            // Run file selection dialog
            var topLevel = TopLevel.GetTopLevel(this);
            Debug.Assert(topLevel != null, nameof(topLevel) + " != null");
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Scene to glTF File",
                SuggestedFileName = "SharpEngineScene.gltf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("glTF file with embedded data and images")
                    {
                        Patterns = new[] { "*.gltf" },
                        MimeTypes = new[] { "model/gltf+json" }
                    },
                    new FilePickerFileType("glTF binary file with embedded data and images")
                    {
                        Patterns = new[] { "*.glb" },
                        MimeTypes = new[] { "model/gltf-binary" }
                    },
                }
            });

            var fileName = file?.TryGetLocalPath();
            if (fileName == null)
                return;

            _commonDiagnostics.ExportSceneToGltf(SharpEngineSceneView.Scene, SharpEngineSceneView.SceneView, fileName);
        }
                
        private async void ExportToObjMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;

            
            // Run file selection dialog
            var topLevel = TopLevel.GetTopLevel(this);
            Debug.Assert(topLevel != null, nameof(topLevel) + " != null");
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Scene to obj file",
                SuggestedFileName = "SharpEngineScene.obj",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Obj file")
                    {
                        Patterns = new[] { "*.obj" },
                        MimeTypes = new[] { "model/obj" }
                    },
                }
            });

            var fileName = file?.TryGetLocalPath();
            if (fileName == null)
                return;

            _commonDiagnostics.ExportSceneToObj(SharpEngineSceneView.Scene, fileName);
        }
        
        private void RenderToBitmapMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _commonDiagnostics.SaveRenderedSceneToDesktop();
        }
        
        private void DumpCameraDetailsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInfoText(_commonDiagnostics.GetCameraDetailsDumpString());
        }

        private void GarbageCollectMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _commonDiagnostics.GarbageCollect();
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
            ActionsRootMenuItem.Close();
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
            ShowStatisticsButton.IsVisible = showShowStatisticsButton;

            ButtonsPanel.IsVisible = showStopPerformanceAnalyzerButton || showShowStatisticsButton;
        }

        private void UpdateWarningsCount(int warningsCount)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                WarningsCountTextBlock.Text = warningsCount.ToString();
                LogWarningsPanel.IsVisible = warningsCount > 0;
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    WarningsCountTextBlock.Text = warningsCount.ToString();
                    LogWarningsPanel.IsVisible = warningsCount > 0;
                });
            }
        }

        private void UpdateStatistics()
        {
            if (SharpEngineSceneView == null || SharpEngineSceneView.SceneView.Statistics == null)
            {
                StatisticsTextBlock.IsVisible = false;
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

            ResultsTitleTextBlock.IsVisible = true;
            StatisticsTextBlock.IsVisible = true;
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
                System.Diagnostics.Debug.WriteLine("Start the application from RenderDoc to be able to capture a frame.");
                return;
            }

            SharpEngineSceneView.RenderScene(forceRender: true, forceUpdate: false);
        }

        private void ShowStatisticsOrCameraInfo(bool showStatistics)
        {
            if (!this.IsLoaded)
                return;

            _showRenderingStatistics = showStatistics;

            if (showStatistics)
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
            ActionsRootMenuItem.Close();
        }

        private void OnlineReferenceHelpMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            StartProcess("https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm");
        }

        private void LogWarningsPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_logMessagesWindow != null)
                return;

            _logMessagesWindow = new LogMessagesWindow();

            _logMessagesWindow.LogMessages = _commonDiagnostics.LogMessages;

            _logMessagesWindow.OnLogMessagesClearedAction = () => _commonDiagnostics.ClearLogMessages();

            _logMessagesWindow.Closing += delegate(object? o, WindowClosingEventArgs args)
            {
                _logMessagesWindow = null;
            };

            _logMessagesWindow.Show();
        }
        
        private void ShowRenderingStatisticsRadioButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_isChangingRadioButtons)
                return;

            _isChangingRadioButtons = true; // This is needed to prevent infinite recursion because we need to manually set the IsChecked for both RadioButtons

            ShowCameraInfoRadioButton.IsChecked = false;
            ShowRenderingStatisticsRadioButton.IsChecked = true;
            ShowStatisticsOrCameraInfo(showStatistics: true);

            _isChangingRadioButtons = false;
        }

        private void ShowCameraInfoRadioButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_isChangingRadioButtons)
                return;

            _isChangingRadioButtons = true; // This is needed to prevent infinite recursion because we need to manually set the IsChecked for both RadioButtons

            ShowRenderingStatisticsRadioButton.IsChecked = false;
            ShowCameraInfoRadioButton.IsChecked = true;
            ShowStatisticsOrCameraInfo(showStatistics: false);

            _isChangingRadioButtons = false;
        }
    }
}
