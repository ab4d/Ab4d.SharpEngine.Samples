using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.OverlayPanels;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class CameraAxisPanelSample : CommonSample
{
    public override string Title => "CameraAxisPanel";

    private TargetPositionCamera? _targetPositionCamera;
    private CameraAxisPanel? _cameraAxisPanel;

    public CameraAxisPanelSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), 
                                        size: new Vector3(80, 40, 60), 
                                        name: "Gold BoxModel")
        {
            Material = StandardMaterials.Gold,
        };

        scene.RootNode.Add(boxModel);
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        CreateCameraAxisPanel();

        base.OnSceneViewInitialized(sceneView);
    }

    protected override void OnDisposed()
    {
        // Calling Dispose on CameraAxisPanel will also remove it from the SceneView
        _cameraAxisPanel?.Dispose();

        base.OnDisposed();
    }

    protected override Camera OnCreateCamera()
    {
        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading = -40,
            Attitude = -25,
            Distance = 300,
            TargetPosition = new Vector3(0, 0, 0),
            ShowCameraLight = ShowCameraLightType.Auto
        };

        return _targetPositionCamera;
    }

    private void CreateCameraAxisPanel()
    {
        if (_cameraAxisPanel != null)
        {
            _cameraAxisPanel.Dispose(); // Remove all resources that are used by an existing CameraAxisPanel
            _cameraAxisPanel = null;
        }

        if (SceneView != null && _targetPositionCamera != null)
        {
            _cameraAxisPanel = new CameraAxisPanel(SceneView, _targetPositionCamera, width: 100, height: 100, _adjustSizeByDpiScale)
            {
                Position = new Vector2(10, 10),
                Alignment = PositionTypes.BottomLeft
            };
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        if (_cameraAxisPanel == null)
            return;

        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("CameraAxisPanel", isHeader: true);

        var positions = new int[] { 0, 10, 20 };
        ui.CreateComboBox(positions.Select(p => $"({p}, {p})").ToArray(), (itemIndex, itemText) =>
        {
            _cameraAxisPanel.Position = new Vector2(positions[itemIndex], positions[itemIndex]);
        }, selectedItemIndex: 1, keyText: "Position:", keyTextWidth: 80, width: 110);

        var alignmentOptions = new PositionTypes[] { PositionTypes.TopLeft, PositionTypes.BottomLeft, PositionTypes.TopRight, PositionTypes.BottomRight };
        ui.CreateComboBox(alignmentOptions.Select(o => o.ToString()).ToArray(), (itemIndex, itemText) =>
        {
            _cameraAxisPanel.Alignment = alignmentOptions[itemIndex];
        }, selectedItemIndex: 1, keyText: "Alignment:", keyTextWidth: 80, width: 110);

        var sizes = new int[] { 80, 100, 150, 200 };
        ui.CreateComboBox(sizes.Select(s => $"{s} x {s}").ToArray(), (itemIndex, itemText) =>
        {
            _cameraAxisPanel.Width  = sizes[itemIndex];
            _cameraAxisPanel.Height = sizes[itemIndex];
        }, selectedItemIndex: 1, keyText: "Size:", keyTextWidth: 80, width: 110);
        
        var backgroundColors = new Color4[] { Colors.Transparent, Colors.White, Colors.Yellow };
        ui.CreateComboBox(backgroundColors.Select(c => c.ToKnownColorString()).ToArray(), (itemIndex, itemText) =>
        {
            _cameraAxisPanel.BackgroundColor = backgroundColors[itemIndex];
        }, selectedItemIndex: 0, keyText: "Background:", keyTextWidth: 80, width: 110);

        ui.AddSeparator();

        ui.CreateCheckBox("ShowAxisNames", _cameraAxisPanel.ShowAxisNames, isChecked => _cameraAxisPanel.ShowAxisNames = isChecked);
        ui.CreateCheckBox("AlignAxisCharsWithCamera", _cameraAxisPanel.AlignAxisCharsWithCamera, isChecked => _cameraAxisPanel.AlignAxisCharsWithCamera = isChecked);
        ui.CreateCheckBox("AdjustSizeByDpiScale", _adjustSizeByDpiScale, isChecked =>
        {
            _adjustSizeByDpiScale = isChecked;
            CreateCameraAxisPanel();
        });
    }

    private bool _adjustSizeByDpiScale = true;
}