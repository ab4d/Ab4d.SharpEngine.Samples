using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Wpf;
using Ab4d.Vulkan;
using Cyotek.Drawing.BitmapFont;
using Page = System.Windows.Controls.Page;

namespace Ab4d.SharpEngine.Samples.Wpf.TestScenes
{
    /// <summary>
    /// Interaction logic for HitTestingTestScene.xaml
    /// </summary>
    public partial class HitTestingTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public enum SceneTypes
        {
            SingleBox = 0,
            Teapot,
            MultipleGeometryModel3Ds,
            InstancedMeshObjects,
            InstancedModel3DGroupObjects,
            Model3DGroups,
            MeshObjectNodes,
        }

        private const int MaxShownHitTestResults = 40;

        private GroupNode? _testObjectsGroup;
        private GroupNode? _hitLinesGroup;

        private bool _getOnlyFrontFacingTriangles;

        private StandardMaterial _backMaterial;
        private SceneTypes _currentSceneType;

        private ArrowModelNode? _mouseArrowNode;
        private LineMaterial? _hitResultLineMaterial;

        private List<WireCrossNode>? _wireCrosses;
        private List<LineNode>? _lineNodes;
        private int _wireCrossIndex;

        public HitTestingTestScene()
        {
            InitializeComponent();

            _bitmapIO = new WpfBitmapIO(); // _bitmapIO provides a cross-platform way to read bitmaps (it uses WPF as backend)

            // Setup logger
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company


            _backMaterial = StandardMaterials.Black;


            // MainSceneView is defined in XAML

            // Set EnableStandardValidation to true, but the Vulkan validation will be enabled only when the Vulkan SDK is installed on the system.
            MainSceneView.CreateOptions.EnableStandardValidation = true;

            MainSceneView.SceneViewInitialized += delegate (object? o, EventArgs args)
            {
                _scene = MainSceneView.Scene;
                _sceneView = MainSceneView.SceneView;

                if (_scene == null || _sceneView == null)
                    return; // This should not happen in SceneViewInitialized

                SetupMouseCameraController();
                CreateLights();

                CreateTestScene(_scene);
            };

            //MainSceneView.SceneUpdating += delegate(object? sender, EventArgs args)
            //{
            //    // Do animations
            //};

            Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                MainSceneView.Dispose();
            };
        }

        private void SetupMouseCameraController()
        {
            if (MainSceneView == null || MainSceneView.SceneView == null)
                return;


            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 800,
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

        private void CreateTestScene(Scene scene)
        {
            _testObjectsGroup = new GroupNode("TestObjectsGroup");
            _hitLinesGroup = new GroupNode("HitLinesGroup");

            scene.RootNode.Add(_testObjectsGroup);
            scene.RootNode.Add(_hitLinesGroup);

            CreateSceneObjects(SceneTypes.Teapot);


            _mouseArrowNode = new ArrowModelNode("MouseRayArrow")
            {
                StartPosition = new Vector3(0, 0, 0),
                EndPosition = new Vector3(0, 0, 0),
                Radius = 1,
                Material = StandardMaterials.Gold
            };

            scene.RootNode.Add(_mouseArrowNode);


            MouseMove += OnMouseMove;
        }

        private void CreateSceneObjects(SceneTypes sceneType)
        {
            if (_sceneView == null)
                return;

            if (_hitLinesGroup != null)
                _hitLinesGroup.Clear();

            if (_testObjectsGroup != null)
                _testObjectsGroup.Clear();

            // Reset camera if it is not changed in CreateSceneObjects
            if (_sceneView.Camera is TargetPositionCamera targetPositionCamera)
            {
                targetPositionCamera.Heading = 30;
                targetPositionCamera.Attitude = -10;
                targetPositionCamera.Distance = 500;
                targetPositionCamera.ViewWidth = 400;
                targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);

                targetPositionCamera.NearPlaneDistance = 0.125f;
                targetPositionCamera.FarPlaneDistance = 1000f;
            }



            switch (sceneType)
            {
                case SceneTypes.SingleBox:
                    ShowBox();
                    break;

                case SceneTypes.Teapot:
                    ShowTeapot();
                    break;

                //case SceneTypes.MultipleGeometryModel3Ds:
                //    //AddMultipleTestObjects(new Point3D(0,0,0), new Size3D(200, 100, 200), 10, 5, 10, 90);
                //    //AddMultipleTestObjects(new Point3D(0,0,0), new Size3D(200, 200, 200), 20, 20, 20, 4);
                //    //AddMultipleTestObjects(new Point3D(0,0,0), new Size3D(200, 200, 200), 5, 5, 5, 4);
                //    AddMultipleTestObjects(new Point3D(0, 0, 0), new Size3D(200, 200, 200), 5, 5, 5, segmentsCount: 50);
                //    break;

                //case SceneTypes.InstancedMeshObjects:
                //    AddInstancedTestObjects(new Point3D(0, 0, 0), new Size3D(200, 200, 200), 5, 5, 5, segmentsCount: 20, useModel3DGroupInstances: false);
                //    break;

                //case SceneTypes.InstancedModel3DGroupObjects:
                //    AddInstancedTestObjects(new Point3D(0, 0, 0), new Size3D(200, 200, 200), 3, 3, 3, segmentsCount: 20, useModel3DGroupInstances: true);
                //    break;

                //case SceneTypes.Model3DGroups:
                //    MeshGeometry3D sphereMeshGeometry3D = new Ab3d.Meshes.SphereMesh3D(centerPosition: new Point3D(0, 0, 0), radius: 5, segments: 50, generateTextureCoordinates: false).Geometry;
                //    AddModel3DGroups(sphereMeshGeometry3D);
                //    break;

                //case SceneTypes.MeshObjectNodes:
                //    AddMeshObjectNodes();
                //    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneType), sceneType, null);
            }

            if (_testObjectsGroup != null)
            {
                _testObjectsGroup.ForEachChild(delegate (ModelNode node)
                {
                    node.BackMaterial = _getOnlyFrontFacingTriangles ? null : _backMaterial;
                });
            }

            _currentSceneType = sceneType;
        }

        private void OnMouseMove(object sender, MouseEventArgs args)
        {
            if (_sceneView == null)
                return;

            //if (_mouseArrowNode == null || (_lastMouseX == args.X && _lastMouseY == args.Y)) return;

            //var ray = sceneView.GetRayFromCamera(args.X, args.Y, adjustForDpiScale: true, adjustForSupersamplingFactor: true);

            //_lastMouseX = args.X;
            //_lastMouseY = args.Y;

            //var hitTestResult = scene.GetClosestHitObject(ray);

            //_mouseArrowNode.StartPosition = ray.Position;
            //_mouseArrowNode.EndPosition = ray.Direction * 500;

            //System.Diagnostics.Debug.WriteLine($"MousePos: {args.X} {args.Y} => {args.X * sceneView.DpiScaleX} {args.Y * sceneView.DpiScaleY} => {args.X * 100.0 * sceneView.DpiScaleX / sceneView.Width}% {args.Y * 100.0 * sceneView.DpiScaleY / sceneView.Height}%");

            var mousePosition = args.GetPosition(MainSceneView);
            var hitTestResult = _sceneView.GetClosestHitObject((float)mousePosition.X, (float)mousePosition.Y);

            if (hitTestResult != null)
            {
                AddWireCross(hitTestResult.HitPosition);
            }
        }

        public void ShowBox()
        {
            if (_sceneView == null || _testObjectsGroup == null)
                return;

            _testObjectsGroup.Clear();

            var boxVisual3D = new BoxModelNode("SilverBox")
            {
                Position = new Vector3(0, 0, 0),
                Size = new Vector3(40, 20, 40),
                Material = StandardMaterials.Silver,
                BackMaterial = StandardMaterials.Black,
                UseSharedBoxMesh = false
            };

            boxVisual3D.Transform = new TranslateTransform(0, 10, 0);


            _testObjectsGroup.Add(boxVisual3D);


            if (_sceneView.Camera is TargetPositionCamera targetPositionCamera)
            {
                targetPositionCamera.Distance = 200;
                targetPositionCamera.Heading = 30;
                targetPositionCamera.Attitude = -50;
            }
        }

        public void ShowTeapot()
        {
            if (_testObjectsGroup == null)
                return;

            _testObjectsGroup.Clear();

            //_testObjectsModelVisual3D.Children.Clear();


            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Teapot.obj");
            //fileName = FileUtils.FixDirectorySeparator(fileName);

            var readerObj = new ReaderObj();
            var readObjModelNode = readerObj.ReadSceneNodes(fileName);

            SceneNodeUtils.PositionAndScaleSceneNode(readObjModelNode,
                                                     position: new Vector3(0, -20, 0),
                                                     positionType: PositionTypes.Center,
                                                     finalSize: new Vector3(300, 200, 300));

            _testObjectsGroup.Add(readObjModelNode);
        }

        public void HitTestCenter()
        {
            if (_sceneView == null)
                return;

            var viewCenter = new Vector2(_sceneView.Width / 2.0f, _sceneView.Height / 2.0f);

            if (_hitLinesGroup != null)
                _hitLinesGroup.Clear();

            HitTest(viewCenter, displayHitResults: true, showPossibleBoundingBoxes: true);
        }

        private void HitTest(Vector2 viewPosition, bool displayHitResults, bool showPossibleBoundingBoxes)
        {
            if (_sceneView == null || _scene == null)
                return;

            //var ray = sceneView.GetRayFromNearPlane(viewPosition.X, viewPosition.Y, adjustForDpiScale: true, adjustForSupersamplingFactor: true);
            var ray = _sceneView.GetRayFromCamera(viewPosition.X, viewPosition.Y, adjustForDpiScale: false, adjustForSupersamplingFactor: true);

            var hitTestResult = _scene.GetClosestHitObject(ray);

            if (hitTestResult != null)
            {
                if (displayHitResults)
                {
                    if (_hitLinesGroup != null)
                    {
                        _hitLinesGroup.Clear();

                        var arrowModelNode = new ArrowModelNode("HitResultArrow")
                        {
                            StartPosition = ray.Position,
                            EndPosition = ray.Direction * hitTestResult.DistanceToRayOrigin,
                            //EndPosition = hitTestResult.HitPosition,
                            Radius = 3,
                            Material = StandardMaterials.Red
                        };

                        _hitLinesGroup.Add(arrowModelNode);
                    }


                    string hitSceneNodeInfo;
                    if (hitTestResult.HitSceneNode != null)
                        hitSceneNodeInfo = string.Format("SceneNode Id {0} ('{1}')", hitTestResult.HitSceneNode.Id, hitTestResult.HitSceneNode.Name ?? "<null>");
                    else
                        hitSceneNodeInfo = "SceneNode: <null>";

                    InfoTextBox.Text += string.Format("DXScene hit test result (Ray.Start: {0:0.0}; Ray.Direction: {1:0.00}):\r\n  PointHit: {2:0.0};   (distance: {3:0})\r\n  {4}; TriangleIndex: {5}\r\n\r\n",
                                                        ray.Position,
                                                        ray.Direction,
                                                        hitTestResult.HitPosition,
                                                        hitTestResult.DistanceToRayOrigin,
                                                        hitSceneNodeInfo,
                                                        hitTestResult.TriangleIndex);

                    InfoTextBox.ScrollToEnd();
                }
            }
            else
            {
                if (displayHitResults)
                {
                    InfoTextBox.Text += "No hit\r\n";
                    InfoTextBox.ScrollToEnd();
                }
            }
        }

        public void GetOnlyFrontFacingTriangles(bool newValue)
        {
            _getOnlyFrontFacingTriangles = newValue;
        }

        private void AddWireCross(Vector3 position)
        {
            if (_sceneView == null || _scene == null)
                return;

            _wireCrosses ??= new List<WireCrossNode>();

            if (_hitResultLineMaterial == null)
                _hitResultLineMaterial = new LineMaterial(Colors.Gold, 1);

            WireCrossNode wireCrossNode;

            if (_wireCrossIndex < MaxShownHitTestResults)
            {
                wireCrossNode = new WireCrossNode(position, 10, _hitResultLineMaterial);

                _wireCrosses.Add(wireCrossNode);
                _scene.RootNode.Add(wireCrossNode);
            }
            else
            {
                int reusedIndex = _wireCrossIndex % MaxShownHitTestResults;
                wireCrossNode = _wireCrosses[reusedIndex];

                wireCrossNode.Position = position;
            }


            _lineNodes ??= new List<LineNode>();

            var cameraPosition = _sceneView.Camera?.GetCameraPosition() ?? new Vector3();

            LineNode lineNode;

            if (_wireCrossIndex < MaxShownHitTestResults)
            {
                lineNode = new LineNode(cameraPosition, position, Colors.Red, 1);
                lineNode.EndLineCap = LineCap.ArrowAnchor;

                _lineNodes.Add(lineNode);
                _scene.RootNode.Add(lineNode);
            }
            else
            {
                int reusedIndex = _wireCrossIndex % MaxShownHitTestResults;
                lineNode = _lineNodes[reusedIndex];

                lineNode.StartPosition = cameraPosition;
                lineNode.EndPosition = position;
            }


            _wireCrossIndex++;
        }

        private void CreateLights()
        {
            if (_scene == null)
                return;


            _scene.Lights.Clear();

            // Add lights
            _scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));

            var directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
            _scene.Lights.Add(directionalLight);

            _scene.Lights.Add(new PointLight(new Vector3(500, 200, 100), range: 10000));
        }

    }
}
