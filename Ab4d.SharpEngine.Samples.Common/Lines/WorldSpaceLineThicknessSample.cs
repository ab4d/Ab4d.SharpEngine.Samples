using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class WorldSpaceLineThicknessSample : CommonSample
{
    public override string Title => "Sceen-space vs. world-space LineThickness";
    public override string Subtitle => 
@"Sceen-space LineThickness (default) renders lines that preserve their thickness on the screen regardless where in 3D space they are.
The actually used line thickness is LineThickness multiplied by dpi-scale.
When you zoom in or out, the line thickness remains the same.

World-space LineThickness creates line rectangles with the width set from the LineThickness. Such lines are thinner when frather away from a perspective camera.
When you zoom in or out, the line thickness is also changed.";
    
    public WorldSpaceLineThicknessSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        var screenSpaceLineNode = new LineNode(new Vector3(0, 20, 0), new Vector3(0, 20, -1000), Ab4d.SharpEngine.Common.Colors.Blue, 10, "ScrenSpaceLine");
        
        // By default IsWorldSpaceLineThickness is false which creates screen-space line thickness
        //screenSpaceLineNode.IsWorldSpaceLineThickness = false; 
        
        scene.RootNode.Add(screenSpaceLineNode);
        
        
        var worldSpaceLineNode = new LineNode(new Vector3(0, 0, 0), new Vector3(0, 0, -1000), Ab4d.SharpEngine.Common.Colors.Red, 10, "WorldSpaceLine")
        {
            IsWorldSpaceLineThickness = true // Set to true to use world-space line thickness
        };
        
        scene.RootNode.Add(worldSpaceLineNode);


        for (int i = 0; i < 10; i++)
        {
            var boxModelNode = new BoxModelNode()
            {
                Position = new Vector3(0, -20, -5 - i * 100),
                Size = new Vector3(10, 10, 10),
                Material = StandardMaterials.Gray
            };
            
            scene.RootNode.Add(boxModelNode);
        }


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -20;
            targetPositionCamera.Attitude = 0;
            targetPositionCamera.Distance = 200;
            targetPositionCamera.TargetPosition = new Vector3(20, 0, 0);
        }
        
        
        var textBlockFactory = await context.GetTextBlockFactoryAsync();
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 0.5f;
        textBlockFactory.FontSize = 4;
        textBlockFactory.BackgroundHorizontalPadding = 4;
        textBlockFactory.BackgroundVerticalPadding = 2;
        
        textBlockFactory.BorderColor = Colors.Blue;
        var textNode1 = textBlockFactory.CreateTextBlock("Sceen-space\nLineThickness: 10", new Vector3(-35, 20, 0), textAttitude: 90);
        scene.RootNode.Add(textNode1);
        
        textBlockFactory.BorderColor = Colors.Red;
        var textNode2 = textBlockFactory.CreateTextBlock("World-space\nLineThickness: 10", new Vector3(-35, 0, 0), textAttitude: 90);
        scene.RootNode.Add(textNode2);

        textBlockFactory.BorderColor = Colors.DimGray;
        var textNode3 = textBlockFactory.CreateTextBlock("Box size: 10", new Vector3(-35, -20, 0), textAttitude: 90);
        scene.RootNode.Add(textNode3);
    }
}