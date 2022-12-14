using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
using Ab4d.SharpEngine.Diagnostics.Wpf;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Wpf;

namespace Ab4d.SharpEngine.Samples.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Uncomment the _startupPage declaration to always start the samples with the specified page
        //private string? _startupPage = "TestScenes/AssimpImporterTestScene.xaml";
        private string? _startupPage = null;

        private BitmapImage? _diagnosticsDisabledImage;
        private BitmapImage? _diagnosticsEnabledImage;

        //private SharpEngineSceneView? _currentSharpEngineSceneView;
        private DiagnosticsWindow? _diagnosticsWindow;

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


            InitializeComponent();

            DisableDiagnosticsButton();

            SamplesContext.Current.CurrentSharpEngineSceneViewChanged += OnCurrentSharpEngineSceneViewChanged;


            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                CloseDiagnosticsWindow(); // If DiagnosticsWindow is not closed, then close it with closing main samples window
            };

            ContentFrame.LoadCompleted += delegate (object o, NavigationEventArgs args)
            {
                // When content of ContentFrame is changed, we try to find the SharpEngineSceneView control
                // that is defined by the newly shown content.

                // Find SharpEngineSceneView
                var sharpEngineSceneView = FindSharpEngineSceneView(ContentFrame.Content);
                SamplesContext.Current.RegisterCurrentSharpEngineSceneView(sharpEngineSceneView);
            };


            // SelectionChanged event handler is used to start the samples with the page set with _startupPage field.
            // SelectionChanged is used because SelectItem cannot be set from this.Loaded event.
            SampleList.SelectionChanged += delegate (object sender, SelectionChangedEventArgs args)
            {
                // The following if can be executed only after the SampleList items are populated
                if (_startupPage != null)
                {
                    string savedStartupPage = _startupPage;
                    _startupPage = null;

                    SelectItem(savedStartupPage);
                }
            };
        }

        private void OnCurrentSharpEngineSceneViewChanged(object? sender, EventArgs e)
        {
            UpdateGraphicsCardInfo();
        }

        private void UpdateGraphicsCardInfo()
        {
            var sharpEngineSceneView = SamplesContext.Current.CurrentSharpEngineSceneView;

            if (sharpEngineSceneView == null || sharpEngineSceneView.Scene == null)
            {
                DisableDiagnosticsButton();
                CloseDiagnosticsWindow();
                SelectedGraphicInfoTextBlock.Text = null;

                if (sharpEngineSceneView != null)
                    sharpEngineSceneView.SceneViewInitialized += OnSceneViewInitialized;

                return;
            }


            string gpuInfoText = sharpEngineSceneView.Scene.GpuDevice.PhysicalDeviceDetails.DeviceName;

            if (sharpEngineSceneView.PresentationType == PresentationTypes.WriteableBitmap)
                gpuInfoText += " (WriteableBitmap)";

            SelectedGraphicInfoTextBlock.Text = gpuInfoText;

            EnableDiagnosticsButton();

            if (_diagnosticsWindow != null)
                _diagnosticsWindow.SharpEngineSceneView = sharpEngineSceneView;
        }

        private void OnSceneViewInitialized(object? sender, EventArgs e)
        {
            UpdateGraphicsCardInfo();
        }
        

        private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDiagnosticsWindow();
        }

        private void EnableDiagnosticsButton()
        {
            if (_diagnosticsEnabledImage == null)
                _diagnosticsEnabledImage = new BitmapImage(new Uri("pack://application:,,,/Ab4d.SharpEngine.Samples.Wpf;component/Resources/Diagnostics.png"));

            DiagnosticsImage.Source = _diagnosticsEnabledImage;
            DiagnosticsButton.IsEnabled = true;

            DiagnosticsButton.ToolTip = null;
        }

        private void DisableDiagnosticsButton()
        {
            if (_diagnosticsDisabledImage == null)
                _diagnosticsDisabledImage = new BitmapImage(new Uri("pack://application:,,,/Ab4d.SharpEngine.Samples.Wpf;component/Resources/Diagnostics-gray.png"));

            DiagnosticsImage.Source = _diagnosticsDisabledImage;
            DiagnosticsButton.IsEnabled = false;

            DiagnosticsButton.ToolTip = "Diagnostics button is disabled because there is no shown SharpEngineSceneView control.";
            ToolTipService.SetShowOnDisabled(DiagnosticsButton, true);
        }

        private void OpenDiagnosticsWindow()
        {
            var sharpEngineSceneView = SamplesContext.Current.CurrentSharpEngineSceneView;

            if (sharpEngineSceneView == null)
            {
                CloseDiagnosticsWindow();
                return;
            }

            _diagnosticsWindow = new DiagnosticsWindow(sharpEngineSceneView);
            _diagnosticsWindow.Closing += delegate (object? sender, CancelEventArgs args)
            {
                _diagnosticsWindow = null;
            };

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
                                                     .First(x => x.Attributes["Page"]?.Value == pageName);

            SampleList.SelectedItem = supportPageElement;

            SampleList.ScrollIntoView(supportPageElement);
        }

        private void RightSideBorder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            string newDescriptionText = "";

            if (e.NewValue is XmlNode xmlNode && xmlNode.Attributes != null)
            {
                var descriptionAttribute = xmlNode.Attributes["Description"];

                if (descriptionAttribute != null)
                    newDescriptionText = descriptionAttribute.Value;


                var seeAlsoAttribute = xmlNode.Attributes["SeeAlso"];

                if (seeAlsoAttribute != null)
                {
                    var seeAlsoText = seeAlsoAttribute.Value;
                    var seeAlsoParts = seeAlsoText.Split(';');

                    var seeAlsoContent = new StringBuilder();
                    for (var i = 0; i < seeAlsoParts.Length; i++)
                    {
                        var seeAlsoPart = seeAlsoParts[i].Trim();
                        if (seeAlsoPart.Length == 0)
                            continue;

                        if (seeAlsoContent.Length > 0)
                            seeAlsoContent.Append(", ");

                        // TextBlockEx support links, for example: "click here \@Ab3d.PowerToys:https://www.ab4d.com/PowerToys.aspx| to learn more"
                        if (seeAlsoPart.StartsWith("\\@"))
                        {
                            seeAlsoContent.Append(seeAlsoPart);
                        }
                        else
                        {
                            string linkDescription;

                            // remove prefix that specifies the type ("T_"), property ("P_"), event ("E_"), ...
                            if (seeAlsoPart[1] == '_') // "T_Ab3d_Controls_MouseCameraController", "P_Ab3d_Controls_MouseCameraController_ClosedHandCursor", ...
                                linkDescription = seeAlsoPart.Substring(2);
                            else
                                linkDescription = seeAlsoPart;

                            linkDescription = linkDescription.Replace('_', '.')                // Convert '_' to '.'
                                                             .Replace("Ab4d.SharpEngine.", "");    // Remove the most common namespaces (preserve less common, for example Ab4d.Utilities)

                            if (seeAlsoPart.EndsWith(".html") || seeAlsoPart.EndsWith(".htm"))
                                linkDescription = linkDescription.Replace(".html", "").Replace(".htm", ""); // remove .html / .htm from linkDescription
                            else
                                seeAlsoPart += ".htm";                                                      // and make sure that the link will end with .htm

                            seeAlsoContent.AppendFormat("\\@{0}:https://www.ab4d.com/help/DXEngine/html/{1}|", linkDescription, seeAlsoPart);
                        }
                    }

                    if (seeAlsoContent.Length > 0)
                    {
                        if (newDescriptionText.Length > 0 && !newDescriptionText.EndsWith("\\n"))
                            newDescriptionText += "\\n"; // Add new line for TextBlockEx

                        newDescriptionText += "See also: " + seeAlsoContent.ToString();
                    }
                }
            }

            if (newDescriptionText.Length > 0)
            {
                DescriptionTextBlock.ContentText = newDescriptionText;
                DescriptionExpander.Visibility = Visibility.Visible;
            }
            else
            {
                DescriptionTextBlock.ContentText = null;
                DescriptionExpander.Visibility = Visibility.Collapsed;
            }
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

        private void DiagnosticsInfoImage_OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // For CORE3 project we need to set UseShellExecute to true,
            // otherwise a "The specified executable is not a valid application for this OS platform" exception is thrown.
            System.Diagnostics.Process.Start(new ProcessStartInfo("https://www.ab4d.com/DirectX/3D/Diagnostics.aspx") { UseShellExecute = true });
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
            ResizeMode = ResizeMode.NoResize; // This will also covert the taskbar
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
