using System.Drawing;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class TransformationsSample : CommonSample
{
    public override string Title => "Transformations";
    public override string Subtitle => "This sample shows different SceneNode transformations.\nThe last two teapots show that different order of transformations in TransformGroup gives different results.";

    private Vector3 _currentPosition = new Vector3(-280, 0, 0);
    private Vector3 _modelSize = new Vector3(60, 60, 60);
    
    private StandardMaterial _teapotMaterial = StandardMaterials.Orange;
    private StandardMesh? _teapotMesh;

    private int _modelIndex;

    public TransformationsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModelNode = new BoxModelNode(centerPosition: new Vector3(0, -5, 50), size: new Vector3(700, 10, 300), "BaseBoxNode")
        {
            Material = StandardMaterials.Silver
        };

        scene.RootNode.Add(boxModelNode);


        AddTeapot(scene, 
                  transform: new StandardTransform(translateX: 0, translateY: -5, translateZ: 0, scale: 1.3f, rotateX: 30, rotateY: 45, rotateZ: 0),
                  text: "StandardTransform\ntranslateY:-5\nScale:1.3\nrotateX: 30, rotateY: 45", 
                  name: "StandardTransform");       
        
        AddTeapot(scene, 
                  transform: new TranslateTransform(x: 10, y: -10, z: 20), 
                  text: "TranslateTransform\n(x:10, y:-10, z:20)", 
                  name: "TranslateTransform");
        
        AddTeapot(scene, 
                  transform: new ScaleTransform(0.5f, 2.5f, 1.2f), 
                  text: "ScaleTransform\n(0.5, 2.5, 1.2)", 
                  name: "ScaleTransform");
        
        AddTeapot(scene, 
                  transform: new AxisAngleRotateTransform(axis: new Vector3(0, 1, 0), angle: 45), 
                  text: "AxisAngleRotateTransform\n(axis: up; angle: 45)", 
                  name: "AxisAngleRotateTransform");

        AddTeapot(scene, 
                  transform: new QuaternionRotateTransform(x: 0, y: 0, z: 0.382683456f, w: 0.9238795f), // Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathUtils.DegreesToRadians(45))
                  text: "QuaternionRotateTransform\n(z: 0.38, w: 0.92)", 
                  name: "QuaternionRotateTransform");


        // TransformGroup can be used to group multiple transformations.
        //
        // NOTE:
        // Order of transformations is IMPORTANT!
        // Usually the following order should be used: scale, rotate, translate.
        // The next transform is showing the same transformations but with different order
        var transformGroup1 = new TransformGroup();
        transformGroup1.Add(new ScaleTransform(scaleX: 1.5f));
        transformGroup1.Add(new AxisAngleRotateTransform(axis: new Vector3(0, 1, 0), angle: 90));
        transformGroup1.Add(new TranslateTransform(20, 0, 20));

        AddTeapot(scene,
                  transform: transformGroup1,
                  text: "TransformGroup\n(scale, rotate, translate)",
                  name: "TransformGroup");
        
        
        // The same transformations as before but with different oder (this gives different results)
        var transformGroup2 = new TransformGroup();
        transformGroup2.Add(new TranslateTransform(20, 0, 20));
        transformGroup2.Add(new AxisAngleRotateTransform(axis: new Vector3(0, 1, 0), angle: 90));
        transformGroup2.Add(new ScaleTransform(scaleX: 1.5f));
        
        AddTeapot(scene,
                  transform: transformGroup2,
                  text: "TransformGroup\n(translate, rotate, scale)",
                  name: "TransformGroup");



        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 20;
            targetPositionCamera.Attitude = -35;
            targetPositionCamera.Distance = 840;
            targetPositionCamera.TargetPosition = new Vector3(-5, 0, 10);
        }
    }

    private void AddTeapot(Scene scene, Transform? transform, string text, string name)
    {
        _teapotMesh ??= TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Teapot,
                                               position: new Vector3(0, 0, 0),
                                               positionType: PositionTypes.Bottom | PositionTypes.Center,
                                               finalSize: _modelSize);

        // Create TransformGroup so 
        var transformGroup = new TransformGroup();

        if (transform != null)
            transformGroup.Add(transform);

        transformGroup.Add(new TranslateTransform(_currentPosition));


        var teapotModel = new MeshModelNode(_teapotMesh, _teapotMaterial, name);
        teapotModel.Transform = transformGroup;

        scene.RootNode.Add(teapotModel);

        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;
        textBlockFactory.FontSize = 8;

        var textZPos = (_modelIndex % 2 == 0) ? 150 : 100;
        var textNode1 = textBlockFactory.CreateTextBlock(text, _currentPosition + new Vector3(0, 20, textZPos), textAttitude: 30);
        scene.RootNode.Add(textNode1);


        var rectangleNode = new RectangleNode(name: name + "Rectangle")
        {
            Position = _currentPosition + new Vector3(0, 0.5f, 0), // lift slightly from BaseBoxNode
            Size = new Vector2(_modelSize.X, _modelSize.Z),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 0, 1),
            LineColor = Colors.Black,
            LineThickness = 1
        };

        scene.RootNode.Add(rectangleNode);

        _modelIndex++;
        _currentPosition += new Vector3(_modelSize.X * 1.5f, 0, 0);
    }
}