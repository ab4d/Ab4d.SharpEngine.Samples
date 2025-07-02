using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class SpecialLineRenderingSample : CommonSample
{
    public override string Title => "Special line rendering:";
    public override string Subtitle => "- standard lines are rendered only in front of 3D objects\n- doted lines are rendered only when they are behind 3D objects (hidden lines)\n- always visible lines are always rendered";

    private GroupNode _testModelsGroupNode;

    private StandardMaterial _solidObjectsMaterial;
    private LineMaterial _standardLineMaterial;
    private LineMaterial _hiddenLineMaterial;
    private PolyLineMaterial _standardPolyLineMaterial;
    private PolyLineMaterial _hiddenPolyLineMaterial;

    public SpecialLineRenderingSample(ICommonSamplesContext context)
        : base(context)
    {
        // To render hidden line (line that is rendered only when it is behind other 3D objects), set the IsHiddenLine property to true.
        // Note that hidden line is not rendered when it is in front of 3D objects.
        // Because of this we duplicate each line:
        // - once it is rendered by using standard line material (to render the part that is IN FRONT of 3D objects),
        // - once it is rendered by using hidden line material (to render the part that is BEHINFD 3D objects).
        //
        // To render lines that are always visible (when in front of objects and when behind objects),
        // use always visible lines. Those lines are added to the Scene.OverlayRenderingLayer (OverlayRenderingLayer.ClearDepthStencilBufferBeforeRendering also needs to be set to true).
        // See ChangeLinesVisibility method below and Advanced/BackgroundAndOverlayRenderingSample sample for more info.
        //
        // TO SUMMARIZE:
        // If you want to use different styles for visible and hidden lines then use two LineMaterials (one with IsHiddenLine set to true).
        // If you want to use the same line style for lines that are visible in fron of the objects and behind them, then use OverlayRenderingLayer.
        _standardLineMaterial = new LineMaterial(Colors.Yellow, lineThickness: 5);
        
        _standardPolyLineMaterial = new PolyLineMaterial(Colors.Yellow, lineThickness: 5);

        // Hidden line is thinner and has a line pattern
        _hiddenLineMaterial = new LineMaterial(Colors.Yellow, lineThickness: 1)
        {
            LinePattern = 0x1111,
            IsHiddenLine = true
        };
        
        // Because poly-lines cannot use line patterns, the hidden poly-line is even thinner
        _hiddenPolyLineMaterial = new PolyLineMaterial(Colors.Yellow, lineThickness: 0.5f)
        {
            //LinePattern = 0x1111, // LinePattern is not supported by PolyLineMaterial
            IsHiddenLine = true
        };


        _solidObjectsMaterial = StandardMaterials.Silver;

        _testModelsGroupNode = new GroupNode("TestModels");
    }

    protected override void OnCreateScene(Scene scene)
    {
        var greenBoxModelNode = new BoxModelNode(centerPosition: new Vector3(0, -2.5f, -10), size: new Vector3(100, 4, 180), material: StandardMaterials.Green);
        scene.RootNode.Add(greenBoxModelNode);

        //_testModelsGroupNode = new GroupNode("TestModels"); // Created in constructor so it not nullable
        scene.RootNode.Add(_testModelsGroupNode);


        CreateCylinderWithCircles(new Vector3(0, 0, -5), 10, 30, _solidObjectsMaterial);

        CreateBoxWithEdgeLines(new Vector3(0, 10, 40), new Vector3(20, 20, 20), _solidObjectsMaterial);

        CreateSphereWireframeModel(new Vector3(0, 15, -50), 15, _solidObjectsMaterial);

        
        // Add simple line that goes through the green box
        var standardLineNode = new LineNode(startPosition: new Vector3(-70, -3, 60), endPosition: new Vector3(70, -3, 60), _standardLineMaterial);
        _testModelsGroupNode.Add(standardLineNode);

        var hiddenLineNode = new LineNode(startPosition: new Vector3(-70, -3, 60), endPosition: new Vector3(70, -3, 60), _hiddenLineMaterial);
        _testModelsGroupNode.Add(hiddenLineNode);


        ChangeLinesVisibility(showStandardLines: true, showHiddenLines: true, showAlwaysVisibleLines: false);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 60;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 250;
        }
    }

    private void CreateCylinderWithCircles(Vector3 bottomCenterPosition, float radius, float height, Material material)
    {
        var cylinderModelNode = new CylinderModelNode()
        {
            BottomCenterPosition = bottomCenterPosition,
            Radius = radius,
            Height = height,
            Material = material
        };

        _testModelsGroupNode.Add(cylinderModelNode);


        int circlesCount = 4;
        for (int i = 0; i < circlesCount; i++)
        {
            var centerPosition = bottomCenterPosition + new Vector3(0, height * (float)i / (float)(circlesCount - 1), 0);

            var ellipseLineNode = new EllipseLineNode(_standardPolyLineMaterial)
            {
                CenterPosition = centerPosition,
                Width = radius * 2.05f,
                Height = radius * 2.05f,
                WidthDirection = new Vector3(1, 0, 0),
                HeightDirection = new Vector3(0, 0, 1),
            };

            _testModelsGroupNode.Add(ellipseLineNode);
            
            var hiddenEllipseLineNode = new EllipseLineNode(_hiddenPolyLineMaterial)
            {
                CenterPosition = centerPosition,
                Width = radius * 2.05f,
                Height = radius * 2.05f,
                WidthDirection = new Vector3(1, 0, 0),
                HeightDirection = new Vector3(0, 0, 1),
            };

            _testModelsGroupNode.Add(hiddenEllipseLineNode);
        }
    }

    private void CreateBoxWithEdgeLines(Vector3 centerPosition, Vector3 size, Material material)
    {
        var boxModelNode = new BoxModelNode()
        {
            Position = centerPosition,
            PositionType = PositionTypes.Center,
            Size = size,
            Material = material
        };

        _testModelsGroupNode.Add(boxModelNode);


        var wireBoxNode = new WireBoxNode(_standardLineMaterial)
        {
            Position = centerPosition,
            PositionType = PositionTypes.Center,
            Size = size,
        };

        _testModelsGroupNode.Add(wireBoxNode);
        
        
        var hiddenWireBoxNode = new WireBoxNode(_hiddenLineMaterial)
        {
            Position = centerPosition,
            PositionType = PositionTypes.Center,
            Size = size,
        };

        _testModelsGroupNode.Add(hiddenWireBoxNode);
    }

    private void CreateSphereWireframeModel(Vector3 centerPosition, float radius, Material material)
    {
        var sphereModelNode = new SphereModelNode(centerPosition, radius, material: material)
        {
            Segments = 10
        };

        _testModelsGroupNode.Add(sphereModelNode);


        var sphereWireframePositions = LineUtils.GetWireframeLinePositions(sphereModelNode, removedDuplicateLines: true);

        // After we have all the line positions, we can render the lines by using MultiLineNode
        var wireframeLineNode = new MultiLineNode(sphereWireframePositions, isLineStrip: false, _standardLineMaterial, "WireframeLine");
        _testModelsGroupNode.Add(wireframeLineNode);

        var hiddenWireframeLineNode = new MultiLineNode(sphereWireframePositions, isLineStrip: false, _hiddenLineMaterial, "HiddenWireframeLine");
        _testModelsGroupNode.Add(hiddenWireframeLineNode);
    }

    private void ChangeLinesVisibility(bool showStandardLines, bool showHiddenLines, bool showAlwaysVisibleLines)
    {
        _testModelsGroupNode.ForEachChild<LineBaseNode>(lineNode =>
        {
            if (lineNode.Material == _standardLineMaterial || lineNode.Material == _standardPolyLineMaterial)
            {
                lineNode.Visibility = showStandardLines ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            }
            else if (lineNode.Material == _hiddenLineMaterial || lineNode.Material == _hiddenPolyLineMaterial)
            {
                lineNode.Visibility = showHiddenLines ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            }

            if (Scene != null && Scene.OverlayRenderingLayer != null)
            {
                // To render lines that are always visible, we need to put them to the OverlayRenderingLayer.
                // Before objects from this layer are rendered, the depth buffer is cleared so no previously rendered object will obscure the lines.
                //
                // To do this on ModelNodes or LineNodes, you can use the CustomRenderingLayer property.
                // On GroupNode, you need to call ModelUtils.SetCustomRenderingLayer method to change CustomRenderingLayer property on all child nodes.

                // Before SharpEngine v2.1 we also need to manually set OverlayRenderingLayer.ClearDepthStencilBufferBeforeRendering to true.
                // This will clear the depth-buffer before rendering objects in OverlayRenderingLayer.
                Scene.OverlayRenderingLayer.ClearDepthStencilBufferBeforeRendering = true;

                if (showAlwaysVisibleLines)
                    lineNode.CustomRenderingLayer = Scene.OverlayRenderingLayer;
                else
                    lineNode.CustomRenderingLayer = null; // This will put the line into the LineGeometryRenderingLayer
            }
        });
    }

    private void ChangeLineRenderingType(int renderingTypeIndex)
    {
        bool showStandardLines, showHiddenLines, showAlwaysVisibleLines;

        switch (renderingTypeIndex)
        {
            default:
            case 0: // Standard (visible) lines
                showStandardLines = true;
                showHiddenLines = false;
                showAlwaysVisibleLines = false;
                break;
            
            case 1: // Only hidden lines
                showStandardLines = false;
                showHiddenLines = true;
                showAlwaysVisibleLines = false;
                break;
            
            case 2: // Visible and hidden lines
                showStandardLines = true;
                showHiddenLines = true;
                showAlwaysVisibleLines = false;
                break;
            
            case 3: // Always visible lines
                showStandardLines = true;
                showHiddenLines = true;
                showAlwaysVisibleLines = true;
                break;
        }

        ChangeLinesVisibility(showStandardLines, showHiddenLines, showAlwaysVisibleLines);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Line rendering type:", isHeader: true);
        ui.CreateRadioButtons(new string[]
            {
                "Only standard (visible) lines (?):Render 3D lines without any special settings - only visible line parts will be shown.",
                "Only hidden lines (?): Render 3D lines with using HiddenLineMaterial material - only line parts that are behind 3D objects (hidden) will be shown.",
                "Visible and hidden lines (?): Render 3D lines with standard and hidden line material to show thick visible lines and thin hidden lines.",
                "Always visible lines (?): Render 3D lines that are visible through other 3D objects."
            }, (selectedIndex, selectedText) =>
            {
                ChangeLineRenderingType(selectedIndex);
            }, 
            selectedItemIndex: 2);

        if (Scene != null && Scene.GpuDevice != null && !Scene.GpuDevice.PhysicalDeviceDetails.PossibleFeatures.GeometryShader)
        {
            ui.AddSeparator();
            ui.CreateLabel("Hidden lines are not rendered because GeometryShader is not supported on this GPU!", maxWidth: 200).SetColor(Colors.Red);
        }
    }
}