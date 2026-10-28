using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.CameraControllers;

public class CameraNavigationCirclesSample : CommonSample
{
    private ManualInputEventsManager? _inputEventsManager;
    private CameraNavigationCircles? _cameraNavigationCircles;

    private ICommonSampleUIElement? _fillNegativeCirclesCheckBox;
    private ICommonSampleUIElement? _fillPositiveCirclesCheckBox;
    private ICommonSampleUIElement? _coloredAxisNamesCheckBox;
    private ICommonSampleUIElement? _backgroundCircleVisibilityComboBox;
    private ICommonSampleUIElement? _negativeAxisNameVisibilityComboBox;
    private ICommonSampleUIElement? _negativeAxisCircleVisibilityComboBox;
    private ICommonSampleUIElement? _positiveAxisNameVisibilityComboBox;
    private ICommonSampleUIElement? _positiveAxisCircleVisibilityComboBox;
    private ICommonSampleUIElement? _axisTypeComboBox;

    public override string Title => "CameraNavigationCircles";
    public override string? Subtitle => "CameraNavigationCircles is an overlay control (see bottom left) that can be used to preview the orientation of the camera\nrotate the camera and select the view axis.";

    public CameraNavigationCirclesSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            // Render hash model above the settings
            targetPositionCamera.TargetPosition = new Vector3(0, -50, 0);

