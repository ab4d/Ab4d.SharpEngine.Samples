// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Microsoft.UI.Xaml;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Transformations;
using Microsoft.UI;
using System;
using Windows.ApplicationModel;
using Ab4d.Assimp;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Windowing;
using Colors = Microsoft.UI.Colors;
using Ab4d.SharpEngine.Vulkan;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Windows.UI;
using Windows.UI.Text;
using Microsoft.UI.Text;
using System.Linq;
using System.Reflection;
using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Input;
using System.IO;
using Ab4d.SharpEngine.Samples.WinUI.Diagnostics;
using Ab4d.SharpEngine.Samples.WinUI.Settings;
using Microsoft.Graphics.Display;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace Ab4d.SharpEngine.Samples.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SamplesWindow : Window
    {
        //private string? _startupPage = "StandardModels.BoxModelNodeSample";
        private string? _startupPage = null;

        private bool _applySeparator;
        private TextBlock? _errorTextBlock;

        private CommonSample? _currentCommonSample;
        private Control? _currentSampleControl;
        private CommonWinUISampleUserControl? _commonWinUISampleUserControl;

        private CommonTitleUserControl? _commonTitlePage;
        private Dictionary<Assembly, string[]>? _assemblyEmbeddedResources;

        private bool _isSceneViewInitializedSubscribed;
        private bool _isPresentationTypeChangedSubscribed;
        private bool _isSceneViewSizeChangedSubscribed;
        private ISharpEngineSceneView? _currentSharpEngineSceneView;

        private BitmapImage? _disabledDiagnosticsImage;
        private BitmapImage? _diagnosticsImage;

        private BitmapImage? _newBitmap;
        private BitmapImage? _updatedBitmap;

        private DiagnosticsWindow? _diagnosticsWindow;
        private bool _automaticallyOpenDiagnosticsWindow;
        private Windows.Graphics.PointInt32 _diagnosticsWindowPosition;

        private SolidColorBrush _samplesListTextBrush = new SolidColorBrush(Color.FromArgb(255, 204, 204, 204));        // #CCC
        private SolidColorBrush _samplesListHeaderTextBrush = new SolidColorBrush(Color.FromArgb(255, 238, 238, 238));  // #EEE
        private SolidColorBrush _samplesListSelectedTextBrush = new SolidColorBrush(Color.FromArgb(255, 255, 187, 88)); // #FFBC57


        public SamplesWindow()
        {
            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  platforms: "All",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

            // By default, enable logging of warnings and errors.
            // In case of problems please send the log text with the description of the problem to AB4D company
            Utilities.Log.LogLevel = LogLevels.Warn;
            Utilities.Log.IsLoggingToDebugOutput = true;


            InitializeComponent(); // To generate the source for InitializeComponent include XamlNameReferenceGenerator

            WinUiUtils.SetWindowIcon(this, @"Assets\sharp-engine-logo.ico");
            WinUiUtils.ChangeCursor(SamplesListBox, InputSystemCursor.Create(InputSystemCursorShape.Hand));

            DisableDiagnosticsButton();

            WinUISamplesContext.Current.CurrentSharpEngineSceneViewChanged += OnCurrentSharpEngineSceneViewChanged;

            this.Title = "Ab4d.SharpEngine samples for WinUI 3";

            LoadSamples();

            this.Closed += delegate (object sender, WindowEventArgs args)
            {
                CloseDiagnosticsWindow();
            };
        }

        private void LoadSamples()
        {
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples.xml");

            var xmlDcoument = new XmlDocument();
            xmlDcoument.Load(fileName);

            if (xmlDcoument.DocumentElement == null)
                throw new Exception("Cannot load Samples.xml");

            var xmlNodeList = xmlDcoument.DocumentElement.SelectNodes("/Samples/Sample");

            if (xmlNodeList == null || xmlNodeList.Count == 0)
                throw new Exception("No samples in Samples.xml");


            //var listBoxItems = new List<ListBoxItem>();

            int selectedIndex = 0;
            foreach (XmlNode xmlNode in xmlNodeList)
            {
                try
                {
                    var listBoxItem = CreateListBoxItem(xmlNode);

                    if (listBoxItem != null)
                    {
                        SamplesListBox.Items.Add(listBoxItem);

                        if (listBoxItem.IsSelected)
                        {
                            selectedIndex = SamplesListBox.Items.Count - 1;
                            listBoxItem.IsSelected = false;
                        }
                    }
                }
                catch
                {
                    Debug.WriteLine("Error parsing sample xml for " + xmlNode.OuterXml);
                }
            }

            //SamplesListBox.Items = listBoxItems;

            if (selectedIndex != -1)
                SamplesListBox.SelectedIndex = selectedIndex;
        }

        private ListBoxItem? CreateListBoxItem(XmlNode xmlNode)
        {
            if (xmlNode.Attributes == null)
                return null;

            bool isSeparator = false;
            bool isTitle = false;
            bool isNew = false;
            bool isUpdated = false;
            string? updateInfo = null;

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
                    
                    case "isnew":
                        isNew = true;
                        break;
                    
                    case "isupdated":
                        isUpdated = true;
                        break;
                    
                    case "updateinfo":
                        updateInfo = attribute.Value.Replace("\\n", "\n");
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
                textBlock.FontWeight = FontWeights.Bold;
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


            if (isNew)
            {
                _newBitmap ??= new BitmapImage(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\new_icon.png")));
                var newImage = new Image()
                {
                    Source = _newBitmap,
                    Width = 19,
                    Height = 9,
                    Margin = new Thickness(5, 3, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                ToolTipService.SetToolTip(newImage, "New sample in this version");

                stackPanel.Children.Add(newImage);
            }

            if (isUpdated)
            {
                _updatedBitmap ??= new BitmapImage(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\updated_icon.png")));
                var updatedImage = new Image()
                {
                    Source = _updatedBitmap,
                    Width = 13,
                    Height = 9,
                    Margin = new Thickness(5, 3, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                ToolTipService.SetToolTip(updatedImage, updateInfo ?? "Updated sample");

                stackPanel.Children.Add(updatedImage);
            }


            var listBoxItem = new ListBoxItem()
            {
                Content = stackPanel,
                Tag = location,
                //Margin = new Thickness(0),
                //Padding = new Thickness(0)
            };

            if (_startupPage != null && _startupPage == location)
                listBoxItem.IsSelected = true;

            return listBoxItem;
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
                var sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.WinUI.{location}, Ab4d.SharpEngine.Samples.WinUI", throwOnError: false);

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

                        var commonSamplesContext = WinUISamplesContext.Current;

                        //var commonSample = Activator.CreateInstance(sampleType, new object?[] { commonSamplesContext }) as CommonSample;
                        var commonSample = selectedConstructorInfo.Invoke(new object?[] { commonSamplesContext }) as CommonSample;

                        _commonWinUISampleUserControl ??= new CommonWinUISampleUserControl();

                        _commonWinUISampleUserControl.CurrentCommonSample = commonSample;

                        _currentSampleControl = _commonWinUISampleUserControl;

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

            // Set WinUISamplesContext.Current.CurrentSharpEngineSceneView before the new sample is loaded
            if (_currentSampleControl != null)
            {
                SharpEngineSceneView? sharpEngineSceneView;
                if (ReferenceEquals(_currentSampleControl, _commonWinUISampleUserControl))
                    sharpEngineSceneView = _commonWinUISampleUserControl.MainSharpEngineSceneView;
                else
                    sharpEngineSceneView = FindSharpEngineSceneView(_currentSampleControl);

                WinUISamplesContext.Current.RegisterCurrentSharpEngineSceneView(sharpEngineSceneView);
            }

            RightSideBorder.Child = _currentSampleControl;
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
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Red),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            _errorTextBlock.Text = errorMessage;

            RightSideBorder.Child = _errorTextBlock;
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
            var sharpEngineSceneView = WinUISamplesContext.Current.CurrentSharpEngineSceneView;

            if (sharpEngineSceneView != null && !_isSceneViewSizeChangedSubscribed)
            {
                sharpEngineSceneView.ViewSizeChanged += OnSceneViewOnViewSizeChanged;
                _isSceneViewSizeChangedSubscribed = true;
            }

            if (sharpEngineSceneView == null || !sharpEngineSceneView.SceneView.IsInitialized)
            {
                DisableDiagnosticsButton();

                if (_diagnosticsWindow != null)
                {
                    _automaticallyOpenDiagnosticsWindow = true; // This will reopen the diagnostics window
                    var appWindow = WinUiUtils.GetAppWindow(_diagnosticsWindow);
                    _diagnosticsWindowPosition = appWindow.Position;
                }

                CloseDiagnosticsWindow();


                // we cannot use Hidden as Visibility so just remove and set Text
                // We need to set a space otherwise the height will be calculated differently
                SelectedGraphicInfoTextBlock.Text = " ";
                UsedGpuTextBlock.Text = " ";
                ViewSizeInfoTextBlock.Text = " ";

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
            ToolTipService.SetToolTip(ViewSizeInfoTextBlock, null);


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

                if (_diagnosticsWindow != null)
                {
                    var appWindow = WinUiUtils.GetAppWindow(_diagnosticsWindow);
                    appWindow.Move(_diagnosticsWindowPosition);
                }

                _automaticallyOpenDiagnosticsWindow = false;
            }
        }

        private void OnPresentationTypeChanged(object? sender, string? reason)
        {
            UpdateGraphicsCardInfo();

            if (reason != null)
                ToolTipService.SetToolTip(ViewSizeInfoTextBlock, reason);
            else
                ToolTipService.SetToolTip(ViewSizeInfoTextBlock, null);
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

            if (element is UserControl userControl)
            {
                // Check the element's Content
                foundDViewportView = FindSharpEngineSceneView(userControl.Content);
            }
            else if (element is ContentControl contentControl)
            {
                // Check the element's Content
                foundDViewportView = FindSharpEngineSceneView(contentControl.Content);
            }
            else if (element is Border border)
            {
                // Check the element's Content
                foundDViewportView = FindSharpEngineSceneView(border.Child);
            }
            else if (element is ItemsControl itemsControl) // for example TabControl
            {
                // Check each child of a Panel
                foreach (object oneItem in itemsControl.Items)
                {
                    if (oneItem is Control control)
                    {
                        foundDViewportView = FindSharpEngineSceneView(control);

                        if (foundDViewportView != null)
                            break;
                    }
                }
            }
            else if (element is Panel panel)
            {
                // Check each child of a Panel
                foreach (object oneChild in panel.Children)
                {
                    //if (oneChild is Control control)
                    //{
                    foundDViewportView = FindSharpEngineSceneView(oneChild);

                    if (foundDViewportView != null)
                        break;
                    //}
                }
            }

            return foundDViewportView;
        }
                
        
        private void UpdateSelectedGraphicInfo(ISharpEngineSceneView sharpEngineSceneView)
        {
            if (sharpEngineSceneView.GpuDevice != null)
            {
                UsedGpuTextBlock.Text = "Used graphics card:"; // we cannot use Hidden as Visibility so just remove and set Text
                SelectedGraphicInfoTextBlock.Text = sharpEngineSceneView.GpuDevice.GpuName;
                UsedGpuTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                UsedGpuTextBlock.Text = " "; // we cannot use Hidden as Visibility so just remove and set Text
                SelectedGraphicInfoTextBlock.Text = " ";
                UsedGpuTextBlock.Visibility = Visibility.Collapsed;
            }

            ToolTipService.SetToolTip(SelectedGraphicInfoTextBlock, null);
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
            var sharpEngineSceneView = WinUISamplesContext.Current.CurrentSharpEngineSceneView;
            if (sharpEngineSceneView != null)
                UpdateViewSizeInfo(sharpEngineSceneView);
        }

        private void EnableDiagnosticsButton()
        {
            _diagnosticsImage ??= new BitmapImage(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Diagnostics.png")));

            DiagnosticsImage.Source = _diagnosticsImage;
            DiagnosticsButton.IsEnabled = true;
        }

        private void DisableDiagnosticsButton()
        {
            _disabledDiagnosticsImage ??= new BitmapImage(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Diagnostics-gray.png")));

            DiagnosticsImage.Source = _disabledDiagnosticsImage;
            DiagnosticsButton.IsEnabled = false;
        }

        private void CloseDiagnosticsWindow()
        {
            if (_diagnosticsWindow != null)
            {
                _diagnosticsWindow.Close();
                _diagnosticsWindow = null;
            }
        }

        private void OpenDiagnosticsWindow()
        {
            if (_currentSharpEngineSceneView == null)
                return;

            if (_diagnosticsWindow == null)
            {
                _diagnosticsWindow = new DiagnosticsWindow();
                _diagnosticsWindow.Closed += (sender, args) => _diagnosticsWindow = null;
            }

            _diagnosticsWindow.SharpEngineSceneView = _currentSharpEngineSceneView;

            // Position the Diagnostics window to the edge of the main window
            // HM: How to get the size of the screen (DisplayInformation.GetForCurrentView does not work in WinUI 3)

            //var mainWindow = WinUiUtils.GetAppWindow(this);
            //var diagnosticsWindow = WinUiUtils.GetAppWindow(_diagnosticsWindow);

            //// Position DiagnosticsWindow to the top-left corner of our window
            //double left = mainWindow.Position.X + mainWindow.Size.Width;

            // The following does not work in WinUI 3
            //var currentView = DisplayInformation.GetForCurrentView();

            //double diagnosticsWindowWidth = DiagnosticsWindow.InitialWindowWidth * currentView.RawDpiX;
            //double maxLeft = left + diagnosticsWindowWidth;

            //if (maxLeft > currentView.ScreenWidthInRawPixels)
            //{
            //    if (mainWindow.Position.X > diagnosticsWindowWidth)
            //        left = mainWindow.Position.X - diagnosticsWindowWidth;
            //    else
            //        left = currentView.ScreenWidthInRawPixels - diagnosticsWindowWidth;
            //}

            //diagnosticsWindow.Move(new Windows.Graphics.PointInt32((int)left, mainWindow.Position.Y));

            _diagnosticsWindow.Activate();
        }

        private async void OpenSettingsWindow()
        {
            var settingsWindow = new SettingsWindow();

            var contentDialog = new ContentDialog()
            {
                XamlRoot = this.RootGrid.XamlRoot,
                Content = settingsWindow
            };

            settingsWindow.ContentDialog = contentDialog;

            await contentDialog.ShowAsync();

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

        private void GraphicsSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }
        
        private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDiagnosticsWindow();
        }

        private void SamplesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowSelectedSample(e);
        }
    }
}
