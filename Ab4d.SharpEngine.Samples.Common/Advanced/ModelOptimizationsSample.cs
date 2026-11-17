using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class ModelOptimizationsSample : CommonSample
{
    public override string Title => "Model Optimizations";
    public override string? Subtitle => "This samples show two possible optimizations for rendering 3D models with many individual meshes:\n1) Using ModelOptimizer can reduce draw calls count by combining meshes with the same material.\n2) Using OptimizedGroupNode renders all models with two draw calls (one for opaque and one for semi-transparent objects) and still allows full control of the materials and transformations.";

    private readonly string _initialFileName = "Resources\\Models\\ship_boat.obj";
    private GroupNode? _originalGroupNode;
    private GroupNode? _modelOptimizerGroupNode;
    private OptimizedGroupNode? _optimizedGroupNode;

    private int _modelOptimizerChildrenCount = 29;

    private enum OptimizationTypes
    {
        None = 0,
        ModelOptimizer = 1,
        OptimizedGroupNode = 2
    }

    private OptimizationTypes _optimizationType = OptimizationTypes.OptimizedGroupNode;


    public ModelOptimizationsSample(ICommonSamplesContext context)
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
            targetPositionCamera.Distance = 8000;
            targetPositionCamera.TargetPosition = new Vector3(-500, 1400, 600);
        }
        
        ShowCameraAxisPanel = true;
    }

    private void ImportFile(string? fileName)
    {
        if (Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();

        // Create ObjImporter object
        // To read texture images we also need to provide BitmapIO and 
        // it is also recommended to set GpuDevice (if not, then textures will be created later when GpuDevice is initialized).
        var objImporter = new ObjImporter(this.BitmapIO, this.GpuDevice);
        _originalGroupNode = objImporter.Import(fileName);

        //Ab4d.SharpEngine.Utilities.ModelUtils.MakeTwoSidedMaterial(_originalGroupNode);
        //Ab4d.SharpEngine.Utilities.ModelUtils.SetAlphaClipThreshold(_originalGroupNode, alphaClipThreshold: 0.1f);

        // We always use ModelOptimizer after loading the mode so we get number of new SceneNode objects.
        _modelOptimizerGroupNode = ModelOptimizer.Optimize(_originalGroupNode);

        _optimizedGroupNode = new OptimizedGroupNode(_originalGroupNode);

        UpdateOptimizationType(_optimizationType);
    }

    private void UpdateOptimizationType(OptimizationTypes newOptimizationType)
    {
        if (Scene == null || _originalGroupNode == null) 
            return;

        Scene.RootNode.Clear();

        if (newOptimizationType == OptimizationTypes.None)
        {
            if (_optimizedGroupNode != null)
            {
                // This will remove _originalGroupNode from _optimizedGroupNode so it can be added to Scene.RootNode
                _optimizedGroupNode.Clear();
                _optimizedGroupNode = null;
            }

            Scene.RootNode.Add(_originalGroupNode);
        }
        else if (newOptimizationType == OptimizationTypes.ModelOptimizer)
        {
            if (_modelOptimizerGroupNode == null)
                _modelOptimizerGroupNode = ModelOptimizer.Optimize(_originalGroupNode);

            _modelOptimizerChildrenCount = _modelOptimizerGroupNode.Count;

            Scene.RootNode.Add(_modelOptimizerGroupNode);
        }
        else //if (newOptimizationType == OptimizationTypes.OptimizedGroupNode)
        {
            if (_optimizedGroupNode == null)
                _optimizedGroupNode = new OptimizedGroupNode(_originalGroupNode);
            
            Scene.RootNode.Add(_optimizedGroupNode);
        }

        _optimizationType = newOptimizationType;
    }

    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        if (_originalGroupNode == null || _optimizedGroupNode == null)
            return;
        
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        
        ui.CreateLabel("Optimization method:", isHeader: true);

        ui.CreateRadioButtons(new string[]
            {
                $"None ({_originalGroupNode.Count} draw calls) (?):Showing the original model where one MeshModelNode is created for each mesh.\nThis is easy to update from the developer perspective but requires a lot of draw calls and slows down rendering.", 
                $"ModelOptimizer ({_modelOptimizerChildrenCount} draw calls) (?):ModelOptimizer combine meshes with the same material into single MeshModelNode object.\nThis significantly improve the rendering performance but it makes updating the objects difficult because the meshes are combined\nso changing the material or transformation of an individual mesh is not possible.",
                 "OptimizedGroupNode (2 draw call) (?):OptimizedGroupNode generates one single vertex and index buffer for all the meshes and\nuses indirect draw to render all objects with only 2 draw calls (one for opaque and one for semi-transparent objects).\nWhat is more, you still can change the material or transformation of each individual mesh.",
            },
            (selectedIndex, selectedText) => UpdateOptimizationType((OptimizationTypes)selectedIndex), 
            selectedItemIndex: (int)_optimizationType);
        
        ui.AddSeparator();
        
        ui.CreateLabel("See individual SceneNodes by clicking on the Diagnostics button and selecting 'Dump SceneNodes' from the menu.\n\nTo see what each draw call renders, select 'Dump RenderingLayers' in the Diagnostics window.", width: 250).SetStyle("italic");
    }
}