using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.glTF.Schema;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Drawing;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.Vulkan;
using Camera = Ab4d.SharpEngine.Cameras.Camera;

namespace Ab4d.SharpEngine.Samples.Common.Graphs;

public class AxisWithLabelsSamples : CommonSample
{
    public override string Title => "AxisWithLabelsNode";
    //public override string Subtitle => "";
    
    
    private bool _adjustFirstLabelPosition = false;
    private bool _adjustLastLabelPosition = false;
    
    public AxisWithLabelsSamples(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
                var defaultAxis = new AxisWithLabelsNode(axisStartPosition: new Vector3(120, 0, 0), axisEndPosition: new Vector3(120, 100, 0), axisTitle: "Default axis");
        scene.RootNode.Add(defaultAxis);


        var changedValuesRangeAxis = new AxisWithLabelsNode(axisTitle: "Changed range and ticks step")
        {
            AxisStartPosition = new Vector3(60, 0, 0),
            AxisEndPosition = new Vector3(60, 100, 0),
            MinimumValue = -50,
            MaximumValue = 50,
            MajorTicksStep = 10,
            MinorTicksStep = 5
        };

        scene.RootNode.Add(changedValuesRangeAxis);


        var changedTicksAxis = new AxisWithLabelsNode(axisTitle: "Changed display format")
        {
            AxisStartPosition = new Vector3(0, 0, 0),
            AxisEndPosition = new Vector3(0, 100, 0),
            MinimumValue = 0,
            MaximumValue = 100,
            MajorTicksStep = 20,
            MinorTicksStep = 2.5f,             // to hide minor ticks set MinorTicksStep to 0
            ValueDisplayFormatString = "$0.0M" // Change format to always display 2 decimals. Default value is "#,##0".
        };

        // You can also set custom culture to format the values:
        changedTicksAxis.ValueDisplayCulture = System.Globalization.CultureInfo.InvariantCulture;

        scene.RootNode.Add(changedTicksAxis);


        var customValuesLabelsAxis = new AxisWithLabelsNode(axisTitle: "Custom value labels")
        {
            AxisStartPosition = new Vector3(-60, 0, 0),
            AxisEndPosition = new Vector3(-60, 100, 0),
            MinimumValue = 1,
            MaximumValue = 5,
            MajorTicksStep = 1,
            MinorTicksStep = 0, // Hide minor ticks; we could also call: customValuesLabelsAxis.SetCustomMinorTickValues(null);
        };

        // one value label is shown for each major tick
        // So set the same number of string as there is the number of ticks.
        // You can get the count by:
        //var majorTicks = customValuesLabelsAxis.GetMajorTickValues();

        customValuesLabelsAxis.SetCustomValueLabels(new string[] { "lowest", "low", "normal", "high", "highest" });
        customValuesLabelsAxis.SetCustomValueColors(new Color4[] { Colors.DarkBlue, Colors.Blue, Colors.Green, Colors.Orange, Colors.Red });

        scene.RootNode.Add(customValuesLabelsAxis);


        var customValuesAxis = new AxisWithLabelsNode(axisTitle: "Logarithmic scale")
        {
            AxisStartPosition = new Vector3(-120, 0, 0),
            AxisEndPosition = new Vector3(-120, 100, 0),
            MinimumValue = 0,
            MaximumValue = 100,
            MinorTicksStep = 0, // Hide minor ticks
        };

        // Create custom major tick values (this will position the major ticks along the axis)
        // But we will display custom values for each major tick - see below.
        customValuesAxis.SetCustomMajorTickValues(new float[] { 0.0f, 33.3f, 66.6f, 100.0f });
        customValuesAxis.SetCustomValueLabels(new string[] { "1", "10", "100", "1000" });

        // Set minor ticks to show log values from 1 to 10
        var minorValues = new List<float>();
        for (int i = 0; i <= 10; i++)
            minorValues.Add(MathF.Log10(i) * 33.3f); // multiply by 33.3 as this is the "position" of the value 10 on the axis (see code a few lines back)

        customValuesAxis.SetCustomMinorTickValues(minorValues.ToArray());

        scene.RootNode.Add(customValuesAxis);


        var horizontalAxis1 = new AxisWithLabelsNode(axisTitle: "Horizontal axis")
        {
            AxisStartPosition = new Vector3(0, 0, 80),
            AxisEndPosition   = new Vector3(-100, 0, 80),
            RightDirectionVector = new Vector3(0, 0, -1), // RightDirectionVector3 is the direction in which the text is drawn. By default, RightDirectionVector3 points to the right (1, 0, 0). We need to change that because this is also this axis direction.
            IsRenderingOnRightSideOfAxis = true,
        };

        scene.RootNode.Add(horizontalAxis1);

        scene.RootNode.Add(new AxisLineNode());


        // Clone the axis
        var offsetVector = new Vector3(0, 0, 20);

        var horizontalAxis2 = horizontalAxis1.Clone(clonedAxisTitle: "Cloned and flipped horizontal axis");
        horizontalAxis2.AxisStartPosition += offsetVector;
        horizontalAxis2.AxisEndPosition += offsetVector;
        horizontalAxis2.IsRenderingOnRightSideOfAxis = !horizontalAxis1.IsRenderingOnRightSideOfAxis; // flip side on which the ticks and labels are rendered

        scene.RootNode.Add(horizontalAxis2);

        
        var upsideDown = new AxisWithLabelsNode(axisTitle: "Upside down axis")
        {
            AxisStartPosition = new Vector3(160, 100, 0),
            AxisEndPosition = new Vector3(160, 0, 0),
        };

        scene.RootNode.Add(upsideDown);

        
        var defaultAxis2 = new AxisWithLabelsNode(axisTitle: "RS: Default")
        {
            AxisStartPosition = new Vector3(200, 0, 0),
            AxisEndPosition = new Vector3(200, 100, 0),
            IsRenderingOnRightSideOfAxis = true,
        };

        scene.RootNode.Add(defaultAxis2);

        
        var upsideDown2 = new AxisWithLabelsNode(axisTitle: "RS: Upside down axis")
        {
            AxisStartPosition = new Vector3(240, 100, 0),
            AxisEndPosition = new Vector3(240, 0, 0),
            IsRenderingOnRightSideOfAxis = true,
        };

        scene.RootNode.Add(upsideDown2);

        
        UpdateAdjustFirstAndLastLabelPosition();

        // NOTE:
        // Many additional customizations are possible by deriving your class from AxisWithLabelsNode
        // and by overriding the virtual methods. The derived class can also access many protected properties.


        _freeCamera ??= new FreeCamera()
        {
            CameraPosition = new Vector3(0, 100, 500)
        };

        scene.RootNode.ForEachChild<AxisWithLabelsNode>(axisNode =>
        {
            axisNode.Camera = _freeCamera;
        });


        //if (targetPositionCamera != null)
        //{
        //    targetPositionCamera.Heading = 25;
        //    targetPositionCamera.Attitude = -30;
        //    targetPositionCamera.Distance = 430;
        //    targetPositionCamera.TargetPosition = new Vector3(-12, 16, -11);

        //    scene.RootNode.ForEachChild<AxisWithLabelsNode>(axisNode =>
        //    {
        //        axisNode.Camera = targetPositionCamera;
        //    });
        //}
    }
    
