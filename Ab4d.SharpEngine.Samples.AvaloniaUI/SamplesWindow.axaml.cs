using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Xml;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Diagnostics;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Settings;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI
{
    public partial class SamplesWindow : Window
    {
        //private string? _startupPage = "AdvancedModels.MultiMaterialModelNodeSample";
        private string? _startupPage = null;

        private Dictionary<string, Bitmap>? _resourceBitmaps;

        private CommonTitleUserControl? _commonTitlePage;
        private Dictionary<Assembly, string[]>? _assemblyEmbeddedResources;

        private SolidColorBrush _samplesListTextBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));        // #CCC
        private SolidColorBrush _samplesListHeaderTextBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238));  // #EEE

        private bool _applySeparator;

        private DiagnosticsWindow? _diagnosticsWindow;
        private bool _automaticallyOpenDiagnosticsWindow;

        private CommonAvaloniaSampleUserControl? _commonAvaloniaSampleUserControl;
        private Control? _currentSampleControl;
        private CommonSample? _currentCommonSample;
        private bool _isSceneViewInitializedSubscribed;
        private bool _isPresentationTypeChangedSubscribed;
        private bool _isSceneViewSizeChangedSubscribed;

        private ISharpEngineSceneView? _currentSharpEngineSceneView;

        private TextBlock? _errorTextBlock;

        public SamplesWindow()
        {
            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  platforms: "All",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

            if (Application.Current != null)
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;


            // By default, enable logging of warnings and errors.
            // In case of problems please send the log text with the description of the problem to AB4D company
            Utilities.Log.LogLevel = LogLevels.Warn;
            Utilities.Log.IsLoggingToDebugOutput = true;

            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            InitializeComponent(); // To generate the source for InitializeComponent include XamlNameReferenceGenerator


#if VULKAN_BACKEND
            // When using Vulkan backend, then the whole application uses Vulkan (also to render Avalonia UI).
            // This requires Avalonia v11.1+ and Ab4d.SharpEngine.AvaloniaUI v2.0.8990 or newer.
            this.Title += " using Vulkan backend";
#endif


            DisableDiagnosticsButton();

            AvaloniaSamplesContext.Current.CurrentSharpEngineSceneViewChanged += OnCurrentSharpEngineSceneViewChanged;

            this.Loaded += delegate(object? sender, RoutedEventArgs args)
            {
                LoadSamples();
            };

            this.Closing += delegate(object? sender, WindowClosingEventArgs args)
            {
                CloseDiagnosticsWindow();
            };

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void LoadSamples()
        {
            if (Avalonia.Controls.Design.IsDesignMode)
                return;

            var xmlDcoument = new XmlDocument();
            xmlDcoument.Load(@"Samples.xml");

            if (xmlDcoument.DocumentElement == null)
                throw new Exception("Cannot load Samples.xml");

            var xmlNodeList = xmlDcoument.DocumentElement.SelectNodes("/Samples/Sample");

            if (xmlNodeList == null || xmlNodeList.Count == 0)
                throw new Exception("No samples in Samples.xml");


            var listBoxItems = new List<ListBoxItem>();

            int selectedIndex = 0;
            foreach (XmlNode xmlNode in xmlNodeList)
            {
                try
                {
                    var listBoxItem = CreateListBoxItem(xmlNode);

                    if (listBoxItem != null)
                    {
                        listBoxItems.Add(listBoxItem);

                        if (listBoxItem.IsSelected)
                        {
                            selectedIndex = listBoxItems.Count - 1;
                            listBoxItem.IsSelected = false;
                        }
                    }
                }
                catch
                {
                    Debug.WriteLine("Error parsing sample xml for " + xmlNode.OuterXml);
                }
            }

            SamplesList.ItemsSource = listBoxItems;

            if (selectedIndex != -1)
                SamplesList.SelectedIndex = selectedIndex;
        }

        private ListBoxItem? CreateListBoxItem(XmlNode xmlNode)
        {
            if (xmlNode.Attributes == null)
                return null;

            bool isSeparator = false;
            bool isTitle = false;

            string? location = null;
            string? title = null;
            
            foreach (XmlAttribute attribute in xmlNode.Attributes)
            {
                switch (attribute.Name.ToLower())
                {
                    case "location":
                        location = attribute.Value;
                        break;
                    
                    case "title":
                        title = attribute.Value;
                        break;
                    
                    case "isseparator":
                        isSeparator = true;
                        break;
                    
                    case "istitle":
                        isTitle = true;
                        break;
                }
            }

            if (isSeparator)
            {
                _applySeparator = true;
                return null;
            }

            var stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            var textBlock = new TextBlock()
            {
                Text = title,
                FontSize = 14
            };

            double topMargin;
            double bottomMargin;

            if (isTitle)
            {
                textBlock.FontWeight = FontWeight.Bold;
                textBlock.Foreground = _samplesListHeaderTextBrush;
                topMargin = 6;
                bottomMargin = 1;
            }
            else
            {
                textBlock.Foreground = _samplesListTextBrush;
                topMargin = 0;
                bottomMargin = 0;
            }

            if (_applySeparator)
            {
                topMargin += 4;
                _applySeparator = false;
            }

            textBlock.Margin = new Thickness(isTitle ? 4 : 10, topMargin, 0, bottomMargin);
            
            stackPanel.Children.Add(textBlock);

            var listBoxItem = new ListBoxItem()
            {
                Content = stackPanel,
                Tag = location,
            };

            if (_startupPage != null && _startupPage == location)
                listBoxItem.IsSelected = true;

            return listBoxItem;
        }

        private void LogoImage_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo("https://www.ab4d.com") { UseShellExecute = true });
        }

        private void GraphicsSettingsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }
        
        private void DiagnosticsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            OpenDiagnosticsWindow();
        }

        private void SamplesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            ShowSelectedSample(e);
        }

        private void ShowSelectedSample(SelectionChangedEventArgs args)
        {
            if (args.AddedItems == null || args.AddedItems.Count == 0 || args.AddedItems[0] is not ListBoxItem listBoxItem)
                return;

            var location = listBoxItem.Tag as string;

            if (location == null) 
                return;


            _currentSampleControl = null;

            if (_currentCommonSample != null)
            {
                _currentCommonSample.Dispose();
                _currentCommonSample = null;
            }

            if (location.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var markdownText = GetMarkdownText(location);

                if (markdownText != null)
                {
                    _commonTitlePage ??= new CommonTitleUserControl();
                    _commonTitlePage.MarkdownText = markdownText;

                    _currentSampleControl = _commonTitlePage;
                    _currentCommonSample = null;
                }
            }

            if (_currentSampleControl == null)
            {
                // Try to create common sample type from page attribute
                var sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.AvaloniaUI.{location}, Ab4d.SharpEngine.Samples.AvaloniaUI", throwOnError: false);

                if (sampleType == null)
                    sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.Common.{location}, Ab4d.SharpEngine.Samples.Common", throwOnError: false);

                if (sampleType != null)
                {
                    var constructors = sampleType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

                    // Try to find a constructor that takes ICommonSamplesContext, else use constructor without any parameters
                    ConstructorInfo? selectedConstructorInfo = null;
                    bool isCommonSampleType = false;

                    foreach (var constructorInfo in constructors)
                    {
                        var parameterInfos = constructorInfo.GetParameters();

                        // First try to get constructor that takes ICommonSamplesContext
                        if (parameterInfos.Any(p => p.ParameterType == typeof(ICommonSamplesContext)))
                        {
                            selectedConstructorInfo = constructorInfo;
                            isCommonSampleType = true;
                            break;
                        }

                        // ... else use constructor without any parameters
                        if (selectedConstructorInfo == null && parameterInfos.Length == 0)
                        {
                            selectedConstructorInfo = constructorInfo;
                            isCommonSampleType = false;
                        }
                    }

                    if (selectedConstructorInfo == null)
                    {
                        ShowError("No constructor without parameters or with ICommonSamplesContext found for the sample:" + Environment.NewLine + location);
                        return;
                    }

                    if (isCommonSampleType)
                    {
                        // Create a common sample control (calling constructor that takes ICommonSamplesContext as parameter)

                        var commonSamplesContext = AvaloniaSamplesContext.Current;

                        //var commonSample = Activator.CreateInstance(sampleType, new object?[] { commonSamplesContext }) as CommonSample;
                        var commonSample = selectedConstructorInfo.Invoke(new object?[] { commonSamplesContext }) as CommonSample;

                        _commonAvaloniaSampleUserControl ??= new CommonAvaloniaSampleUserControl();

                        _commonAvaloniaSampleUserControl.CurrentCommonSample = commonSample;

                        _currentSampleControl = _commonAvaloniaSampleUserControl;

                        _currentCommonSample = commonSample;
                    }
                    else
                    {
                        // Create sample control (calling constructor without parameters)
                        _currentSampleControl = selectedConstructorInfo.Invoke(null) as Control;
                        _currentCommonSample = null;
                    }
                }
                else
                {
                    _currentSampleControl = null;
                    _currentCommonSample = null;
                }
            }

            if (_currentSampleControl == null)
            {
                ShowError("Sample not found: " + Environment.NewLine + location);
                return;
            }

            // Set AvaloniaSamplesContext.Current.CurrentSharpEngineSceneView before the new sample is loaded
            if (_currentSampleControl != null)
            {
                SharpEngineSceneView? sharpEngineSceneView;
                if (ReferenceEquals(_currentSampleControl, _commonAvaloniaSampleUserControl))
                    sharpEngineSceneView = _commonAvaloniaSampleUserControl.MainSceneView;
                else
                    sharpEngineSceneView = FindSharpEngineSceneView(_currentSampleControl);

                AvaloniaSamplesContext.Current.RegisterCurrentSharpEngineSceneView(sharpEngineSceneView);
            }

            SelectedSampleContentControl.Content = _currentSampleControl;
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

        private void ShowError(string errorMessage)
        {
            _errorTextBlock ??= new TextBlock()
            {
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            _errorTextBlock.Text = errorMessage;

            SelectedSampleContentControl.Content = _errorTextBlock;
        }

        private void EnableDiagnosticsButton()
        {
            DiagnosticsImage.Source = ReadBitmapFromAvaloniaResource("Resources/Diagnostics.png");
            DiagnosticsButton.IsEnabled = true;

            ToolTip.SetTip(DiagnosticsButton, null);
        }

        private void DisableDiagnosticsButton()
        {
            DiagnosticsImage.Source = ReadBitmapFromAvaloniaResource("Resources/Diagnostics-gray.png");

            DiagnosticsButton.IsEnabled = false;

            // Avalonia does not support showing tooltip on disabled control: https://github.com/AvaloniaUI/Avalonia/issues/3847
            //ToolTip.SetTip(DiagnosticsButton, "Diagnostics button is disabled because there is no shown SharpEngineSceneView control.");
        }


        public Bitmap? ReadBitmapFromAvaloniaResource(string resourceName)
        {
            _resourceBitmaps ??= new Dictionary<string, Bitmap>();

            if (_resourceBitmaps.TryGetValue(resourceName, out var cachedBitmap))
                return cachedBitmap;


            var uri = new Uri("avares://Ab4d.SharpEngine.Samples.AvaloniaUI/" + resourceName);

            if (!AssetLoader.Exists(uri))
                return null;

            Bitmap bitmap;
            using (var stream = AssetLoader.Open(uri))
            {
                bitmap = new Bitmap(stream);
            }

            _resourceBitmaps.Add(resourceName, bitmap);

            return bitmap;
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

                if (_isSceneViewSizeChangedSubscribed)
                {
                    _currentSharpEngineSceneView.ViewSizeChanged -= OnSceneViewOnViewSizeChanged;
                    _isSceneViewSizeChangedSubscribed = false;
                }

                _currentSharpEngineSceneView = null;
            }

            UpdateGraphicsCardInfo();
        }

        private void UpdateGraphicsCardInfo()
        {
            var sharpEngineSceneView = AvaloniaSamplesContext.Current.CurrentSharpEngineSceneView;

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

                // we cannot use Hidden as Visibility so just remove and set Text
                SelectedGraphicInfoTextBlock.Text = "";
                UsedGpuTextBlock.Text = "";
                ViewSizeInfoTextBlock.Text = "";

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

            ToolTip.SetTip(ViewSizeInfoTextBlock, null);


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
                UsedGpuTextBlock.Text = "Used graphics card:";
                UsedGpuTextBlock.IsVisible = true;
            }
            else
            {
                SelectedGraphicInfoTextBlock.Text = "";
                UsedGpuTextBlock.IsVisible = false;
            }

            ToolTip.SetTip(SelectedGraphicInfoTextBlock, null);
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
            var sharpEngineSceneView = AvaloniaSamplesContext.Current.CurrentSharpEngineSceneView;
            if (sharpEngineSceneView != null)
                UpdateViewSizeInfo(sharpEngineSceneView);
        }

        private void OnPresentationTypeChanged(object? sender, string? reason)
        {
            UpdateGraphicsCardInfo();

            if (reason != null)
                ToolTip.SetTip(ViewSizeInfoTextBlock, reason);
            else
                ToolTip.SetTip(ViewSizeInfoTextBlock, null);
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

        // Searches the logical controls tree and returns the first instance of SharpEngineSceneView if found
        private SharpEngineSceneView? FindSharpEngineSceneView(object? element)
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
            else if (element is ItemsControl itemsControl) // for example TabControl
            {
                // Check each child of a Panel
                if (itemsControl.Items != null)
                {
                    foreach (object? oneItem in itemsControl.Items)
                    {
                        if (oneItem is Control control)
                        {
                            foundDViewportView = FindSharpEngineSceneView(control);

                            if (foundDViewportView != null)
                                break;
                        }
                    }
                }
            }
            else if (element is Panel panel)
            {
                // Check each child of a Panel
                foreach (var oneControl in panel.Children.OfType<Control>())
                {
                    foundDViewportView = FindSharpEngineSceneView(oneControl);

                    if (foundDViewportView != null)
                        break;
                }
            }

            return foundDViewportView;
        }

        private async void OpenSettingsWindow()
        {
            var settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);

            if (settingsWindow.IsChanged)
            {
                if (_currentSharpEngineSceneView != null)
                {
                    _currentSharpEngineSceneView.SceneView.Resize(newMultisampleCount: GlobalSharpEngineSettings.MultisampleCount,
                                                                  newSupersamplingCount: GlobalSharpEngineSettings.SupersamplingCount,
                                                                  renderNextFrameAfterResize: false);

                    // We need to call RenderScene on SharpEngineSceneView and not only on SceneView, otherwise in SharedTexture mode, the shared texture is not updated.
                    _currentSharpEngineSceneView.RenderScene(forceRender: true, forceUpdate: false);

                    UpdateViewSizeInfo(_currentSharpEngineSceneView);
                }
            }
        }

        private void OpenDiagnosticsWindow()
        {
            if (_currentSharpEngineSceneView == null)
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

            _diagnosticsWindow.SharpEngineSceneView = _currentSharpEngineSceneView;

            // Position DiagnosticsWindow to the top-left corner of our window

            double dpiScale = _currentSharpEngineSceneView.SceneView.DpiScaleX;
            double diagnosticsWindowWidth = DiagnosticsWindow.InitialWindowWidth * dpiScale;

            double left = this.Position.X + this.Width * dpiScale;
            double maxLeft = left + diagnosticsWindowWidth;

            if (Screens.Primary != null && maxLeft > Screens.Primary.WorkingArea.Width)
            {
                if (this.Position.X > diagnosticsWindowWidth)
                    left = this.Position.X - diagnosticsWindowWidth;
                else
                    left = Screens.Primary.WorkingArea.Width - diagnosticsWindowWidth;
            }

            _diagnosticsWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            _diagnosticsWindow.Position = new PixelPoint((int)(left), this.Position.Y);

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
    }
}
