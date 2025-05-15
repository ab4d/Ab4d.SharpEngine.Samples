using System.IO;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class ModelOptimizerSample : CommonSample
{
    public override string Title => "ModelOptimizer";
    public override string? Subtitle => "ModelOptimizer can reduce draw calls count by combining models with the same material.";

    private readonly string _initialFileName = "Resources\\Models\\ship_boat.obj";
    private GroupNode? _originalGroupNode;
    private GroupNode? _optimizedGroupNode;


    public ModelOptimizerSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
        ImportFile(fileName);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -210;
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.Distance = 7500;
            targetPositionCamera.TargetPosition = new Vector3(-500, 1100, 600);
        }
        
        ShowCameraAxisPanel = true;
    }

    private void ImportFile(string? fileName)
    {
        if (Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();

        // Create ReaderObj object
        // To read texture images we also need to provide BitmapIO and 
        // it is also recommended to set GpuDevice (if not, then textures will be created later when GpuDevice is initialized).
        var readerObj = new ReaderObj(this.BitmapIO, this.GpuDevice);
        _originalGroupNode = readerObj.ReadSceneNodes(fileName);

        //Ab4d.SharpEngine.Utilities.ModelUtils.MakeTwoSidedMaterial(_originalGroupNode);
        //Ab4d.SharpEngine.Utilities.ModelUtils.SetAlphaClipThreshold(_originalGroupNode, alphaClipThreshold: 0.1f);

        _optimizedGroupNode = ModelOptimizer.Optimize(_originalGroupNode);

        Scene.RootNode.Add(_optimizedGroupNode);
    }

    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        if (_originalGroupNode == null || _optimizedGroupNode == null)
            return;
        
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        
        ui.CreateLabel("Show", isHeader: true);

        ui.CreateRadioButtons(new string[]
            {
                $"Original model ({_originalGroupNode.Count} SceneNodes)", 
                $"Optimized model ({_optimizedGroupNode.Count} SceneNodes)"
            },
            (selectedIndex, selectedText) =>
            {
                if (Scene == null) return;

                Scene.RootNode.Clear();
                if (selectedIndex == 0)
                    Scene.RootNode.Add(_originalGroupNode);
                else
                    Scene.RootNode.Add(_optimizedGroupNode);
            }, selectedItemIndex: 1);

        ui.AddSeparator();
        
        ui.CreateLabel("ModelOptimizer reduced draw calls count because models with the same material are merged:", width: 250);
        ui.CreateLabel($"SceneNodes count: {_originalGroupNode.Count} => {_optimizedGroupNode.Count}").SetStyle("bold");
        
        ui.AddSeparator();
        
        ui.CreateLabel("See individual SceneNodes by clicking on the Diagnostics button and selecting 'Dump SceneNodes' from the menu.", width: 250).SetStyle("italic");
    }
}