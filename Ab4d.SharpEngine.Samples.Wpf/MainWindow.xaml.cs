using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.Samples.Wpf.Diagnostics;
using Ab4d.SharpEngine.Wpf;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Uncomment the _startupPage declaration to always start the samples with the specified page
        //private string? _startupPage = "Advanced.MultiSceneNodesSample";
        private string? _startupPage = null;

        private ISharpEngineSceneView? _currentSharpEngineSceneView;
        private bool _isPresentationTypeChangedSubscribed;
        private bool _isSceneViewInitializedSubscribed;

        private CommonWpfSamplePage? _commonWpfSamplePage;
        private CommonTitlePage? _commonTitlePage;

        private BitmapImage? _diagnosticsDisabledImage;
        private BitmapImage? _diagnosticsEnabledImage;

        private Dictionary<Assembly, string[]>? _assemblyEmbeddedResources;

        private DiagnosticsWindow? _diagnosticsWindow;
        private bool _automaticallyOpenDiagnosticsWindow;

        private string? _currentSampleXaml;
        private CommonSample? _currentCommonSample;

        public MainWindow()
        {
            // The following is a sample global exception handler that can be used 
            // to get system info (with details about graphics card and drivers)
            // in case of exception in SharpEngine.
            // You can use similar code to improve your error reporting data.
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                if (e.ExceptionObject is SharpEngineException)
                {
                    // Here we just show a MessageBox with some exception info.
                    // In a real application it is recommended to report or store full exception and system info (fullSystemInfo)
                    MessageBox.Show(string.Format("Unhandled {0} occurred while running the sample:\r\n{1}\r\n\r\nIf this is not expected, please report that to support@ab4d.com.",
                        e.ExceptionObject.GetType().Name,
                        ((Exception)e.ExceptionObject).Message),
                        "Ab4d.SharpEngine exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            InitializeComponent();

            DisableDiagnosticsButton();

            WpfSamplesContext.Current.CurrentSharpEngineSceneViewChanged += OnCurrentSharpEngineSceneViewChanged;


            this.Unloaded += (sender, args) => CloseDiagnosticsWindow();

            ContentFrame.LoadCompleted += (o, args) =>
            {
                // When content of ContentFrame is changed, we try to find the SharpEngineSceneView control
                // that is defined by the newly shown content.

                // Find SharpEngineSceneView
                var sharpEngineSceneView = FindSharpEngineSceneView(ContentFrame.Content);
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

        private void ShowSelectedSample(SelectionChangedEventArgs args)
        {
            if (args.AddedItems == null || args.AddedItems.Count == 0 || args.AddedItems[0] is not XmlElement xmlElement)
                return;

            if (_currentCommonSample != null)
            {
                _currentCommonSample.Dispose();
                _currentCommonSample = null;
            }

            var locationAttribute = xmlElement.GetAttribute("Location");

            if (locationAttribute.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                ContentFrame.Source = new Uri(locationAttribute, UriKind.Relative);

                _currentSampleXaml   = locationAttribute;
                _currentCommonSample = null;
                return;
            }
            
            if (locationAttribute.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var markdownText = GetMarkdownText(locationAttribute);

                if (markdownText != null)
                {
                    _commonTitlePage ??= new CommonTitlePage();
                    _commonTitlePage.MarkdownText = markdownText;

                    ContentFrame.Navigate(_commonTitlePage);

                    _currentSampleXaml = locationAttribute;
                    _currentCommonSample = null;
                    return;
                }
            }

            // Try to create common sample type from page attribute

            var sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.Wpf.{locationAttribute}, Ab4d.SharpEngine.Samples.Wpf", throwOnError: false);

            if (sampleType == null)
                sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.Common.{locationAttribute}, Ab4d.SharpEngine.Samples.Common", throwOnError: false);

            if (sampleType != null)
            {
                var commonSamplesContext = WpfSamplesContext.Current;
                var commonSample = Activator.CreateInstance(sampleType, new object?[] { commonSamplesContext }) as CommonSample;

                _commonWpfSamplePage ??= new CommonWpfSamplePage();
                
                if (_currentSampleXaml != null || ContentFrame.Content == null)
                {
                    ContentFrame.Navigate(_commonWpfSamplePage);
                    _currentSampleXaml = null;
                }

                _commonWpfSamplePage.CurrentCommonSample = commonSample;

                _currentCommonSample = commonSample;
            }
            else
            {
                MessageBox.Show("Cannot find: " + locationAttribute, "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

            if (sharpEngineSceneView == null || !sharpEngineSceneView.SceneView.BackBuffersInitialized)
            {
                DisableDiagnosticsButton();

                if (_diagnosticsWindow != null)
                    _automaticallyOpenDiagnosticsWindow = true; // This will reopen the diagnostics window

                CloseDiagnosticsWindow();

                SelectedGraphicInfoTextBlock.Text = null;
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

            if (sharpEngineSceneView.GpuDevice != null)
            {
                SelectedGraphicInfoTextBlock.Text = sharpEngineSceneView.GpuDevice.GpuName + $" ({sharpEngineSceneView.PresentationType.ToString()})";
                SelectedGraphicInfoTextBlock.ToolTip = null;

                UsedGpuTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                SelectedGraphicInfoTextBlock.Text = "";
                SelectedGraphicInfoTextBlock.ToolTip = null;

                UsedGpuTextBlock.Visibility = Visibility.Hidden;
            }

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

        private void OnPresentationTypeChanged(object? sender, string? reason)
        {
            UpdateGraphicsCardInfo();

            if (reason != null)
                SelectedGraphicInfoTextBlock.ToolTip = reason;
            else
                SelectedGraphicInfoTextBlock.ToolTip = null;
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
        

        private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDiagnosticsWindow();
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

            _diagnosticsWindow =  new DiagnosticsWindow();
            _diagnosticsWindow.SharpEngineSceneView =  sharpEngineSceneView;
            _diagnosticsWindow.Closing += (sender, args) => _diagnosticsWindow = null;

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
        private SharpEngineSceneView? FindSharpEngineSceneView(object element)
        {
            var foundDViewportView = element as SharpEngineSceneView;

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
    }
}