    private void UpdateAdjustFirstAndLastLabelPosition()
    {
        Scene?.RootNode.ForEachChild<AxisWithLabelsNode>(axisNode =>
        {
            axisNode.AdjustFirstLabelPosition = _adjustFirstLabelPosition;
            axisNode.AdjustLastLabelPosition = _adjustLastLabelPosition;
        });
    }


    private FreeCamera? _freeCamera;

    protected override Camera OnCreateCamera()
    {
        _freeCamera ??= new FreeCamera()
        {
            CameraPosition = new Vector3(0, 100, 500)
        };

        return _freeCamera;
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("AdjustFirstLabelPosition (?):When checked, then the first label is moved up.\nThis can prevent overlapping the first label with adjacent axis.\nThe amount of movement is calculated by multiplying font size and the LabelAdjustmentFactor (0.45 by default).", 
            _adjustFirstLabelPosition, 
            isChecked =>
            {
                _adjustFirstLabelPosition = isChecked;
                UpdateAdjustFirstAndLastLabelPosition();
            });
        
        ui.CreateCheckBox("AdjustFirstLabelPosition (?):When checked, then the last label is moved down.\nThis can prevent overlapping the last label with adjacent axis.\nThe amount of movement is calculated by multiplying font size and the LabelAdjustmentFactor (0.45 by default).", 
            _adjustLastLabelPosition, 
            isChecked =>
            {
                _adjustLastLabelPosition = isChecked;
                UpdateAdjustFirstAndLastLabelPosition();
            });

        ui.CreateButton("UPDATE", () =>
        {
            Scene?.RootNode.ForEachChild<AxisWithLabelsNode>(axisNode =>
            {
                axisNode.UpdateTextDirections(SceneView.Camera);
            });
        });
    }
}