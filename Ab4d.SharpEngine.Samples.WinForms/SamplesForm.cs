using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.WinForms.Common;
using Ab4d.SharpEngine.WinForms;
using Microsoft.Web.WebView2.WinForms;

namespace Ab4d.SharpEngine.Samples.WinForms
{
    public partial class SamplesForm : Form
    {
        //private string? _startupPage = "StandardModels.TorusKnotModelNodeSample";        
        private string? _startupPage = null;

        // To enable Vulkan's standard validation, set EnableStandardValidation to true.
        // Also, you need to install Vulkan SDK from https://vulkan.lunarg.com
        // Using Vulkan validation may reduce the performance of rendering.
        public const bool EnableStandardValidation = true;

        private Font? _headerItemFont;

        private Control? _currentSampleControl;
        
        private CommonWinFormsSampleUserControl? _commonWinFormsSampleUserControl;
        private WebView2? _titleWebView;

        private Dictionary<Assembly, string[]>? _assemblyEmbeddedResources;

        private CommonSample? _currentCommonSample;
        private bool _isSceneViewInitializedSubscribed;

        private string? _currentSampleLocationText;
        private ISharpEngineSceneView? _currentSharpEngineSceneView;

        public SamplesForm()
        {
            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  platforms: "All",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");


            // HighDpiMode is usually set un in Program.cs, but we can also set it up here
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // By default, enable logging of warnings and errors.
            // In case of problems please send the log text with the description of the problem to AB4D company
            Utilities.Log.LogLevel = LogLevels.Warn;
            Utilities.Log.IsLoggingToDebugOutput = true;

            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            InitializeComponent();


            // TODO: Implement Diagnostics window
            // Hide diagnosticsButton for now:
            diagnosticsButton.Visible = false;
            panel3.Size = new Size(panel3.Size.Width, 134);


            WinFormsSamplesContext.Current.CurrentSharpEngineSceneViewChanged += OnCurrentSharpEngineSceneViewChanged;

            UpdateGraphicsCardInfo();

            LoadSamples();
        }


        private void LoadSamples()
        {
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples.xml");
            var samplesXmlNodList = CommonSample.LoadSamples(fileName, 
                                                             uiFramework: "WinForms", 
                                                             errorMessage => MessageBox.Show(errorMessage));

            samplesListView.Columns.Add(new ColumnHeader() { Width = samplesListView.Width - 4 - SystemInformation.VerticalScrollBarWidth });
            samplesListView.HeaderStyle = ColumnHeaderStyle.None;

            if (_headerItemFont == null)
                _headerItemFont = new Font(samplesListView.Font, FontStyle.Bold);

            foreach (XmlNode xmlNode in samplesXmlNodList)
            {
                try
                {
                    var listBoxItem = CreateListBoxItem(xmlNode);

                    if (listBoxItem != null)
                        samplesListView.Items.Add(listBoxItem);
                }
                catch
                {
                    Debug.WriteLine("Error parsing sample xml for " + xmlNode.OuterXml);
                }
            }

            if (_startupPage == null)
                samplesListView.SelectedIndices.Add(0);
        }

        private ListViewItem? CreateListBoxItem(XmlNode xmlNode)
        {
            if (xmlNode.Attributes == null)
                return null;

            bool isSeparator = false;
            bool isTitle = false;

            string? location = null;
            string? title = null;
            bool isNew = false;
            bool isUpdated = false;
            string? updateInfo = null;

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
                return null; // WinForms do not support ListView with items that have custom margins



            if (!isTitle)
                title = "  " + title;

            string? tooltip = null;
            
            if (isNew)
            {
                title += "  (NEW)";
                tooltip = "New sample in this version";
            }

            if (isUpdated)
            {
                title += "  (UP)";
                tooltip = updateInfo ?? "Updated sample";
            }


            var listBoxItem = new ListViewItem(title)
            {
                Tag = location,
            };

            if (isTitle)
                listBoxItem.Font = _headerItemFont;

            if (tooltip != null)
                listBoxItem.ToolTipText = tooltip;

            if (_startupPage != null && _startupPage == location)
                listBoxItem.Selected = true;

            return listBoxItem;
        }

