using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Diagnostics;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.Samples.Wpf.Diagnostics;
using Ab4d.SharpEngine.Samples.Wpf.Settings;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Uncomment the _startupPage declaration to always start the samples with the specified page
        private string? _startupPage = null;

        // To enable Vulkan's standard validation, set EnableStandardValidation to true.
        // Also, you need to install Vulkan SDK from https://vulkan.lunarg.com
        // Using Vulkan validation may reduce the performance of rendering.
        public static bool EnableStandardValidation = false;
        
        public const bool PreventShowingErrorReportWhenDebuggerIsAttached = true; // Set to false to show error report dialog even when debugger is attached

        public static Action<Ab4d.SharpEngine.Wpf.SharpEngineSceneView>? ConfigureSharpEngineSceneViewAction;
        
        private ISharpEngineSceneView? _currentSharpEngineSceneView;
        private bool _isPresentationTypeChangedSubscribed;
        private bool _isSceneViewInitializedSubscribed;
        private bool _isSceneViewSizeChangedSubscribed;

        private CommonWpfSamplePage? _commonWpfSamplePage;
        private CommonTitlePage? _commonTitlePage;

        private BitmapImage? _diagnosticsDisabledImage;
        private BitmapImage? _diagnosticsEnabledImage;

        private Dictionary<Assembly, string[]>? _assemblyEmbeddedResources;

        private DiagnosticsWindow? _diagnosticsWindow;
        private bool _automaticallyOpenDiagnosticsWindow;

        private string? _currentSampleLocation;
        private CommonSample? _currentCommonSample;

        private SettingsWindow.AdvancedSharpEngineSettings? _advancedSettings;
        private RandomSamplesRunner? _randomSamplesRunner;
        private Button? _testRunnerButton;

        
        public MainWindow()
        {
            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

            // The following is a sample global exception handler that can be used 
            // to get system info (with details about graphics card and drivers)
            // in case of exception in SharpEngine.
            // You can use similar code to improve your error reporting data.
            AppDomain.CurrentDomain.UnhandledException += ReportUnhandledException;

            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // By default, enable logging of warnings and errors.
            // In case of problems please send the log text with the description of the problem to AB4D company
            Utilities.Log.LogLevel = LogLevels.Warn;
            Utilities.Log.IsLoggingToDebugOutput = true;


            InitializeComponent();

            //SetupTestRunnerButton();

            // Snap WPF objects to device pixels. This shows sharper image because the rendered 3D scene is not linearly scaled by WPF.
            // See comment in the constructor of QuickStart/AntiAliasingSample.xaml.cs for more info.
            this.UseLayoutRounding = true;

            DisableDiagnosticsButton();

            WpfSamplesContext.Current.CurrentSharpEngineSceneViewChanged += OnCurrentSharpEngineSceneViewChanged;

            this.Loaded += delegate (object? sender, RoutedEventArgs args)
            {
                LoadSamples();
            };

            this.Unloaded += (sender, args) => CloseDiagnosticsWindow();

            ContentFrame.LoadCompleted += (o, args) =>
            {
                // When content of ContentFrame is changed, we try to find the SharpEngineSceneView control
                // that is defined by the newly shown content.

                Ab4d.SharpEngine.Wpf.SharpEngineSceneView? sharpEngineSceneView;
                if (_commonWpfSamplePage != null && ReferenceEquals(_commonWpfSamplePage.CurrentCommonSample, _currentCommonSample))
                    sharpEngineSceneView = _commonWpfSamplePage.MainSceneView;
                else
                    sharpEngineSceneView = FindSharpEngineSceneView(ContentFrame.Content);

                WpfSamplesContext.Current.RegisterCurrentSharpEngineSceneView(sharpEngineSceneView);
            };


            // SelectionChanged event handler is used to start the samples with the page set with _startupPage field.
            // SelectionChanged is used because SelectItem cannot be set from this.Loaded event.
            SampleList.SelectionChanged += (sender, args) =>
            {
                // The following can be executed only after the SampleList items are populated
                if (_startupPage != null)
                {
                    string savedStartupPage = _startupPage;
                    _startupPage = null;

                    SelectItem(savedStartupPage);
                    return;
                }

                ShowSelectedSample(args);
            };
        }

        private void LoadSamples()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples.xml");
            SampleList.ItemsSource = CommonSample.LoadSamples(fileName,
                                                              uiFramework: "Wpf",
                                                              errorMessage => MessageBox.Show(errorMessage, "", MessageBoxButton.OK, MessageBoxImage.Exclamation));
        }

        private void ShowSelectedSample(SelectionChangedEventArgs args)
        {
            if (args.AddedItems == null || args.AddedItems.Count == 0 || args.AddedItems[0] is not XmlElement xmlElement)
                return;

            if (_currentCommonSample != null)
            {
                _currentCommonSample.Dispose();
                _currentCommonSample = null;
            }
         
            
            var sampleLocation = xmlElement.GetAttribute("Location");

            _currentSampleLocation = sampleLocation;
            
            
            if (string.IsNullOrEmpty(sampleLocation))
                return; // probably user selected a separator (this can be selected by using keyboard)


            if (sampleLocation.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var markdownText = GetMarkdownText(sampleLocation);

                if (markdownText != null)
                {
                    _commonTitlePage ??= new CommonTitlePage();
                    _commonTitlePage.MarkdownText = markdownText;

                    ContentFrame.Navigate(_commonTitlePage);

                    _currentCommonSample = null;
                    return;
                }
            }


            var createdSample = CommonSample.CreateSampleObject(uiFramework: "Wpf", sampleLocation, WpfSamplesContext.Current, errorMessage => MessageBox.Show(errorMessage, "", MessageBoxButton.OK, MessageBoxImage.Exclamation));

            if (createdSample is CommonSample createdCommonSample)
            {
                if (_commonWpfSamplePage == null)
                {
                    _commonWpfSamplePage = new CommonWpfSamplePage();
                    WpfSamplesContext.Current.RegisterCurrentSharpEngineSceneView(_commonWpfSamplePage.MainSceneView);
                }

                if (_currentCommonSample == null)
                    ContentFrame.Navigate(_commonWpfSamplePage);

                _commonWpfSamplePage.CurrentCommonSample = createdCommonSample;
                _currentCommonSample = createdCommonSample;
            }
            else if (createdSample is FrameworkElement createdFrameworkElement)
            {
                ContentFrame.Navigate(createdFrameworkElement);
                _currentCommonSample = null;
            }
            else
            {
                _currentCommonSample = null;
            }
        }

        private string? GetMarkdownText(string location)
        {
            var markdownText = GetMarkdownText(this.GetType().Assembly, location);

            if (markdownText == null)
                markdownText = GetMarkdownText(typeof(CommonSample).Assembly, location);

            return markdownText;
        }

        private string? GetMarkdownText(System.Reflection.Assembly assembly, string location)
        {
            _assemblyEmbeddedResources ??= new Dictionary<Assembly, string[]>();

            if (!_assemblyEmbeddedResources.TryGetValue(assembly, out var embeddedResourceNames))
            {
                embeddedResourceNames = assembly.GetManifestResourceNames();
                _assemblyEmbeddedResources.Add(assembly, embeddedResourceNames);
            }

            foreach (var embeddedResource in embeddedResourceNames)
            {
                if (embeddedResource.EndsWith(location))
                {
                    var manifestResourceStream = assembly.GetManifestResourceStream(embeddedResource);
                    if (manifestResourceStream != null)
                    {
                        string markdownText;

                        using (var streamReader = new StreamReader(manifestResourceStream))
                            markdownText = streamReader.ReadToEnd();

                        return markdownText;
                    }
                }
            }

            return null;
        }

        private void OnCurrentSharpEngineSceneViewChanged(object? sender, EventArgs e)
        {
            if (_currentSharpEngineSceneView != null)
            {
                if (_isSceneViewInitializedSubscribed)
                {
                    _currentSharpEngineSceneView.SceneViewInitialized -= OnSceneViewInitialized;
                    _isSceneViewInitializedSubscribed = false;
                }

                if (_isSceneViewSizeChangedSubscribed)
                {
                    _currentSharpEngineSceneView.ViewSizeChanged -= OnSceneViewOnViewSizeChanged;
                    _isSceneViewSizeChangedSubscribed = false;
                }

                if (_isPresentationTypeChangedSubscribed)
                {
                    _currentSharpEngineSceneView.PresentationTypeChanged -= OnPresentationTypeChanged;
                    _isPresentationTypeChangedSubscribed = false;
                }

                _currentSharpEngineSceneView = null;
            }

            UpdateGraphicsCardInfo();
        }

        private void UpdateGraphicsCardInfo()
        {
            var sharpEngineSceneView = WpfSamplesContext.Current.CurrentSharpEngineSceneView;

            if (sharpEngineSceneView != null && !_isSceneViewSizeChangedSubscribed)
            {
                sharpEngineSceneView.ViewSizeChanged += OnSceneViewOnViewSizeChanged;
                _isSceneViewSizeChangedSubscribed = true;
            }

            if (sharpEngineSceneView == null || !sharpEngineSceneView.SceneView.BackBuffersInitialized)
            {
                DisableDiagnosticsButton();

                if (_diagnosticsWindow != null)
                    _automaticallyOpenDiagnosticsWindow = true; // This will reopen the diagnostics window

                CloseDiagnosticsWindow();

                SelectedGraphicInfoTextBlock.Text = null;
                ViewSizeInfoTextBlock.Text = null;
                UsedGpuTextBlock.Visibility = Visibility.Hidden;

                if (sharpEngineSceneView != null)
                {
                    if (!_isSceneViewInitializedSubscribed)
                    {
                        sharpEngineSceneView.SceneViewInitialized += OnSceneViewInitialized;
                        _isSceneViewInitializedSubscribed = true;
                    }

                    _currentSharpEngineSceneView = sharpEngineSceneView;
                }

                return;
            }

            UpdateSelectedGraphicInfo(sharpEngineSceneView);
            UpdateViewSizeInfo(sharpEngineSceneView);
            ViewSizeInfoTextBlock.ToolTip = null;

            if (!_isPresentationTypeChangedSubscribed)
            {
                sharpEngineSceneView.PresentationTypeChanged += OnPresentationTypeChanged;
                _isPresentationTypeChangedSubscribed = true;
            }

            _currentSharpEngineSceneView = sharpEngineSceneView;

            EnableDiagnosticsButton();

            if (_diagnosticsWindow != null)
            {
                _diagnosticsWindow.SharpEngineSceneView = sharpEngineSceneView;
            }
            else if (_automaticallyOpenDiagnosticsWindow)
            {
                OpenDiagnosticsWindow();
                _automaticallyOpenDiagnosticsWindow = false;
            }
        }

        private void UpdateSelectedGraphicInfo(ISharpEngineSceneView sharpEngineSceneView)
        {
            if (sharpEngineSceneView.GpuDevice != null)
            {
                SelectedGraphicInfoTextBlock.Text = sharpEngineSceneView.GpuDevice.GpuName;
                UsedGpuTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                SelectedGraphicInfoTextBlock.Text = "";
                UsedGpuTextBlock.Visibility = Visibility.Hidden;
            }

            SelectedGraphicInfoTextBlock.ToolTip = null;
        }

        private void UpdateViewSizeInfo(ISharpEngineSceneView sharpEngineSceneView)
        {
            if (sharpEngineSceneView.GpuDevice != null)
            {
                var sceneView = sharpEngineSceneView.SceneView;

                string viewInfo;
                if (sceneView.BackBuffersInitialized)
                {
                    viewInfo = $"{sceneView.Width} x {sceneView.Height}";

                    if (sceneView.MultisampleCount > 1)
                        viewInfo += $" {sceneView.MultisampleCount}xMSAA";

                    var supersamplingCount = sceneView.SupersamplingCount; // number of pixels used for one final pixel
                    if (supersamplingCount > 1)
                        viewInfo += string.Format(" {0:0.#}xSSAA", supersamplingCount);

                    viewInfo += $" ({sharpEngineSceneView.PresentationType})";
                }
                else
                {
                    viewInfo = "";
                }

                ViewSizeInfoTextBlock.Text = viewInfo;
            }
            else
            {
                ViewSizeInfoTextBlock.Text = "";
            }
        }

        private void OnSceneViewOnViewSizeChanged(object sender, ViewSizeChangedEventArgs e)
        {
            var sharpEngineSceneView = WpfSamplesContext.Current.CurrentSharpEngineSceneView;

            if (sharpEngineSceneView != null)
                UpdateViewSizeInfo(sharpEngineSceneView);
        }

        private void OnPresentationTypeChanged(object? sender, string? reason)
        {
            UpdateGraphicsCardInfo();

            if (reason != null)
                ViewSizeInfoTextBlock.ToolTip = reason;
            else
                ViewSizeInfoTextBlock.ToolTip = null;
        }

        private void OnSceneViewInitialized(object? sender, EventArgs e)
        {
            if (_currentSharpEngineSceneView != null && _isSceneViewInitializedSubscribed)
            {
                _currentSharpEngineSceneView.SceneViewInitialized -= OnSceneViewInitialized;
                _isSceneViewInitializedSubscribed = true;
            }

            UpdateGraphicsCardInfo();
        }


        private void GraphicsSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }

        private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDiagnosticsWindow();
        }

        private void OpenSettingsWindow()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            
            settingsWindow.AdvancedSettings = _advancedSettings;
            settingsWindow.ShowTestRunner = _testRunnerButton != null;
            settingsWindow.IsStandardValidationEnabled = EnableStandardValidation;

            settingsWindow.ShowDialog();

            if (settingsWindow.IsChanged)
            {
                if (_currentSharpEngineSceneView != null)
                {
                    _currentSharpEngineSceneView.MultisampleCount = GlobalSharpEngineSettings.MultisampleCount;
                    _currentSharpEngineSceneView.SupersamplingCount = GlobalSharpEngineSettings.SupersamplingCount;

                    // We need to call RenderScene on SharpEngineSceneView and not only on SceneView, otherwise in SharedTexture mode, the shared texture is not updated.
                    _currentSharpEngineSceneView.RenderScene(forceRender: true, forceUpdate: false);

                    UpdateViewSizeInfo(_currentSharpEngineSceneView);
                }
            }
            
            
            if (settingsWindow.ShowTestRunner && _testRunnerButton == null)
            {
                SetupTestRunnerButton();
            }
            else if (!settingsWindow.ShowTestRunner && _testRunnerButton != null)
            {
                ButtonsPanel.Children.Remove(_testRunnerButton);
                _testRunnerButton = null;
                
                GraphicsSettingsButton.Margin = new Thickness(0, 0, 20, 0);
                
                if (_randomSamplesRunner != null && _randomSamplesRunner.IsRunning)
                    _randomSamplesRunner.Stop();
            }
            
            var newAdvancedSettings = settingsWindow.AdvancedSettings;

            bool isAdvancedSettingsChanged;
            
            if (newAdvancedSettings != null)
            {
                if (_advancedSettings != null)
                {
                    isAdvancedSettingsChanged = newAdvancedSettings.UseWritableBitmap != _advancedSettings.UseWritableBitmap ||
                                                newAdvancedSettings.DisableBackgroundUpload != _advancedSettings.DisableBackgroundUpload ||
                                                newAdvancedSettings.DisableMaterialSorting != _advancedSettings.DisableMaterialSorting ||
                                                newAdvancedSettings.DisableTransparencySorting != _advancedSettings.DisableTransparencySorting ||
                                                newAdvancedSettings.PreserveBackBuffersWhenHidden != _advancedSettings.PreserveBackBuffersWhenHidden ||
                                                newAdvancedSettings.AllowDirectTextureSharingForIntelGpu != _advancedSettings.AllowDirectTextureSharingForIntelGpu ||
                                                newAdvancedSettings.IsUsingSharedTextureForIntegratedIntelGpu != _advancedSettings.IsUsingSharedTextureForIntegratedIntelGpu;
                }
                else
                {
                    isAdvancedSettingsChanged = true;
                }
                
                // Is any settings enabled?
                if (newAdvancedSettings.UseWritableBitmap ||
                    newAdvancedSettings.DisableBackgroundUpload ||
                    newAdvancedSettings.DisableMaterialSorting ||
                    newAdvancedSettings.DisableTransparencySorting ||
                    newAdvancedSettings.PreserveBackBuffersWhenHidden ||
                    newAdvancedSettings.AllowDirectTextureSharingForIntelGpu ||
                    newAdvancedSettings.PreserveBackBuffersWhenHidden)
                {
                    _advancedSettings = newAdvancedSettings;
                }
                else
                {
                    _advancedSettings = null;
                }
                
                if (_advancedSettings != null)
                    MainWindow.ConfigureSharpEngineSceneViewAction = OnConfigureSharpEngineSceneViewAction;
                else
                    MainWindow.ConfigureSharpEngineSceneViewAction = null;
            }
            else
            {
                isAdvancedSettingsChanged = _advancedSettings != null;
                _advancedSettings = null;
            }

                        
            if (EnableStandardValidation != settingsWindow.IsStandardValidationEnabled)
            {
                // When EnableStandardValidation is changed, then we need to recreate the Vulkan instance.
                if (_currentSharpEngineSceneView != null)
                    _currentSharpEngineSceneView.Dispose();
                
                if (VulkanInstance.FirstVulkanInstance != null)
                    VulkanInstance.FirstVulkanInstance.Dispose();
                
                EnableStandardValidation = settingsWindow.IsStandardValidationEnabled;
                isAdvancedSettingsChanged = true;
            }
            
            if (isAdvancedSettingsChanged)
            {
                _commonWpfSamplePage = null; // Reset the CommonWpfSamplePage so it will be recreated with new settings
                ReloadCurrentSample();
            }
        }
        
        // This method is called when we need to apply advanced settings from SettingsWindow - it is called each time a new instance of SharpEngineSceneView is created
        private void OnConfigureSharpEngineSceneViewAction(Ab4d.SharpEngine.Wpf.SharpEngineSceneView sharpEngineSceneView)
        {
            if (_advancedSettings == null)
                return;
            
            sharpEngineSceneView.PresentationType = _advancedSettings.UseWritableBitmap ? PresentationTypes.WriteableBitmap : PresentationTypes.SharedTexture;

            sharpEngineSceneView.EnableBackgroundUpload                    = !_advancedSettings.DisableBackgroundUpload;
            sharpEngineSceneView.Scene.IsMaterialSortingEnabled            = !_advancedSettings.DisableMaterialSorting;
            sharpEngineSceneView.Scene.IsTransparencySortingEnabled        = !_advancedSettings.DisableTransparencySorting;
            sharpEngineSceneView.DisposeBackBuffersWhenHidden              = !_advancedSettings.PreserveBackBuffersWhenHidden;
            sharpEngineSceneView.AllowDirectTextureSharingForIntelGpu      = _advancedSettings.AllowDirectTextureSharingForIntelGpu;
            sharpEngineSceneView.IsUsingSharedTextureForIntegratedIntelGpu = _advancedSettings.IsUsingSharedTextureForIntegratedIntelGpu;
        }

        private void EnableDiagnosticsButton()
        {
            _diagnosticsEnabledImage ??= new BitmapImage(new Uri("pack://application:,,,/Ab4d.SharpEngine.Samples.Wpf;component/Resources/Diagnostics.png"));

            DiagnosticsImage.Source = _diagnosticsEnabledImage;
            DiagnosticsButton.IsEnabled = true;

            DiagnosticsButton.ToolTip = null;
        }

        private void DisableDiagnosticsButton()
        {
            _diagnosticsDisabledImage ??= new BitmapImage(new Uri("pack://application:,,,/Ab4d.SharpEngine.Samples.Wpf;component/Resources/Diagnostics-gray.png"));

            DiagnosticsImage.Source = _diagnosticsDisabledImage;
            DiagnosticsButton.IsEnabled = false;

            DiagnosticsButton.ToolTip = "Diagnostics button is disabled because there is no shown SharpEngineSceneView control.";
            ToolTipService.SetShowOnDisabled(DiagnosticsButton, true);
        }

        private void OpenDiagnosticsWindow()
        {
            var sharpEngineSceneView = WpfSamplesContext.Current.CurrentSharpEngineSceneView;

            if (sharpEngineSceneView == null)
            {
                CloseDiagnosticsWindow();
                return;
            }

            if (_diagnosticsWindow != null)
            {
                if (_diagnosticsWindow.WindowState == WindowState.Minimized)
                    _diagnosticsWindow.WindowState = WindowState.Normal;

                _diagnosticsWindow.Activate();
                return;
            }

            _diagnosticsWindow = new DiagnosticsWindow();
            _diagnosticsWindow.Closing += (sender, args) => _diagnosticsWindow = null;

            _diagnosticsWindow.SharpEngineSceneView = sharpEngineSceneView;

            // Position DiagnosticsWindow to the top-left corner of our window
            double left = this.Left + this.ActualWidth;
            double maxLeft = left + DiagnosticsWindow.InitialWindowWidth;

            if (maxLeft > SystemParameters.VirtualScreenWidth)
            {
                if (this.Left > DiagnosticsWindow.InitialWindowWidth)
                    left = this.Left - DiagnosticsWindow.InitialWindowWidth;
                else
                    left -= (maxLeft - SystemParameters.VirtualScreenWidth);
            }

            _diagnosticsWindow.Left = left;
            _diagnosticsWindow.Top = this.Top;

            _diagnosticsWindow.Show();
        }

        private void CloseDiagnosticsWindow()
        {
            if (_diagnosticsWindow != null)
            {
                try
                {
                    _diagnosticsWindow.Close();
                    _diagnosticsWindow = null;
                }
                catch
                {
                    // pass
                }
            }
        }

        private void SelectItem(string pageName)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                SampleList.SelectedItem = null;
                return;
            }

            var supportPageElement = SampleList.Items.OfType<System.Xml.XmlElement>()
                                                     .First(x => x.Attributes["Location"]?.Value == pageName);

            SampleList.SelectedItem = supportPageElement;

            SampleList.ScrollIntoView(supportPageElement);
        }

        // Searches the logical controls tree and returns the first instance of SharpEngineSceneView if found
        private Ab4d.SharpEngine.Wpf.SharpEngineSceneView? FindSharpEngineSceneView(object element)
        {
            var foundDViewportView = element as Ab4d.SharpEngine.Wpf.SharpEngineSceneView;

            if (foundDViewportView != null)
                return foundDViewportView;

            if (element is ContentControl contentControl)
            {
                // Check the element's Content
                foundDViewportView = FindSharpEngineSceneView(contentControl.Content);
            }
            else if (element is Decorator decorator) // for example Border
            {
                // Check the element's Child
                foundDViewportView = FindSharpEngineSceneView(decorator.Child);
            }
            else if (element is Page page)
            {
                // Page is not ContentControl so handle it specially (note: Window is ContentControl)
                foundDViewportView = FindSharpEngineSceneView(page.Content);
            }
            else if (element is ItemsControl itemsControl) // for example TabControl
            {
                // Check each child of a Panel
                foreach (object oneItem in itemsControl.Items)
                {
                    if (oneItem is UIElement uiItem)
                    {
                        foundDViewportView = FindSharpEngineSceneView(uiItem);

                        if (foundDViewportView != null)
                            break;
                    }
                }
            }
            else if (element is Panel panel)
            {
                // Check each child of a Panel
                foreach (UIElement oneChild in panel.Children)
                {
                    foundDViewportView = FindSharpEngineSceneView(oneChild);

                    if (foundDViewportView != null)
                        break;
                }
            }

            return foundDViewportView;
        }

        private void LogoImage_OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // For CORE3 project we need to set UseShellExecute to true,
            // otherwise a "The specified executable is not a valid application for this OS platform" exception is thrown.
            System.Diagnostics.Process.Start(new ProcessStartInfo("https://www.ab4d.com") { UseShellExecute = true });
        }

        private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
        {
            // Prevent navigation (for example clicking back button) because our ListBox is not updated when this navigation occurs
            // We prevent navigation with clearing the navigation history each time navigation item changes
            ContentFrame.NavigationService.RemoveBackEntry();
        }

                
        private void ReportUnhandledException(object o, UnhandledExceptionEventArgs e)
        {
            if ((System.Diagnostics.Debugger.IsAttached && PreventShowingErrorReportWhenDebuggerIsAttached) || e.ExceptionObject is not Exception ex) // When debugger is attached, let the debugger handle the exception
                return;

            var errorReport = CommonDiagnostics.GetErrorReport(ex, _currentSampleLocation, _currentSharpEngineSceneView, addInnerExceptions: false, addStackTrace: false, addSystemInfo: false, addEngineSettings: false, addReportIssueText: true);
            var message = errorReport + "\nWould you like to save detailed error report with stack trace and system info to a txt file on the desktop?";

            var result = MessageBox.Show(message, "Unhandled exception", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                var fullErrorReport = CommonDiagnostics.GetErrorReport(ex, _currentSampleLocation, _currentSharpEngineSceneView, addInnerExceptions: true, addStackTrace: true, addSystemInfo: true, addEngineSettings: true, addReportIssueText: true);
                System.IO.File.WriteAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SharpEngineSample-error-report.txt"), fullErrorReport);
            }
        }

        public void ReloadCurrentSample()
        {
            var savedItem = SampleList.SelectedItem;

            SampleList.SelectedItem = null;

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate
            {
                SampleList.SelectedItem = savedItem;
            }));
        }

        public void ShowFullScreen()
        {
            Grid.SetColumn(RightSideBorder, 0);
            Grid.SetColumnSpan(RightSideBorder, 2);
            Grid.SetRow(RightSideBorder, 0);
            Grid.SetRowSpan(RightSideBorder, 2);

            RightSideBorder.Margin = new Thickness(0);
            RightSideBorder.Padding = new Thickness(0);

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize; // This will also covert the task bar
            WindowState = WindowState.Maximized;

            // Allow hitting escape to exit full screen
            this.PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
                ExitFullScreen();
        }

        public void ExitFullScreen()
        {
            this.PreviewKeyDown -= OnPreviewKeyDown;

            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;

            Grid.SetColumn(RightSideBorder, 1);
            Grid.SetColumnSpan(RightSideBorder, 1);
            Grid.SetRow(RightSideBorder, 1);
            Grid.SetRowSpan(RightSideBorder, 1);

            RightSideBorder.Margin = new Thickness(5);
            RightSideBorder.Padding = new Thickness(10);
        }

        private void SetupTestRunnerButton()
        {
            _testRunnerButton = new Button() { Content = "TEST", Margin = new Thickness(0, 0, 5, 0) };
            _testRunnerButton.Click += TestButton_OnClick;

            ButtonsPanel.Children.Insert(0, _testRunnerButton);

            GraphicsSettingsButton.Margin = new Thickness(0, 0, 5, 0);
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_randomSamplesRunner == null)
            {
                _randomSamplesRunner = new RandomSamplesRunner(samplesList: (List<System.Xml.XmlNode>)SampleList.ItemsSource,
                                                               sampleSelectorAction: sampleIndex => SampleList.SelectedIndex = sampleIndex,
                                                               beginInvokeAction: action => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action),
                                                               customLogAction: null, 
                                                               showCurrentlyRunningSample: false);
            }

            if (_randomSamplesRunner.IsRunning)
            {
                _randomSamplesRunner.Stop();
                
                if (_testRunnerButton != null)
                    _testRunnerButton.Content = "TEST";
            }
            else
            {
                if (_testRunnerButton != null)
                    _testRunnerButton.Content = "STOP";
                
                _randomSamplesRunner.Start(); // run all samples

                // To run only some samples, use:
                // To get the indexes of the samples run _randomSamplesRunner.DumpAllSamples() in the Immediate window
                //_randomSamplesRunner.Start(startSampleIndex: 10, endSampleIndex: 50); 
            }
        }
    }
}
