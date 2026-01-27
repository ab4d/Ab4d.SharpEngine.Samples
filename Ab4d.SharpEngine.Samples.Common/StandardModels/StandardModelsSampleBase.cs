using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;
using Ab4d.SharpEngine.Core;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public abstract class StandardModelsSampleBase : CommonSample
{
    protected ICommonSampleUIPanel? rootStackPanel;

    protected ModelNode? modelNode;
    protected MultiLineNode? wireframeLineNode;
    protected SceneNode? normalsLineNode;

    protected Material? modelMaterial;

    protected bool isTrianglesCheckBoxShown = true;
    protected bool isNormalsCheckBoxShown = true;
    protected bool isSemiTransparentCheckBoxShown = true;
    protected bool isTextureCheckBoxShown = true;

    protected bool isShowTrianglesChecked = true;
    protected bool isShowNormalsChecked = true;
    protected bool isSemiTransparentMaterialChecked = true;
    protected bool isTextureMaterialChecked = false;

    protected float normalsLength = 0; // calculated from mesh size
    protected float normalsLineThickness = 1;

    protected string? propertiesTitleText = "Properties";
    
    protected GpuImage? textureImage;

    protected string positionsCountText = "0";
    protected string trianglesCountText = "0";
    
    protected ICommonSampleUIElement? positionsCountLabel;
    protected ICommonSampleUIElement? trianglesCountLabel;
    protected ICommonSampleUIElement? showTrianglesCheckBox;
    protected ICommonSampleUIElement? showNormalsCheckBox;
    protected ICommonSampleUIElement? isSemiTransparentCheckBox;
    protected ICommonSampleUIElement? isTextureCheckBox;

    private GpuImage? _commonTexture;
    

    public StandardModelsSampleBase(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        _commonTexture = await base.GetCommonTextureAsync("10x10-texture.png", scene.GpuDevice);
        
        ShowCameraAxisPanel = true;

        modelNode = CreateModelNode();

        UpdateModelNode();
        UpdateMaterial();

        scene.RootNode.Add(modelNode);
        UpdateModelNode(); // Call that again in case we need mesh and this is generated only after the scene is known
    }

    protected abstract ModelNode CreateModelNode();
    
    protected virtual void UpdateModelNode()
    {
        if (modelNode == null || Scene == null || Scene.GpuDevice == null)
            return;

        if (!modelNode.IsInitialized)
            modelNode.InitializeSceneResources(Scene); // This will generate the mesh so we will be able to get triangles and normals
        else
            modelNode.Update();

        var standardMesh = modelNode.GetMesh() as StandardMesh;
        var meshTransform = modelNode.GetMeshTransform();

        UpdateTriangles(standardMesh, meshTransform);
        UpdateNormals(standardMesh, meshTransform);

        UpdateMeshStatistics(standardMesh);
    }

    protected virtual void UpdateTriangles(StandardMesh? mesh, Transform? modelTransform)
    {
        if (Scene == null || !isShowTrianglesChecked)
            return;

        if (wireframeLineNode != null)
            Scene.RootNode.Remove(wireframeLineNode);

        if (mesh == null)
            return;

        // Show wireframe positions
        var wireframePositions = LineUtils.GetWireframeLinePositions(mesh, removedDuplicateLines: true); // remove duplicate lines at the edges of triangles


        var lineMaterial = new LineMaterial(Color3.Black, lineThickness: 1)
        {
            DepthBias = 0.002f
        };

        wireframeLineNode = new MultiLineNode(wireframePositions, isLineStrip: false, lineMaterial, "WireframeLines")
        {
            Transform = modelTransform,
        };

        Scene.RootNode.Add(wireframeLineNode);
    }
    
    protected virtual void UpdateNormals(StandardMesh? mesh, Transform? modelTransform)
    {
        if (Scene == null || !isShowNormalsChecked)
            return;

        if (normalsLineNode != null)
            Scene.RootNode.Remove(normalsLineNode);

        if (mesh == null) 
            return;


        var normalLinePositions = LineUtils.GetNormalLinePositions(mesh, normalsLength, modelTransform);

        if (normalLinePositions == null)
            return;

        var lineMaterial = new LineMaterial(Colors.Orange, normalsLineThickness)
        {
#if VULKAN
            EndLineCap = LineCap.ArrowAnchor
#endif
        };

        normalsLineNode = new MultiLineNode(normalLinePositions, isLineStrip: false, lineMaterial, "NormalLines");

        Scene.RootNode.Add(normalsLineNode);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        rootStackPanel = ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("View", isHeader: true);

        if (isTrianglesCheckBoxShown)
            showTrianglesCheckBox = ui.CreateCheckBox("Show triangles", isShowTrianglesChecked, OnShowTrianglesChanged);

        if (isNormalsCheckBoxShown)
            showNormalsCheckBox = ui.CreateCheckBox("Show normals", isShowNormalsChecked, OnShowNormalsChanged);

        if (isSemiTransparentCheckBoxShown)
            isSemiTransparentCheckBox = ui.CreateCheckBox("Is semi-transparent", isSemiTransparentMaterialChecked, OnIsSemiTransparentChanged);

        if (isTextureCheckBoxShown)
            isTextureCheckBox = ui.CreateCheckBox("Is texture material", isTextureMaterialChecked, OnIsTextureMaterialChanged);

        if (propertiesTitleText != null)
            ui.CreateLabel(propertiesTitleText, isHeader: true);

        OnCreatePropertiesUI(ui);
    }

    protected virtual void AddMeshStatisticsControls(ICommonSampleUIProvider ui, bool addSharpEdgeInfo = false, float sharpEdgeInfoWidth =  220)
    {
        ui.CreateLabel("Mesh Statistics", isHeader: true);
        positionsCountLabel = ui.CreateKeyValueLabel("Positions:", () => positionsCountText, keyTextWidth: 70);
        trianglesCountLabel = ui.CreateKeyValueLabel("Triangles:", () => trianglesCountText, keyTextWidth: 70);

        if (addSharpEdgeInfo)
        {
            ui.AddSeparator();
            ui.CreateLabel("To create sharp edges, the positions need to be duplicated so that each position can have normal vector in its own direction (see orange normal lines).", width: sharpEdgeInfoWidth).SetStyle("italic");
        }
    }
    
    protected virtual void UpdateMeshStatistics(StandardMesh? standardMesh)
    {
        uint positionsCount = standardMesh?.VertexCount ?? 0;
        positionsCountText = positionsCount.ToString();

        uint trianglesCount = (uint)(standardMesh?.IndexCount ?? 0) / 3;
        trianglesCountText = trianglesCount.ToString();

        if (positionsCountLabel != null)
            positionsCountLabel.UpdateValue();
        
        if (trianglesCountLabel != null)
            trianglesCountLabel.UpdateValue();
    }

    protected abstract void OnCreatePropertiesUI(ICommonSampleUIProvider ui);

    protected virtual void OnShowTrianglesChanged(bool isChecked)
    {
        isShowTrianglesChecked = isChecked;

        if (modelNode == null || Scene == null)
            return;

        var standardMesh = modelNode.GetMesh() as StandardMesh;

        if (standardMesh == null)
            return;

        if (isChecked)
            UpdateTriangles(standardMesh, modelNode.GetMeshTransform());
        else if (wireframeLineNode != null)
            Scene.RootNode.Remove(wireframeLineNode);
    }
    
    protected virtual void OnShowNormalsChanged(bool isChecked)
    {
        isShowNormalsChecked = isChecked;

        if (modelNode == null || Scene == null)
            return;

        var standardMesh = modelNode.GetMesh() as StandardMesh;

        if (standardMesh == null)
            return;

        if (isChecked)
            UpdateNormals(standardMesh, modelNode.GetMeshTransform());
        else if (normalsLineNode != null)
            Scene.RootNode.Remove(normalsLineNode);
    }
    
    protected virtual void OnIsTextureMaterialChanged(bool isChecked)
    {
        isTextureMaterialChecked = isChecked;
        UpdateMaterial();
    }
    
    protected virtual void OnIsSemiTransparentChanged(bool isChecked)
    {
        isSemiTransparentMaterialChecked = isChecked;
        UpdateMaterial();
    }

    protected virtual Material GetMaterial()
    {
        if (modelMaterial is not StandardMaterial standardMaterial)
        {
            standardMaterial = new StandardMaterial();
            modelMaterial = standardMaterial;
        }

        if (isTextureMaterialChecked && _commonTexture != null)
        {
            standardMaterial.DiffuseTexture = _commonTexture;
            standardMaterial.DiffuseColor = Color3.White; // When using DiffuseTexture, then DiffuseColor is used as a filter (it is multiplies by each color in the texture)
        }
        else
        {
            standardMaterial.DiffuseTexture = null;
            standardMaterial.DiffuseColor = Colors.Yellow; //Color3.FromByteRgb(39, 126, 147); // Colors.Gold;
        }

        standardMaterial.Opacity = isSemiTransparentMaterialChecked ? 0.8f : 1.0f;

        return standardMaterial;
    }

    protected virtual void UpdateMaterial()
    {
        if (modelNode == null) 
            return;

        modelNode.Material = GetMaterial();

        if (isSemiTransparentMaterialChecked)
            modelNode.BackMaterial = modelNode.Material;
        else
            modelNode.BackMaterial = null;
    }

    protected override Camera OnCreateCamera()
    {
        targetPositionCamera = new TargetPositionCamera()
        {
            Heading = 30,
            Attitude = -25,
            Distance = 300,
            ViewWidth = 300,

            // Offset the 3D scene to the left (move TargetPosition to the right)
            // but preserve rotation around (0, 0, 0).
            // This will move shown 3D object to the left and will not render it behind the options
            TargetPosition = new Vector3(30, 0, 0),
            RotationCenterPosition = new Vector3(0, 0, 0)
        };

        return targetPositionCamera;
    }

    protected ICommonSampleUIElement CreateComboBoxWithVectors(ICommonSampleUIProvider ui,
                                                               Vector3[] vectors,
                                                               Action<int, Vector3> itemChangedAction,
                                                               int selectedItemIndex,
                                                               double width = 0,
                                                               string? keyText = null,
                                                               double keyTextWidth = 0)
    {
        var itemTexts = vectors.Select(v => $"({v.X}, {v.Y}, {v.Z})").ToArray();

        var comboBox = ui.CreateComboBox(itemTexts,
                                         (index, itemText) => itemChangedAction(index, vectors[index]),
                                         selectedItemIndex,
                                         width,
                                         keyText,
                                         keyTextWidth);

        return comboBox;
    }
    
    protected ICommonSampleUIElement CreateComboBoxWithVectors(ICommonSampleUIProvider ui,
                                                               Vector2[] vectors,
                                                               Action<int, Vector2> itemChangedAction,
                                                               int selectedItemIndex,
                                                               double width = 0,
                                                               string? keyText = null,
                                                               double keyTextWidth = 0)
    {
        var itemTexts = vectors.Select(v => $"({v.X}, {v.Y})").ToArray();

        var comboBox = ui.CreateComboBox(itemTexts,
                                         (index, itemText) => itemChangedAction(index, vectors[index]),
                                         selectedItemIndex,
                                         width,
                                         keyText,
                                         keyTextWidth);

        return comboBox;
    }
}