        private async Task ShowSelectedSample(ListViewItem? selectedListViewItem)
        {
            if (selectedListViewItem == null)
            {
                // TODO: Hide current sample
                return;
            }

            var sampleLocation = selectedListViewItem.Tag as string;

            if (sampleLocation == null || sampleLocation == _currentSampleLocationText) 
                return;


            if (!ReferenceEquals(_currentSampleControl, _titleWebView) &&
                !ReferenceEquals(_currentSampleControl, _commonWinFormsSampleUserControl) &&
                _currentSampleControl is IDisposable currentDisposableSampleControl)
            {
                currentDisposableSampleControl.Dispose();
            }

            _currentSampleControl = null;

            if (_currentCommonSample != null)
            {
                _currentCommonSample.Dispose();
                _currentCommonSample = null;
            }

            if (sampleLocation.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var markdownText = GetMarkdownText(sampleLocation);
                    
                if (markdownText != null)
                {
                    var markdownHtml = Markdig.Markdown.ToHtml(markdownText);

                    // Set default font
                    markdownHtml = "<html><body style='font-family: sans-serif; margin: 20pt'>" +
                                   markdownHtml +
                                   "</body></html>";

                    // It is not possible to show local image in WebView2, so we need to replace the local file name with http address:
                    markdownHtml = markdownHtml.Replace("img src=\"Resources/", "img height='400px' src=\"https://www.ab4d.com/Images/CadImporter/");

                    _titleWebView ??= new WebView2()
                    {
                        Dock = DockStyle.Fill
                    };

                    await _titleWebView.EnsureCoreWebView2Async();

                    _titleWebView.NavigateToString(markdownHtml);

                    _currentSampleControl = _titleWebView;
                    _currentCommonSample = null;
                }
            }

            if (_currentSampleControl == null)
            {
                var createdSample = CommonSample.CreateSampleObject(uiFramework: "WinForms", sampleLocation, WinFormsSamplesContext.Current, errorMessage => MessageBox.Show(errorMessage));

                if (createdSample is CommonSample createdCommonSample)
                {
                    if (_commonWinFormsSampleUserControl == null)
                    {
                        _commonWinFormsSampleUserControl = new CommonWinFormsSampleUserControl();
                        _commonWinFormsSampleUserControl.Dock = DockStyle.Fill;
                    }

                    // Set CurrentSharpEngineSceneView before the _commonWinFormsSampleUserControl is loaded
                    WinFormsSamplesContext.Current.RegisterCurrentSharpEngineSceneView(_commonWinFormsSampleUserControl.MainSceneView);

                    this.SuspendLayout();
                    _commonWinFormsSampleUserControl.CurrentCommonSample = createdCommonSample;
                    this.ResumeLayout();

                    _currentSampleControl = _commonWinFormsSampleUserControl;

                    _currentCommonSample = createdCommonSample;
                }
                else if (createdSample is UserControl createdControl)
                {
                    createdControl.Dock = DockStyle.Fill;
                    _currentSampleControl = createdControl;
                    _currentCommonSample = null;
                }
                else
                {
                    _currentSampleControl = null;
                    _currentCommonSample = null;
                }
            }

            // Set WinFormsSamplesContext.Current.CurrentSharpEngineSceneView before the new sample is loaded
            if (_currentSampleControl != null)
            {
                SharpEngineSceneView? sharpEngineSceneView;
                if (ReferenceEquals(_currentSampleControl, _commonWinFormsSampleUserControl))
                    sharpEngineSceneView = _commonWinFormsSampleUserControl.MainSceneView;
                else
                    sharpEngineSceneView = FindSharpEngineSceneView(_currentSampleControl);

                WinFormsSamplesContext.Current.RegisterCurrentSharpEngineSceneView(sharpEngineSceneView);
            }

            if (!(samplePanel.Controls.Count > 0 && samplePanel.Controls[0] == _currentSampleControl))
            {
                samplePanel.Controls.Clear();
                samplePanel.Controls.Add(_currentSampleControl);
            }

            _currentSampleLocationText = sampleLocation;
        }

        // Searches the logical controls tree and returns the first instance of SharpEngineSceneView if found
        private SharpEngineSceneView? FindSharpEngineSceneView(object? control)
        {
            var foundDViewportView = control as SharpEngineSceneView;

            if (foundDViewportView != null)
                return foundDViewportView;

            if (control is UserControl userControl)
            {
                foreach (var childControl in userControl.Controls)
                {
                    foundDViewportView = FindSharpEngineSceneView(childControl);
                    if (foundDViewportView != null)
                        return foundDViewportView;
                }
            }
            else if (control is Panel panel)
            {
                foreach (var childControl in panel.Controls)
                {
                    foundDViewportView = FindSharpEngineSceneView(childControl);
                    if (foundDViewportView != null)
                        return foundDViewportView;
                }
            }

            return foundDViewportView;
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

                _currentSharpEngineSceneView = null;
            }

            UpdateGraphicsCardInfo();
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

        private void UpdateGraphicsCardInfo()
        {
            var sharpEngineSceneView = WinFormsSamplesContext.Current.CurrentSharpEngineSceneView;

            if (sharpEngineSceneView == null || !sharpEngineSceneView.SceneView.BackBuffersInitialized)
            {
                //DisableDiagnosticsButton();

                //if (_diagnosticsWindow != null)
                //    _automaticallyOpenDiagnosticsWindow = true; // This will reopen the diagnostics window

                //CloseDiagnosticsWindow();

                usedGpuTextLabel.Text = "";
                gpuInfoLabel.Text = "";
                

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
                usedGpuTextLabel.Text = "Used graphics card:";
                gpuInfoLabel.Text = sharpEngineSceneView.GpuDevice.GpuName;
            }
            else
            {
                usedGpuTextLabel.Text = "";
                gpuInfoLabel.Text = "";
                
            }

            //ToolTip.SetTip(SelectedGraphicInfoTextBlock, null);

            _currentSharpEngineSceneView = sharpEngineSceneView;

            //EnableDiagnosticsButton();

            //if (_diagnosticsWindow != null)
            //{
            //    _diagnosticsWindow.SharpEngineSceneView = sharpEngineSceneView;
            //}
            //else if (_automaticallyOpenDiagnosticsWindow)
            //{
            //    OpenDiagnosticsWindow();
            //    _automaticallyOpenDiagnosticsWindow = false;
            //}
        }


        private async void samplesListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (samplesListView.SelectedItems.Count > 0)
                await ShowSelectedSample(samplesListView.SelectedItems[0]);
        }
    }
}
