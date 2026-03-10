using System.Drawing;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Graphs;

public class AxesBoxNodeSample : CommonSample
{
    public override string Title => "AxesBoxNode";
    public override string Subtitle => "AxesBoxNode can show all 6 axes with tick lines and value labels.\nIt automatically switches and orients the shown axes.\nTick lines and value labels can be shown as 3D text or as 2D text.";
    
    private AxesBoxNode _axesBoxNode;
    
    private Color4[]? _gradientColors;
    
    private bool _customizeZAxisValues;
    private bool _customizeZAxisColors;
    
    
    public AxesBoxNodeSample(ICommonSamplesContext context)
        : base(context)
    {
        _axesBoxNode = new AxesBoxNode()
        {
            CenterPosition = new Vector3(0, 0, 0),
            Size = new Vector3(100, 100, 100),
            
            ValueLabelsColor = new Color4(0.3f, 0.3f, 0.3f, 1), // Slightly dim the value labels

            AxisShowingStrategy = AxesBoxNode.AxisShowingStrategies.FrontFacingPlanes,
            AdjustFirstAndLastLabelPositions = true, // Adjust positions of first and last value labels so they do not overlap with values on other axis
        };
        
        // Set axis titles
        _axesBoxNode.XAxis1.AxisTitle = "XAxis1";
        _axesBoxNode.XAxis2.AxisTitle = "";
        
        _axesBoxNode.YAxis1.AxisTitle = "YAxis1";
        _axesBoxNode.YAxis2.AxisTitle = "";
        
        _axesBoxNode.ZAxis1.AxisTitle = "ZAxis1";
        _axesBoxNode.ZAxis2.AxisTitle = "";
        
        // Set axes data ranges:
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.XAxis, minimumValue: 0, maximumValue: 100, majorTicksStep: 10, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.YAxis, minimumValue: 0, maximumValue: 100, majorTicksStep: 10, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.ZAxis, minimumValue: -20, maximumValue: 200, majorTicksStep: 20, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);
        
        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -20;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 430;

