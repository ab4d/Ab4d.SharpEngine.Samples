using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Graphs;

public class AxesBoxNodeSample : CommonSample
{
    public override string Title => "AxesBoxNode";
    public override string Subtitle => "TODO";
    
    
    public AxesBoxNodeSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -20;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 430;
        }
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        ShowDemoAxes(sceneView);
        base.OnSceneViewInitialized(sceneView);
    }

    private void ShowDemoAxes(SceneView sceneView)
    {
        var axesBox = new AxesBoxNode()
        {
            CenterPosition = new Vector3(0, 0, 0),
            Size = new Vector3(100, 100, 100),

            AxisShowingStrategy = AxesBoxNode.AxisShowingStrategies.FrontFacingPlanes,

            Camera = sceneView.Camera,


        };

        // Set axes names:
        axesBox.XAxis1.AxisTitle = "XAxis1";
        axesBox.XAxis2.AxisTitle = null;

        axesBox.YAxis1.AxisTitle = "YAxis1";
        axesBox.YAxis2.AxisTitle = null;

        axesBox.ZAxis1.AxisTitle = "ZAxis1";
        axesBox.ZAxis2.AxisTitle = null;

        sceneView.Scene.RootNode.Add(axesBox);

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
}