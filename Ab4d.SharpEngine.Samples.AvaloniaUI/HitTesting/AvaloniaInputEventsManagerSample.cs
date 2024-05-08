using System.Numerics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Avalonia.Input;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.HitTesting;

public class AvaloniaInputEventsManagerSample : CommonSample
{
    public override string Title => "InputEventsManager sample";

    private InputEventsManager? _inputEventsManager;

    private Material? _savedMaterial;

    public AvaloniaInputEventsManagerSample(ICommonSamplesContext context)
        : base(context)
    {
        
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (context.CurrentSharpEngineSceneView is not SharpEngineSceneView sharpEngineSceneView)
            return;

        var eventsSourceElement = sharpEngineSceneView.Parent as IInputElement;
        _inputEventsManager = new InputEventsManager(sharpEngineSceneView, eventsSourceElement);


        var baseBox = new BoxModelNode(centerPosition: new Vector3(0, -10, 0), size: new Vector3(400, 20, 400), material: StandardMaterials.Green, "Base box");
        scene.RootNode.Add(baseBox);


        var glassBox = new BoxModelNode(centerPosition: new Vector3(0, 100, 200), size: new Vector3(400, 200, 10), material: StandardMaterials.LightBlue.SetOpacity(0.3f), "Glass box");
        scene.RootNode.Add(glassBox);

        _inputEventsManager.RegisterExcludedSceneNode(glassBox);

        

        
        for (int i = 0; i < 10; i++)
        {
            var oneBox = new BoxModelNode(new Vector3(i * 50, 20, 0), new Vector3(40, 40, 40), StandardMaterials.Silver, $"Box_{i}")
            {
                UseSharedBoxMesh = false,
                Transform = new TranslateTransform(0, 100, 0)
            };
            scene.RootNode.Add(oneBox);

            var modelNodeEventsSource = new ModelNodeEventsSource(oneBox);
            modelNodeEventsSource.PointerEnter += (sender, args) =>
            {
                _savedMaterial = oneBox.Material;
                oneBox.Material = StandardMaterials.Orange;
            };

            modelNodeEventsSource.PointerLeave += (sender, args) => oneBox.Material = _savedMaterial;
            
            modelNodeEventsSource.PointerClick += (sender, args) =>
            {
                oneBox.Material = StandardMaterials.Red;
                _savedMaterial = oneBox.Material;
            };

            modelNodeEventsSource.PointerDoubleClick += (sender, args) =>
            {
                oneBox.Material = StandardMaterials.Gold;
                _savedMaterial = oneBox.Material;
            };

            _inputEventsManager.RegisterEventsSource(modelNodeEventsSource);
        }
    }
}