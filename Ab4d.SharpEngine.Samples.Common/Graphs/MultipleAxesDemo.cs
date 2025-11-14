using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Graphs;

public class MultipleAxesDemo : CommonSample
{
    public override string Title => "Multiple axes demo";
    public override string Subtitle => "This sample shows multiple AxisWithLabelsNode objects";
    
    private bool _isUpdatingOnCameraChange = true;
    private ICommonSampleUIElement? _updatingButton;

    public MultipleAxesDemo(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var defaultAxis = new AxisWithLabelsNode(axisStartPosition: new Vector3(120, 0, 0),
                                                 axisEndPosition: new Vector3(120, 100, 0),
                                                 targetPositionCamera,
                                                 axisTitle: "Default axis");

        //var defaultAxis = new AxisWithLabelsNode(axisStartPosition: new Vector3(120, 0, 0), 
        //                                         axisEndPosition: new Vector3(120, 100, 0), 
        //                                         axisTitle: "Default axis");

        scene.RootNode.Add(defaultAxis);

       
        var changedValuesRangeAxis = new AxisWithLabelsNode(axisTitle: "Changed range and ticks step")
        {
            AxisStartPosition = new Vector3(60, 0, 0),
            AxisEndPosition = new Vector3(60, 100, 0),
            MinimumValue = -50,
            MaximumValue = 50,
            MajorTicksStep = 10,
            MinorTicksStep = 5,

            // When camera is assigned, then AxisWithLabelsNode will automatically update the text direction based on the current camera angle
            Camera = targetPositionCamera
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
            ValueDisplayFormatString = "$0.0M", // Change format to always display 2 decimals. Default value is "#,##0".
            Camera = targetPositionCamera
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
            Camera = targetPositionCamera
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
            Camera = targetPositionCamera
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
            Camera = targetPositionCamera
        };

        scene.RootNode.Add(horizontalAxis1);
        

        // Clone the axis
        var offsetVector = new Vector3(0, 0, 20);

        var horizontalAxis2 = horizontalAxis1.Clone(clonedAxisTitle: "Cloned and flipped horizontal axis");
        horizontalAxis2.AxisStartPosition += offsetVector;
        horizontalAxis2.AxisEndPosition += offsetVector;
        horizontalAxis2.IsRenderingOnRightSideOfAxis = !horizontalAxis1.IsRenderingOnRightSideOfAxis; // flip side on which the ticks and labels are rendered

        scene.RootNode.Add(horizontalAxis2);
        

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 500;

            //defaultAxis.Camera = targetPositionCamera;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        _updatingButton = ui.CreateButton("Stop updating on camera change", () =>
        {
            _isUpdatingOnCameraChange = !_isUpdatingOnCameraChange;

            if (_isUpdatingOnCameraChange)
                _updatingButton?.SetText("Stop updating on camera change");
            else
                _updatingButton?.SetText("Start updating on camera change");

            Scene?.RootNode.ForEachChild<AxisWithLabelsNode>(axisNode => axisNode.UpdateOnCameraChanges = _isUpdatingOnCameraChange);
        });
    }
}