            targetPositionCamera.Distance = 400;
        }

        // Create hash symbol
        var hashSymbolMesh = MeshFactory.CreateHashSymbolMesh(centerPosition: new Vector3(0, 0, 0),
                                                              shapeYVector: new Vector3(0, 0, -1),
                                                              extrudeVector: new Vector3(0, 16, 0),
                                                              size: 100,
                                                              barThickness: 16,
                                                              barOffset: 20,
                                                              name: "HashSymbolMesh");

        var hashModel = new Ab4d.SharpEngine.SceneNodes.MeshModelNode(hashSymbolMesh, "HashSymbolModel")
        {
            Material = new StandardMaterial(Color3.FromByteRgb(255, 197, 0)),
            Transform = new StandardTransform()
        };

        scene.RootNode.Add(hashModel);
    }

    protected override void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
        // NOTE:
        // This method gets inputEventsManager that is ManualInputEventsManager and not InputEventsManager.
        // ManualInputEventsManager is a genetic platform-independent object that is defined in Ab4d.SharpEngine.
        // The InputEventsManager is platform specific and a different class is defined for WPF, Avalonia, and other platforms.

        _inputEventsManager = inputEventsManager;

        _cameraNavigationCircles = new CameraNavigationCircles(_inputEventsManager.SceneView, 
                                                               _inputEventsManager, 
                                                               width: 150, height: 150, 
                                                               adjustSizeByDpiScale: true, 
                                                               alignment: PositionTypes.BottomLeft)
        {
            Position = new Vector2(10, 10),
            Alignment = PositionTypes.BottomLeft,

            ModifierKeyForOppositeAxis = KeyboardModifiers.ShiftKey
        };

        // It is also possible to create CameraNavigationCircles without InputEventsManager and only with a SceneView (use another constructor and set inputEventsManager to null),
        // In this case user needs to manually call ProcessPointerMove, ProcessPointerDown and ProcessPointerUp methods.


        // Subscribe events:
        _cameraNavigationCircles.AxisClicked += (sender, args) => LogMessage("AxisClicked: " + args.AxisName);
        _cameraNavigationCircles.AxisSelected += (sender, args) => LogMessage("AxisSelected: " + args.AxisName);
        _cameraNavigationCircles.AxisDeselected += (sender, args) => LogMessage("AxisDeselected: " + args.AxisName);
        _cameraNavigationCircles.BackgroundCircleEnter += (sender, args) => LogMessage("BackgroundCircleEnter");
        _cameraNavigationCircles.BackgroundCircleLeave += (sender, args) => LogMessage("BackgroundCircleLeave");

        base.OnInputEventsManagerInitialized(inputEventsManager);
    }

    private void LogMessage(string message)
    {
        //InfoTextBox.Text = message + "\n" + InfoTextBox.Text;
    }

    private static readonly (CameraNavigationCircles.AxisTypes, CameraNavigationCircles.NavigationCircleVisibility, CameraNavigationCircles.NavigationCircleVisibility, CameraNavigationCircles.NavigationCircleVisibility, CameraNavigationCircles.NavigationCircleVisibility, CameraNavigationCircles.NavigationCircleVisibility, bool, bool, bool)[]
        PresentsArray = new [] {
        (CameraNavigationCircles.AxisTypes.Lines, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, true, false, false),
        (CameraNavigationCircles.AxisTypes.Lines, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, false, true, false),
        (CameraNavigationCircles.AxisTypes.LinesWithArrows, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, true, false, false),
        (CameraNavigationCircles.AxisTypes.Lines, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.Always, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, CameraNavigationCircles.NavigationCircleVisibility.OnPointerOver, true, false, false),
    };

    private void SelectPreset(int selectedIndex)
    {
        var presetValues = PresentsArray[selectedIndex];

        _axisTypeComboBox!.SetValue(presetValues.Item1);
        _positiveAxisCircleVisibilityComboBox!.SetValue(presetValues.Item2);
        _positiveAxisNameVisibilityComboBox!.SetValue(presetValues.Item3);
        _negativeAxisCircleVisibilityComboBox!.SetValue(presetValues.Item4);
        _negativeAxisNameVisibilityComboBox!.SetValue(presetValues.Item5);
        _backgroundCircleVisibilityComboBox!.SetValue(presetValues.Item6);
        
        _coloredAxisNamesCheckBox!.SetValue(presetValues.Item7);
        _fillPositiveCirclesCheckBox!.SetValue(presetValues.Item8);
        _fillNegativeCirclesCheckBox!.SetValue(presetValues.Item9);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        var rootPanel = ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: false, addBorder: false, isSemiTransparent: false);

        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, parent: rootPanel, xMargin: 0);

        ui.CreateLabel("View settings", isHeader: true);

        ui.CreateLabel("Presets:");
        ui.CreateRadioButtons(new string[] { "Default", "Colored circles", "Axis arrows", "3D object" },
            (selectedIndex, selectedText) => SelectPreset(selectedIndex),
            0);
        
        ui.AddSeparator();

        var combBoxWidth = 120;
        var keyTextWidth = 170f;

        _axisTypeComboBox = ui.CreateEnumComboBox(axisType => _cameraNavigationCircles!.AxisType = axisType,
            _cameraNavigationCircles!.AxisType,
            combBoxWidth, "AxisType:", keyTextWidth);
        
        _positiveAxisCircleVisibilityComboBox = ui.CreateEnumComboBox(visibility => _cameraNavigationCircles!.PositiveAxisCircleVisibility = visibility,
            _cameraNavigationCircles!.PositiveAxisCircleVisibility,
            combBoxWidth, "PositiveAxisCircleVisibility:", keyTextWidth);
        
        _positiveAxisNameVisibilityComboBox = ui.CreateEnumComboBox(visibility => _cameraNavigationCircles!.PositiveAxisNameVisibility = visibility,
            _cameraNavigationCircles!.PositiveAxisNameVisibility,
            combBoxWidth, "PositiveAxisNameVisibility:", keyTextWidth);
        
        _negativeAxisCircleVisibilityComboBox = ui.CreateEnumComboBox(visibility => _cameraNavigationCircles!.NegativeAxisCircleVisibility = visibility,
            _cameraNavigationCircles!.NegativeAxisCircleVisibility,
            combBoxWidth, "NegativeAxisCircleVisibility:", keyTextWidth);
        
        _negativeAxisNameVisibilityComboBox = ui.CreateEnumComboBox(visibility => _cameraNavigationCircles!.NegativeAxisNameVisibility = visibility,
            _cameraNavigationCircles!.NegativeAxisNameVisibility,
            combBoxWidth, "NegativeAxisNameVisibility:", keyTextWidth);
        
        _backgroundCircleVisibilityComboBox = ui.CreateEnumComboBox(visibility => _cameraNavigationCircles!.BackgroundCircleVisibility = visibility,
            _cameraNavigationCircles!.BackgroundCircleVisibility,
            combBoxWidth, "BackgroundCircleVisibility:", keyTextWidth);

        _coloredAxisNamesCheckBox = ui.CreateCheckBox("Colored axis names", true, isChecked =>
        {
            if (isChecked)
            {
                // When PositiveAxisNameColor or NegativeAxisNameColor are null, then axis color is used instead of a fixed color.
                _cameraNavigationCircles.PositiveAxisNameColor = null;
                _cameraNavigationCircles.NegativeAxisNameColor = null;
            }
            else
            {
                // Define a fixed color for PositiveAxisNameColor and NegativeAxisNameColor
                _cameraNavigationCircles.PositiveAxisNameColor = Color4.Black;
                _cameraNavigationCircles.NegativeAxisNameColor = Color4.Black;
            }
        });

        _fillPositiveCirclesCheckBox = ui.CreateCheckBox("Fill positive circles", false, isChecked => _cameraNavigationCircles.FillPositiveCircles = isChecked);
        _fillNegativeCirclesCheckBox = ui.CreateCheckBox("Fill negative circles", false, isChecked => _cameraNavigationCircles.FillNegativeCircles = isChecked);

        
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, width: 300, parent: rootPanel, xMargin: 0);

        ui.CreateLabel("Other settings", isHeader: true);

        ui.CreateCheckBox("SelectClosestAxis", true, isChecked => { });
        ui.CreateCheckBox("IsCameraRotationEnabled", true, isChecked => { });
        ui.CreateCheckBox("IsRotateToAxisEnabled", true, isChecked => { });
        ui.CreateCheckBox("ShowOppositeAxisWhenClickedOnCur (?):When checked and when user clicks on an existing axis (the one in the center),\nthen the camera will rotate to the opposite axis.", true, isChecked => { });

        ui.AddSeparator();


        combBoxWidth = 100;
        keyTextWidth = 195;

        ui.CreateEnumComboBox((axisType) => { },
            KeyboardModifiers.ControlKey,
            combBoxWidth, "ModifierKeyToPreserveAttitude:", keyTextWidth);
        
        ui.CreateEnumComboBox((axisType) => { },
            KeyboardModifiers.ShiftKey,
            combBoxWidth, "ModifierKeyForOppositeAxis:", keyTextWidth);
        
        var animationDurationTexts = Enumerable.Range(0, 7).Select(i => $"{i * 0.1f} {(i == 0 ? "(immediate)" : "s")}").ToArray();
        ui.CreateComboBox(animationDurationTexts,
            (selectedIndex, selectedText) => { },
            selectedItemIndex: 4,
            combBoxWidth, "AxisClickAnimationDuration:", keyTextWidth);

        var rotationAmounts = new int[] { 45, 90, 180, 270 };
        ui.CreateComboBox(
            rotationAmounts.Select(v => v.ToString()).ToArray(),
            (selectedIndex, selectedText) => { },
            selectedItemIndex: 2,
            combBoxWidth, "CameraRotationAmount:", keyTextWidth);
        
        var textSizesTexts = Enumerable.Range(0, 6).Select(i => $"{10 + i}").ToArray();
        ui.CreateComboBox(
            textSizesTexts,
            (selectedIndex, selectedText) => { },
            selectedItemIndex: 3,
            combBoxWidth, "AxisTextSize:", keyTextWidth);
        
        var sizes = new int[] { 80, 100, 120, 140 };
        ui.CreateComboBox(
            sizes.Select(v => v.ToString()).ToArray(),
            (selectedIndex, selectedText) => { },
            selectedItemIndex: 2,
            combBoxWidth, "Size:", keyTextWidth);

        ui.AddSeparator();


        ui.CreateButton("Customize axes (Z up)", () => { });
        ui.CreateButton("Toggle background", () => { });

        ui.CreateLabel("Additional customizations are possible by deriving your class from CameraNavigationCircles and overriding virtual methods.").SetStyle("italic");
    }
}