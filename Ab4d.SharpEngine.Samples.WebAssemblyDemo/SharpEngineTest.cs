using Ab4d.SharpEngine.Browser;
using System;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.WebAssemblyDemo;

public class SharpEngineTest
{
    private ICanvasInterop? _canvasInterop;
    private Ab4d.SharpEngine.WebGL.WebGLDevice? _webGlDevice;
    private Scene? _scene;
    private SceneView? _sceneView;

    private StandardMaterial? _hashMaterial;
    private int _addedObjectsGroupIndex;
    private int _totalAddedObjectsCount;

    public static SharpEngineTest Instance = new SharpEngineTest();

    private SharpEngineTest()
    {
		// Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
		// To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
		Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
											  licenseType: "SamplesLicense",
											  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");
    }

    // The following method can be called from BlazorTesterApp project
    public void InitSharpEngine(ICanvasInterop canvasInterop)
    {
        Log("InitSharpEngine from WebAssemblyDemo called");

        _canvasInterop = canvasInterop;

        var gpuDevice = Ab4d.SharpEngine.WebGL.WebGLDevice.Create(canvasInterop);

        if (!gpuDevice.IsInitialized)
        {
            Log("ERROR: WebGLDevice is not initialized");
            return;
        }

        _webGlDevice = gpuDevice;

        try
        {
            Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Trace;
            Ab4d.SharpEngine.Utilities.Log.IsLoggingToConsole = true;

            _scene = new Scene(gpuDevice, "MainScene");         // Create Scene object and also initialize it with the gpuDevice.
            _sceneView = new SceneView(_scene, "MainSceneView");


            float hashModelSize = 100;
            float hashModelBarThickness = 16;
            float hashModelBarOffset = 20;

            var hashSymbolMesh = MeshFactory.CreateHashSymbolMesh(centerPosition: new Vector3(0, hashModelBarThickness * 0.5f, 0),
                                                                  shapeYVector: new Vector3(0, 0, 1),
                                                                  extrudeVector: new Vector3(0, hashModelBarThickness, 0),
                                                                  size: hashModelSize,
                                                                  barThickness: hashModelBarThickness,
                                                                  barOffset: hashModelBarOffset,
                                                                  name: "HashSymbolMesh");

            _hashMaterial = new StandardMaterial(diffuseColor: Color3.FromByteRgb(255, 197, 0));

            var hashSymbolNode = new MeshModelNode(hashSymbolMesh, _hashMaterial, "HashSymbolNode");
            _scene.RootNode.Add(hashSymbolNode);


            var wireGridNode = new WireGridNode("Wire grid")
            {
                CenterPosition = new Vector3(0, -0.5f, 0),
                Size = new Vector2(200, 200),

                WidthDirection = new Vector3(1, 0, 0),   // this is also the default value
                HeightDirection = new Vector3(0, 0, -1), // this is also the default value

                WidthCellsCount = 20,
                HeightCellsCount = 20,

                MajorLineColor = Colors.Black,
                MajorLineThickness = 1,

                MinorLineColor = Colors.Gray,
                MinorLineThickness = 1,

                MajorLinesFrequency = 5,

                IsClosed = true,
            };

            _scene.RootNode.Add(wireGridNode);


            _sceneView.BackgroundColor = Colors.LightSkyBlue;

            var camera = new TargetPositionCamera()
            {
                Heading = 30,
                Attitude = -25,
                Distance = 400,
                ShowCameraLight = ShowCameraLightType.Always
            };

            _sceneView.Camera = camera;

            _scene.SetAmbientLight(0.4f);

            var pointerCameraController = new PointerCameraController(_sceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,
                ZoomMode = CameraZoomMode.PointerPosition,
                RotateAroundPointerPosition = false,
                IsPinchZoomEnabled = true, // zoom with touch pinch gesture
                IsPinchMoveEnabled = true  // move camera with two fingers
            };

            //_sceneView.Render();
        
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void ChangeMaterial(string? colorText)
    {
        if (_hashMaterial == null)
            return;

        Color3 newColor;

        if (string.IsNullOrEmpty(colorText) || !Color3.TryParse(colorText, out newColor))
            newColor = Color3.FromHsl(Random.Shared.NextSingle() * 360); // if colorText was not set or we cannot parse it, create a random color

        _hashMaterial.DiffuseColor = newColor;

        Log($"Hash color changed to {newColor.ToKnownColorString()}"); // this will display a known color (e.g. "blue") or hex value of the color ("#0000FF") if color is not known.
    }

    public void ToggleCameraRotation()
    {
        if (_sceneView == null || _sceneView.Camera is not TargetPositionCamera targetPositionCamera)
            return;

        if (targetPositionCamera.IsRotating)
        {
            targetPositionCamera.StopRotation();
            Log("Camera rotation stopped");
        }
        else
        {
            targetPositionCamera.StartRotation(40);
            Log("Camera rotation started");
        }
    }

    public void AddObjects(int objectsCount)
    {
        if (_scene == null)
            return;

        _addedObjectsGroupIndex++; // start with 1
        var groupNode = new GroupNode($"AddedObjectsGroup_{_addedObjectsGroupIndex}");
        _scene.RootNode.Add(groupNode);

        var material = StandardMaterials.LightGreen;

        for (int i = 0; i < objectsCount; i++)
        {
            var boxModelNode = new BoxModelNode(centerPosition: new Vector3(_addedObjectsGroupIndex * 30 - 130, -30, -50 + i * 15),
                size: new Vector3(10, 10, 10),
                material,
                name: $"Box_{_addedObjectsGroupIndex + 1}_{i + 1}");

            groupNode.Add(boxModelNode);
        }

        _totalAddedObjectsCount += objectsCount;

        Log($"Added {objectsCount} boxes (total {_totalAddedObjectsCount} objects added)");
    }
    
    private void Log(string message)
    {
        Console.WriteLine("SharpEngineTest: " + message);

        // Call JavaScript showInfoText function to change the Info text on the web page
        _ = JavaScriptInterop.ShowInfoText(message); // Call async method from sync context
    }
}