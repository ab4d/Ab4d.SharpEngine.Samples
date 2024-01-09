using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class PositionAndScaleSample: CommonSample
{
    public override string Title => "Position and scale 3D models with PositionAndScaleSceneNode method";
    public override string Subtitle => "Easily position any SceneNode by providing final position and position type and set SceneNode's final size.\nPosition is shown by a red cross.\nSize is shown by a green wire-box.";


    public PositionAndScaleSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var teapotMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Teapot,
                                                position: new Vector3(0, 0, 0),
                                                positionType: PositionTypes.Bottom | PositionTypes.Center,
                                                finalSize: new Vector3(60, 60, 60));

        AddSceneNode(teapotMesh, scene, new Vector3(-120, 0, 0), PositionTypes.Bottom,                   new Vector3(80, 80, 80));
        AddSceneNode(teapotMesh, scene, new Vector3(-40, 60, 0), PositionTypes.Left | PositionTypes.Top, new Vector3(80, 60, 80));
        AddSceneNode(teapotMesh, scene, new Vector3(120, 15, 0), PositionTypes.Center,                   new Vector3(80, 60, 20), preserveAspectRatio: false, wireCrossLineLength: 80);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(0, 30, 0);
            targetPositionCamera.Heading = 15;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 500;
        }

        ShowCameraAxisPanel = true;
    }

    private void AddSceneNode(Mesh mesh, Scene scene, Vector3 position, PositionTypes positionType, Vector3 size, bool preserveAspectRatio = true, float wireCrossLineLength = 50)
    {
        // Create a new Model3DGroup that will hold the originalModel3D.
        // This allows us to have different transformation for each originalModel3D (transformation is on Model3DGroup)
        var teapotModel = new MeshModelNode(mesh, StandardMaterials.Orange);


        // The easiest way to adjust the position of the model is to use TranslateTransform:
        //teapotModel.Transform = new TranslateTransform(x: 10, y: 20, z: 30);
        //
        // But this only offsets the current position and if we do not know the current position,
        // we also do not know the final position after translation.
        //
        // The advantage of PositionAndScaleSceneNode is that we can specify the final model position and
        // also define the position type!

        // Similarly, we can use the ScaleTransform:
        //teapotModel.Transform = new ScaleTransform(scaleX: 1, scaleY: 2, scaleZ: 3);
        //
        // But again, if we do not know initial size, we also do not know the size after ScaleTransform.
        // 
        // Again, the PositionAndScaleSceneNode method uses the final size.

        Ab4d.SharpEngine.Utilities.ModelUtils.PositionAndScaleSceneNode(teapotModel, position, positionType, size, preserveAspectRatio);
        
        // If we want only to position the model and not scale it
        //Ab4d.SharpEngine.Utilities.ModelUtils.PositionSceneNode(teapotModel, position, positionType, preserveCurrentTransformation: true);

        scene.RootNode.Add(teapotModel);


        // Now add red WireCrossNode at the specified position
        var wireCrossNode = new WireCrossNode()
        {
            Position = position,
            LinesLength = wireCrossLineLength,
            LineThickness = 2,
            LineColor = Colors.Red
        };

        scene.RootNode.Add(wireCrossNode);


        // Now show a WireBoxNode (box from 3D lines) that would represent the position, positionType and size.

        // To get the correct CenterPosition of the WireBoxNode,
        // we start with creating a BoundingBox that would be used when CenterPosition would be set to (0, 0, 0):
        var minSize = new Vector3(-size.X * 0.5f, -size.Y * 0.5f, -size.Z * 0.5f);
        var maxSize = new Vector3(size.X * 0.5f, size.Y * 0.5f, size.Z * 0.5f);

        // Then we use that bounding box and call GetModelTranslationVector3D method
        // that will tell us how much we need to move the bounding box so that it will be positioned at position and for positionType:
        var wireboxCenterOffset = Ab4d.SharpEngine.Utilities.ModelUtils.GetModelTranslationVector(new BoundingBox(minSize, maxSize), position, positionType);

        // Now we can use the result wireboxCenterOffset as a CenterPosition or a WireBoxNode

        var wireBoxNode = new WireBoxNode()
        {
            Position = new Vector3(wireboxCenterOffset.X, wireboxCenterOffset.Y, wireboxCenterOffset.Z),
            PositionType = PositionTypes.Center,
            Size = size,
            LineColor = Colors.Green,
            LineThickness = 1,
        };

        scene.RootNode.Add(wireBoxNode);


        // Finally we add TextBlockVisual3D to show position and size information for this model
        // Note that the TextBlockVisual3D is added to the TransparentObjectsVisual3D.
        // The reason for this is that TextBlockVisual3D uses semi-transparent background.
        // To correctly show other object through semi-transparent, the semi-transparent must be added to the scene after solid objects.
        var infoText = $"Position: ({position.X}, {position.Y}, {position.Z})\nPositionType: {positionType}\nSize: ({size.X}, {size.Y}, {size.Z})";
        if (!preserveAspectRatio)
            infoText += "\npreserveAspectRatio: false";

        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;
        textBlockFactory.FontSize = 6;

        var textPosition = new Vector3(teapotModel.GetLocalBoundingBox().GetCenterPosition().X, -15, 55); // Show so that X center position is the same as model center position

        var textNode = textBlockFactory.CreateTextBlock(infoText, textPosition, textAttitude: 30);
        scene.RootNode.Add(textNode);
    }
}