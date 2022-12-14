using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using Ab4d.Assimp;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Wpf;
using static System.Formats.Asn1.AsnWriter;

namespace Ab4d.SharpEngine.Samples.Wpf.TestScenes
{
    /// <summary>
    /// Interaction logic for AssimpImporterTestScene.xaml
    /// </summary>
    public partial class AssimpImporterTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;
        
        private AssimpImporter? _assimpImporter;

        private float _lineThickness;
        private float _wireframeDepthBias = 0.0005f;

        private LineMaterial? _wireframeLineMaterial;
        private MultiLineNode? _wireframeLineNode;
        private SceneNode? _importedModelNodes;

        public AssimpImporterTestScene()
        {
            InitializeComponent();

            LineThicknessComboBox.ItemsSource = new float[] { 0.1f, 0.2f, 0.5f, 1f, 2f };
            LineThicknessComboBox.SelectedIndex = 3;
            _lineThickness = 1f;


            var dragAndDropHelper = new DragAndDropHelper(this, ".*");
            dragAndDropHelper.FileDropped += (sender, args) => ImportFile(args.FileName);


            _bitmapIO = new WpfBitmapIO(); // _bitmapIO provides a cross-platform way to read bitmaps (it uses WPF as backend)

            // Setup logger
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company


            // MainSceneView is defined in XAML

            // Set EnableStandardValidation to true, but the Vulkan validation will be enabled only when the Vulkan SDK is installed on the system.
            MainSceneView.CreateOptions.EnableStandardValidation = true;

            MainSceneView.SceneViewCreated += delegate(object sender, SceneViewCreatedEventArgs args)
            {
                _scene     = args.Scene;
                _sceneView = args.SceneView;

                InitAssimpLibrary();

                SetupMouseCameraController();

                string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\duck.dae");
                ImportFile(fileName);
            };

            Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                MainSceneView.Dispose();
            };
        }


        private void InitAssimpLibrary()
        {
            if (_scene == null || _scene.GpuDevice == null || _bitmapIO == null)
                return;

            // Load native Assimp importer library
            string assimpLibPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assimp-lib" + System.IO.Path.DirectorySeparatorChar);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                assimpLibPath += Environment.Is64BitProcess ? "win-x64\\Assimp64.dll" : "win-x86\\Assimp32.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.Is64BitProcess)
            {
                assimpLibPath += "linux-x64/libassimp.so.5";
            }


            if (!System.IO.File.Exists(assimpLibPath))
                throw new NotSupportedException("No supported Assimp library available");


            try
            {
                AssimpLibrary.Instance.Initialize(assimpLibPath, throwException: true);
            }
            catch (Exception ex)
            {
                var errorTextBlock = new TextBlock()
                {
                    Foreground = Brushes.Red,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                errorTextBlock.Inlines.Add(new Run(
@$"Error loading native Assimp library:
{ex.Message}

The most common cause of this error is that the Visual C++ Redistributable for Visual Studio 2019 is not installed on the system. 
See the following web page for more info:
"));

                var hyperlink = new Hyperlink()
                {
                    NavigateUri = new Uri("https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170")
                };

                hyperlink.Inlines.Add(new Run("Microsoft Visual C++ Redistributable latest supported downloads"));
                hyperlink.RequestNavigate += delegate(object sender, RequestNavigateEventArgs args)
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri) { UseShellExecute = true });
                    args.Handled = true;
                };

                errorTextBlock.Inlines.Add(hyperlink);

                RootGrid.Children.Add(errorTextBlock);

                LoadButton.IsEnabled = false;

                return;
            }

            if (!AssimpLibrary.Instance.IsInitialized)
                throw new Exception("Cannot initialize native Assimp library");

            _assimpImporter = new AssimpImporter(_scene.GpuDevice, _bitmapIO);


            string allImportFileFormats = string.Join(", ", _assimpImporter.SupportedImportFileExtensions);
            FileFormatsTextBlock.Text = $"Using native Assimp library version {_assimpImporter.AssimpVersionString}.\r\n\r\nSupported import formats:\r\n{allImportFileFormats}";
        }

        private void ImportFile(string fileName)
        {
            if (_scene == null || _assimpImporter == null)
                return;

            _scene.RootNode.Clear();
            _importedModelNodes = null;
            ShowInfoButton.IsEnabled = false;

            string fileExtension = System.IO.Path.GetExtension(fileName);
            if (!_assimpImporter.IsImportFormatSupported(fileExtension))
            {
                MessageBox.Show("Assimp does not support importing files file extension: " + fileExtension);
                return;
            }


            // FixDirectorySeparator method returns file path with correctly sets backslash or slash as directory separator based on the current OS.
            fileName = Ab4d.SharpEngine.Utilities.FileUtils.FixDirectorySeparator(fileName);

            _importedModelNodes = _assimpImporter.ImportSceneNodes(fileName);

            if (_importedModelNodes != null)
            {
                _scene.RootNode.Add(_importedModelNodes);


                var wireframePositions = LineUtils.GetWireframeLinePositions(_importedModelNodes, removedDuplicateLines: false); // remove duplicates can take some time for bigger models

                _wireframeLineMaterial = new LineMaterial(Color3.Black, _lineThickness)
                {
                    DepthBias = _wireframeDepthBias
                };

                _wireframeLineNode = new MultiLineNode(wireframePositions, isLineStrip: false, _wireframeLineMaterial, "Wireframe");
                UpdateWireframeVisibility();

                _scene.RootNode.Add(_wireframeLineNode);


                if (_importedModelNodes.WorldBoundingBox.IsEmpty)
                    _importedModelNodes.Update();

                if (_targetPositionCamera != null && !_importedModelNodes.WorldBoundingBox.IsEmpty)
                {
                    _targetPositionCamera.TargetPosition = _importedModelNodes.WorldBoundingBox.GetCenterPosition();
                    _targetPositionCamera.Distance = _importedModelNodes.WorldBoundingBox.GetDiagonalLength() * 2;
                }

                ShowInfoButton.IsEnabled = true;
            }
        }

        private void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            openFileDialog.Filter = "3D model file (*.*)|*.*";
            openFileDialog.Title = "Open 3D model file file";

            if ((openFileDialog.ShowDialog() ?? false) && !string.IsNullOrEmpty(openFileDialog.FileName))
                ImportFile(openFileDialog.FileName);
        }

        private void UpdateWireframeVisibility()
        {
            if (_wireframeLineNode == null)
                return;

            _wireframeLineNode.Visibility = (ShowWireframeCheckBox.IsChecked ?? false) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }

        private void OnShowWireframeCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateWireframeVisibility();
        }

        private void LineThicknessComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_wireframeLineMaterial == null)
                return;

            _lineThickness = (float)LineThicknessComboBox.SelectedItem;
            _wireframeLineMaterial.LineThickness = _lineThickness;
        }

        private void ShowInfoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_scene == null || _importedModelNodes == null)
                return;

            string objectInfo = _scene.GetSceneNodesDumpString(showChildNodeIndex: false, 
                                                               showWorldBoundingBox: false, 
                                                               showLocalBoundingBox: false, 
                                                               showMeshInfo: true, 
                                                               showMaterialInfo: true, 
                                                               showTransform: true, 
                                                               showDirtyFlags: false, 
                                                               showLastFrameDirtyFlags: false,
                                                               showStatistics: true,
                                                               showAllEffects: false,
                                                               showGpuHandles: false);


            var textBox = new TextBox()
            {
                Margin = new Thickness(10, 10, 10, 10),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                Text = objectInfo
            };

            var window = new Window()
            {
                Title = "3D Object info"
            };

            window.Content = textBox;
            window.Show();
        }



        private void SetupMouseCameraController()
        {
            if (MainSceneView == null || MainSceneView.SceneView == null)
                return;


            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 1200,
                TargetPosition = new Vector3(0, 0, -150),
                ShowCameraLight = ShowCameraLightType.Auto
            };

            MainSceneView.SceneView.Camera = _targetPositionCamera;


            _mouseCameraController = new MouseCameraController(MainSceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,                                                   // this is already the default value but is still set up here for clarity
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
                QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.MousePosition,
                RotateAroundMousePosition = true
            };
        }
    }
}