            // Adjust the camera so the 3D scene is moved to the left (see Cameras/OffCenterCameraSample for more info)
            targetPositionCamera.TargetPosition = new Vector3(30, 0, 0);
            targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0);
        }
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        ShowDemoAxes(sceneView);
        base.OnSceneViewInitialized(sceneView);
    }

    private void ShowDemoAxes(SceneView sceneView)
    {
        _axesBoxNode.Camera = sceneView.Camera;
        sceneView.Scene.RootNode.Add(_axesBoxNode);

        //                 <visuals:AxesBoxVisual3D x:Name="AxesBox"
        //                          CenterPosition="0 0 0"
        //                          Size="100 100 100"
        //                          
        //                          Camera="{Binding ElementName=Camera1}"
        //                          OverlayCanvas="{Binding ElementName=AxisOverlayCanvas}"
        //                          
        //                          Is3DTextShown="True"
        //                          IsWireBoxFullyClosed="{Binding ElementName=IsWireBoxFullyClosedCheckBox, Path=IsChecked}"
        //                          AdjustFirstAndLastLabelPositions="{Binding ElementName=AdjustFirstAndLastLabelPositionsCheckBox, Path=IsChecked}"
        //                          AxisShowingStrategy="{Binding ElementName=AxisShowingStrategyComboBox, Path=SelectedItem}"
        //                          
        //                          ShowBottomConnectionLines="{Binding ElementName=ShowBottomConnectionLinesCheckBox, Path=IsChecked}"
        //                          ShowBackConnectionLines="{Binding ElementName=ShowBackConnectionLinesCheckBox, Path=IsChecked}"
        //                          ShowXAxisConnectionLines="{Binding ElementName=ShowXAxisConnectionLinesCheckBox, Path=IsChecked}"
        //                          ShowYAxisConnectionLines="{Binding ElementName=ShowYAxisConnectionLinesCheckBox, Path=IsChecked}"
        //                          ShowZAxisConnectionLines="{Binding ElementName=ShowZAxisConnectionLinesCheckBox, Path=IsChecked}"
        //                          
        //                          AxisTitleBrush="Black"
        //                          AxisTitleFontSize="6"
        //                          AxisTitleFontWeight="Bold"

        //                          ValueLabelsBrush="Black"
        //                          ValueLabelsFontSize="6"
        //                          ValueLabelsFontWeight="Normal"
        //                          ValueLabelsPadding="3"
        //                          ValueDisplayFormatString="#,##0"
        //                          
        //                          AxisLineColor="Black"
        //                          AxisLineThickness="2"
        //                          
        //                          TicksLineColor="Black"
        //                          TicksLineThickness="1"
        //                          
        //                          ConnectionLinesColor="Gray"
        //                          ConnectionLinesThickness="0.5"
        //                          
        //                          MajorTicksLength="5"
        //                          MinorTicksLength="2.5" />
        //                          <!-- Axes line data ranges and axes titles are set in code behind -->
    }


    
    private void UpdateZAxisCustomization()
    {
        var originalValueLabels = _axesBoxNode.ZAxis1.GetValueLabels();
        int valueLabelsCount = originalValueLabels.Length;

        if (_customizeZAxisValues)
        {
            var customValueLabels = new string[valueLabelsCount];
            for (int i = 0; i < valueLabelsCount; i++)
                customValueLabels[i] = string.Format("{0:0}%", (i * 100f) / (valueLabelsCount - 1));

            _axesBoxNode.ZAxis1.SetCustomValueLabels(customValueLabels);
            _axesBoxNode.ZAxis2.SetCustomValueLabels(customValueLabels);
        }
        else
        {
            // Disable custom labels
            _axesBoxNode.ZAxis1.SetCustomValueLabels(null);
            _axesBoxNode.ZAxis2.SetCustomValueLabels(null);
        }

        if (_customizeZAxisColors)
        {
            var gradientColors = EnsureGradientColors(valueLabelsCount);

            _axesBoxNode.ZAxis1.SetCustomValueColors(gradientColors);
            _axesBoxNode.ZAxis2.SetCustomValueColors(gradientColors);
        }
        else
        {
            // Disable custom labels
            _axesBoxNode.ZAxis1.SetCustomValueColors(null);
            _axesBoxNode.ZAxis2.SetCustomValueColors(null);
        }
    }


    private Color4[] EnsureGradientColors(int valueLabelsCount)
    {
        if (_gradientColors == null || _gradientColors.Length != valueLabelsCount)
        {
            var gradient = new GradientStop[]
            {
                new GradientStop(Colors.Red, 1),
                new GradientStop(Colors.Orange, 0.8f),
                new GradientStop(Colors.Yellow, 0.6f),
                new GradientStop(Colors.Green, 0.4f),
                new GradientStop(Colors.DarkBlue, 0.2f),
                new GradientStop(Colors.DodgerBlue, 0)
            };

            _gradientColors = TextureFactory.CreateGradientColors(gradient, arraySize: valueLabelsCount);
        }

        return _gradientColors;
    }

    private void RandomizeAxesBox()
    {
        _axesBoxNode.AxisTitleColor = GetRandomHsvColor4();
        _axesBoxNode.AxisTitleFontSize = GetRandomFloat() * 5 + 5;

        _axesBoxNode.AxisLineColor = GetRandomHsvColor4();
        _axesBoxNode.AxisLineThickness = GetRandomFloat() * 3 + 0.5f;

        _axesBoxNode.TicksLineColor = GetRandomHsvColor4();
        _axesBoxNode.TicksLineThickness = GetRandomFloat() * 3 + 0.5f;

        _axesBoxNode.ConnectionLinesColor = GetRandomHsvColor4();
        _axesBoxNode.ConnectionLinesThickness = GetRandomFloat() * 3 + 0.5f;

        _axesBoxNode.MinorTicksLength = GetRandomFloat() * 3 + 2;
        _axesBoxNode.MajorTicksLength = _axesBoxNode.MinorTicksLength * 2;

        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.XAxis, GetRandomFloat() * 50, GetRandomFloat() * 50 + 60, majorTicksStep: 10, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.YAxis, GetRandomFloat() * 50, GetRandomFloat() * 50 + 60, majorTicksStep: 10, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.ZAxis, GetRandomFloat() * 50, GetRandomFloat() * 50 + 60, majorTicksStep: 10, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);

        UpdateZAxisCustomization();
    }
    
    
    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel(@"AxisShowingStrategy: (?):None: AxesBoxNode will not automatically show and hide axes.
FrontFacingPlanes: AxesBoxNode will automatically show and hide axes so that the axes that define front facing planes will be shown.
LeftmostAxis: AxesBoxNode will automatically show and hide vertical axes (ZAxis) so that only axis that is furthest to the left on the screen is shown.
RightmostAxis: AxesBoxNode will automatically show and hide vertical axes so that only axis that is furthest to the right on the screen is shown.");
        
        var strategies = new string[] { "None.", "FrontFacingPlanes", "LeftmostAxis", "RightmostAxis" };
        ui.CreateComboBox(strategies, (selectedIndex, selectedText) =>
        {
            _axesBoxNode.AxisShowingStrategy = (AxesBoxNode.AxisShowingStrategies)selectedIndex;
        }, selectedItemIndex: 1);
        
        ui.AddSeparator();
        ui.CreateLabel("Axes visibility:", isHeader: true);

        ui.CreateCheckBox("Show bottom X axis (XAxis1)", true, isChecked => _axesBoxNode.IsXAxis1Visible = isChecked);
        ui.CreateCheckBox("Show top X axis (XAxis2)", false, isChecked => _axesBoxNode.IsXAxis2Visible = isChecked);
        ui.AddSeparator();
        
        ui.CreateCheckBox("Show bottom Y axis (YAxis1)", true, isChecked => _axesBoxNode.IsYAxis1Visible = isChecked);
        ui.CreateCheckBox("Show top Y axis (YAxis2)", false, isChecked => _axesBoxNode.IsYAxis2Visible = isChecked);
        ui.AddSeparator();
        
        ui.CreateCheckBox("Show first Z axis (ZAxis1)", true, isChecked => _axesBoxNode.IsZAxis1Visible = isChecked);
        ui.CreateCheckBox("Show second Z axis (ZAxis2)", true, isChecked => _axesBoxNode.IsZAxis2Visible = isChecked);
        
        ui.AddSeparator();
        ui.CreateLabel("Connection lines visibility:", isHeader: true);

        ui.CreateCheckBox("Show X axis connection lines", false, isChecked => _axesBoxNode.ShowXAxisConnectionLines = isChecked);
        ui.CreateCheckBox("Show Y axis connection lines", false, isChecked => _axesBoxNode.ShowYAxisConnectionLines = isChecked);
        ui.CreateCheckBox("Show Z axis connection lines", true, isChecked => _axesBoxNode.ShowZAxisConnectionLines = isChecked);
        ui.AddSeparator();

        ui.CreateCheckBox("Show bottom connection lines", true, isChecked => _axesBoxNode.ShowBottomConnectionLines = isChecked);
        ui.CreateCheckBox("Show back connection lines", true, isChecked => _axesBoxNode.ShowBackConnectionLines = isChecked);
        ui.AddSeparator();
        
        ui.CreateCheckBox("IsWireBoxFullyClosed", false, isChecked => _axesBoxNode.IsWireBoxFullyClosed = isChecked);
        ui.CreateCheckBox("AdjustFirstAndLastLabelPositions", true, isChecked => _axesBoxNode.AdjustFirstAndLastLabelPositions = isChecked);
        
        ui.AddSeparator();

        ui.CreateCheckBox("Customize Z axis values", _customizeZAxisValues, isChecked =>
        {
            _customizeZAxisValues = isChecked;
            UpdateZAxisCustomization();
        });
        
        ui.CreateCheckBox("Customize Z axis colors", _customizeZAxisColors, isChecked =>
        {
            _customizeZAxisColors = isChecked;
            UpdateZAxisCustomization();
        });

        ui.CreateButton("Randomize", () => RandomizeAxesBox());
    }
}