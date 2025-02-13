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
        private string? _startupPage = "Titles.IntroductionUserControl";        
        //private string? _startupPage = null;

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
            var xmlDcoument = new XmlDocument();
            xmlDcoument.Load(@"Samples.xml");

            if (xmlDcoument.DocumentElement == null)
                throw new Exception("Cannot load Samples.xml");

            var xmlNodeList = xmlDcoument.DocumentElement.SelectNodes("/Samples/Sample");

            if (xmlNodeList == null || xmlNodeList.Count == 0)
                throw new Exception("No samples in Samples.xml");


            samplesListView.Columns.Add(new ColumnHeader() { Width = samplesListView.Width - 4 - SystemInformation.VerticalScrollBarWidth });
            samplesListView.HeaderStyle = ColumnHeaderStyle.None;

            if (_headerItemFont == null)
                _headerItemFont = new Font(samplesListView.Font, FontStyle.Bold);

            ListViewGroup? currentListViewGroup = null;

            foreach (XmlNode xmlNode in xmlNodeList)
            {
                try
                {
                    var listBoxItem = CreateListBoxItem(xmlNode, ref currentListViewGroup);

                    if (listBoxItem != null)
                        samplesListView.Items.Add(listBoxItem);
                }
                catch
                {
                    Debug.WriteLine("Error parsing sample xml for " + xmlNode.OuterXml);
                }
            }
        }

        private ListViewItem? CreateListBoxItem(XmlNode xmlNode, ref ListViewGroup? currentListViewGroup)
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
                return null; // WinForms do not support ListView with items that have custom margins



            if (!isTitle)
                title = "  " + title;

            var listBoxItem = new ListViewItem(title)
            {
                Tag = location,
            };

            if (isTitle)
                listBoxItem.Font = _headerItemFont;

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

            var location = selectedListViewItem.Tag as string;

            if (location == null || location == _currentSampleLocationText) 
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

            if (location.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var markdownText = GetMarkdownText(location);
                    
                if (markdownText != null)
                {
                    var markdownHtml = Markdig.Markdown.ToHtml(markdownText);

                    // Set default font
                    markdownHtml = "<html><body style='font-family: sans-serif; margin: 20pt'>" +
                                   markdownHtml +
                                   "</body></html>";

                    // It is not possible to show local image in WebView2, so we need to replace the local file name with http address:
                    markdownHtml = markdownHtml.Replace("img src=\"Resources/CadImporter-for-SharpEngine.png", "img height='400px' src=\"https://www.ab4d.com/Images/CadImporter/CadImporter_0_1.png");

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
                // Try to create common sample type from page attribute
                var sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.WinForms.{location}, Ab4d.SharpEngine.Samples.WinForms", throwOnError: false);

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
                        _currentSampleLocationText = null;
                        MessageBox.Show("No constructor without parameters or with ICommonSamplesContext found for the sample:" + Environment.NewLine + location);
                        return;
                    }

                    if (isCommonSampleType)
                    {
                        // Create a common sample control (calling constructor that takes ICommonSamplesContext as parameter)

                        var commonSamplesContext = WinFormsSamplesContext.Current;

                        //var commonSample = Activator.CreateInstance(sampleType, new object?[] { commonSamplesContext }) as CommonSample;
                        var commonSample = selectedConstructorInfo.Invoke(new object?[] { commonSamplesContext }) as CommonSample;

                        if (_commonWinFormsSampleUserControl == null)
                        {
                            _commonWinFormsSampleUserControl = new CommonWinFormsSampleUserControl();
                            _commonWinFormsSampleUserControl.Dock = DockStyle.Fill;
                        }

                        // Set CurrentSharpEngineSceneView before the _commonWinFormsSampleUserControl is loaded
                        WinFormsSamplesContext.Current.RegisterCurrentSharpEngineSceneView(_commonWinFormsSampleUserControl.MainSceneView);

                        this.SuspendLayout();
                        _commonWinFormsSampleUserControl.CurrentCommonSample = commonSample;
                        this.ResumeLayout();

                        _currentSampleControl = _commonWinFormsSampleUserControl;

                        _currentCommonSample = commonSample;
                    }
                    else
                    {
                        // Create sample control (calling constructor without parameters)
                        _currentSampleControl = selectedConstructorInfo.Invoke(null) as UserControl;
                        
                        if (_currentSampleControl != null)
                            _currentSampleControl.Dock = DockStyle.Fill;

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
                _currentSampleLocationText = null;
                MessageBox.Show("Sample not found: " + Environment.NewLine + location);
                return;
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

            _currentSampleLocationText = location;
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

        //private SharpEngineSceneView? FindSharpEngineSceneView(ControlCollection controls)
        //{


        //    return null;
        //}


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
