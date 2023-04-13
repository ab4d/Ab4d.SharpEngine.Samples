using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class InstancedMeshNodeSample : CommonSample
{
    public override string Title => "Mesh instancing with InstancedMeshNode";
    public override string Subtitle => "Very efficient rendering of the same mesh where each mesh instance can have its own offset, scale and rotation (WorldMatrix) and its own color.";

    private InstancedMeshNode? _instancedMeshNode;
    private WorldColorInstanceData[]? _instancesData;
    private StandardMesh? _sphereMesh;
    private StandardMesh? _pyramidMesh;
    private StandardMesh? _teapotMesh;
    private ICommonSampleUIElement? _totalPositionsLabel;
    private bool _useTransparency;

    public InstancedMeshNodeSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _sphereMesh = Meshes.MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), 5, 30);

        _instancedMeshNode = new InstancedMeshNode("InstancedMeshNode");
        _instancedMeshNode.Mesh = _sphereMesh;

        _instancesData = CreateInstancesData(center: new Vector3(0, 0, 0), 
                                             size: new Vector3(400, 400, 400), 
                                             modelScaleFactor: (float)1, 
                                             xCount: 20, yCount: 20, zCount: 20, 
                                             useTransparency: _useTransparency);

        _instancedMeshNode.SetInstancesData(_instancesData);

        scene.RootNode.Add(_instancedMeshNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 90;
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Always;
        }
    }

    private static WorldColorInstanceData[] CreateInstancesData(Vector3 center, Vector3 size, float modelScaleFactor, int xCount, int yCount, int zCount, bool useTransparency)
    {
        var instancedData = new WorldColorInstanceData[xCount * yCount * zCount];

        float xStep = xCount <= 1 ? 0 : (float)(size.X / (xCount - 1));
        float yStep = yCount <= 1 ? 0 : (float)(size.Y / (yCount - 1));
        float zStep = zCount <= 1 ? 0 : (float)(size.Z / (zCount - 1));

        float alphaFactor = 1.0f / instancedData.Length;

        int i = 0;
        for (int z = 0; z < zCount; z++)
        {
            float zPos = (float)(center.Z - (size.Z / 2.0) + (z * zStep));
            float zPercent = (float)z / (float)zCount;

            for (int y = 0; y < yCount; y++)
            {
                float yPos = (float)(center.Y - (size.Y / 2.0) + (y * yStep));
                float yPercent = (float)y / (float)yCount;

                for (int x = 0; x < xCount; x++)
                {
                    float xPos = (float)(center.X - (size.X / 2.0) + (x * xStep));

                    instancedData[i].World = new Matrix4x4(modelScaleFactor, 0, 0, 0,
                                                           0, modelScaleFactor, 0, 0,
                                                           0, 0, modelScaleFactor, 0,
                                                           xPos, yPos, zPos, 1);

                    float alpha = useTransparency ? alphaFactor * i : 1;
                    instancedData[i].DiffuseColor = new Color4(red: 0.3f + ((float)x / (float)xCount) * 0.7f,
                                                               green: 0.3f + yPercent * 0.7f,
                                                               blue: 0.3f + zPercent * 0.7f,
                                                               alpha: alpha);

                    i++;
                }
            }
        }

        return instancedData;
    }

    private void ChangeMesh(int selectedIndex)
    {
        if (_instancedMeshNode == null || _instancesData == null)
            return;
        
        if (selectedIndex == 0)
        {
            _pyramidMesh ??= MeshFactory.CreatePyramidMesh(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
            _instancedMeshNode.Mesh = _pyramidMesh;
        }
        else if (selectedIndex == 1)
        {
            _instancedMeshNode.Mesh = _sphereMesh;
        }
        else if (selectedIndex == 2)
        {
            if (_teapotMesh == null)
            {
                var teapotFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Models/Teapot.obj");

                var readerObj = new ReaderObj();
                var teapotNode = readerObj.ReadSceneNodes(teapotFileName);

                if (teapotNode.Count > 0 && teapotNode[0] is MeshModelNode teapotMeshModelNode)
                {
                    if (teapotMeshModelNode.Mesh is StandardMesh teapotMesh)
                    {
                        Ab4d.SharpEngine.Utilities.ModelUtils.PositionAndScaleSceneNode(teapotMeshModelNode, position: new Vector3(0, 0, 0), positionType: PositionTypes.Center, finalSize: new Vector3(20, 20, 20));
                        teapotMesh = Ab4d.SharpEngine.Utilities.MeshUtils.TransformMesh(teapotMesh, teapotMeshModelNode.Transform);

                        _teapotMesh = teapotMesh;
                    }
                }
            }

            _instancedMeshNode.Mesh = _teapotMesh;
        }

        _totalPositionsLabel?.UpdateValue();
    }

    private void RecreateInstancesData(int selectedIndex)
    {
        if (_instancedMeshNode == null)
            return;

        int yInstancesCount;
        if (selectedIndex > 3)
            yInstancesCount = 2000;
        else
            yInstancesCount = (int)Math.Pow(2, selectedIndex) * 5;

        _instancesData = CreateInstancesData(new Vector3(0, 0, 0), new Vector3(400, 400, 400), (float)1, 20, yInstancesCount, 20, useTransparency: _useTransparency);
        //var instancesData = CreateSimpleInstancedData();

        _instancedMeshNode.SetInstancesData(_instancesData);
        _totalPositionsLabel?.UpdateValue();
    }

    private void ChangeInstancesCount(bool isChecked)
    {
        if (_instancesData == null || _instancedMeshNode == null)
            return;

        if (isChecked)
            _instancedMeshNode.InstancesCount = _instancesData.Length / 2;
        else
            _instancedMeshNode.InstancesCount = _instancesData.Length;

        _totalPositionsLabel?.UpdateValue();
    }

    private void ChangeStartInstanceIndex(bool isChecked)
    {
        if (_instancesData == null || _instancedMeshNode == null)
            return;

        if (isChecked)
            _instancedMeshNode.StartInstanceIndex = (int)(_instancesData.Length * 0.25);
        else
            _instancedMeshNode.StartInstanceIndex = 0;

        _totalPositionsLabel?.UpdateValue();
    }
    
    private void ChangeScale(bool isChecked)
    {
        var instancesData = _instancesData;

        if (instancesData == null || _instancedMeshNode == null)
            return;

        float scale = isChecked ? 5 : 1;

        for (int i = 0; i < 100; i++)
        {
            // M11, M22 and M33 value in WorldMatrix define the ScaleX, ScaleY and ScaleZ of the transformation
            instancesData[i].World.M11 = scale;
            instancesData[i].World.M22 = scale;
            instancesData[i].World.M33 = scale;
        }

        _instancedMeshNode.UpdateInstancesData(updateBoundingBox: true);
    }
    
    private void ChangeTranslation(bool isChecked)
    {
        var instancesData = _instancesData;

        if (instancesData == null || _instancedMeshNode == null)
            return;

        float yOffset = isChecked ? 100 : -100;

        for (int i = 0; i < 100; i++)
        {
            // M41, M42 and M43 value in WorldMatrix define the OffsetX, OffsetY and OffsetZ of the transformation
            //instancesData[i].World.M41 += xOffset;
            //instancesData[i].World.M42 += yOffset;
            instancesData[i].World.M43 -= yOffset;
        }

        _instancedMeshNode.UpdateInstancesData(updateBoundingBox: true);
    }
    
    private void ShowHideMiddleThird(bool isChecked)
    {
        var instancesData = _instancesData;

        if (instancesData == null || _instancedMeshNode == null)
            return;

        int startIndex = (int)(instancesData.Length * 0.3);
        int endIndex = (int)(instancesData.Length * 0.6);

        // To hide (discard) rendering of specific mesh, set its Alpha color to 0
        for (int i = startIndex; i < endIndex; i++)
        {
            instancesData[i].DiffuseColor = new Color4(instancesData[i].DiffuseColor.Red,
                                                       instancesData[i].DiffuseColor.Green,
                                                       instancesData[i].DiffuseColor.Blue,
                                                       alpha: isChecked ? 0 : 1);

        }

        // After data in instancesData array are changed, we need to call UpdateInstancesData 
        // to save the new value in GPU Buffer
        _instancedMeshNode.UpdateInstancesData(updateBoundingBox: false);
    }

    public void ChangeUseSingleObjectColor(bool isChecked)
    {
        if (_instancedMeshNode == null)
            return;

        if (isChecked)
        {
            if (_instancedMeshNode.UseAlphaBlend)
                _instancedMeshNode.UseSingleObjectColor(Colors.Orange.SetAlpha(0.5f)); // all instances will be rendered by Orange color
            else
                _instancedMeshNode.UseSingleObjectColor(Colors.Orange); // all instances will be rendered by Orange color
        }
        else
        {
            _instancedMeshNode.UseInstanceObjectColor(); // To disabled single color rendering, call UseInstanceObjectColor
        }
    }

    private void ChangeTransparency(bool isChecked)
    {
        _useTransparency = isChecked;

        if (_instancedMeshNode == null || _instancesData == null)
            return;

        // When using transparent colors (Alpha < 1), we need to enable alpha-blending
        _instancedMeshNode.UseAlphaBlend = isChecked;

        if (_instancedMeshNode.IsUsingSingleObjectColor)
        {
            if (isChecked)
                _instancedMeshNode.UseSingleObjectColor(Colors.Orange.SetAlpha(0.5f));
            else
                _instancedMeshNode.UseSingleObjectColor(Colors.Orange);
        }
        else
        {
            float factor = 1.0f / _instancesData.Length;

            if (isChecked)
            {
                // Alpha color value is changed based on the index
                for (int i = 0; i < _instancesData.Length; i++)
                {
                    _instancesData[i].DiffuseColor = new Color4(_instancesData[i].DiffuseColor.Red,
                                                                _instancesData[i].DiffuseColor.Green,
                                                                _instancesData[i].DiffuseColor.Blue,
                                                                alpha: i * factor);
                }
            }
            else
            {
                // Alpha color is always 1 (no transparency)
                for (int i = 0; i < _instancesData.Length; i++)
                {
                    _instancesData[i].DiffuseColor = new Color4(_instancesData[i].DiffuseColor.Red,
                                                                _instancesData[i].DiffuseColor.Green,
                                                                _instancesData[i].DiffuseColor.Blue,
                                                                alpha: 1);
                }
            }

            _instancedMeshNode.UpdateInstancesData(updateBoundingBox: false);
        }
    }

    private string GetTotalPositionsText()
    {
        if (_instancedMeshNode == null || _instancedMeshNode.Mesh == null || _instancesData == null)
            return "";

        return $"{_instancedMeshNode.Mesh.VertexCount:#,##0} * {_instancesData.Length:#,##0} = {_instancedMeshNode.Mesh.VertexCount * _instancesData.Length:#,##0}";
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Mesh:");
        ui.CreateRadioButtons(new string[] { "Pyramid (16 positions)", "Sphere (961 positions)", "Teapot (2,976 positions)" }, (selectedIndex, selectedText) => ChangeMesh(selectedIndex), selectedItemIndex: 1);

        ui.AddSeparator();

        ui.CreateLabel("Instances count:");
        ui.CreateComboBox(new string[] { "2000 (20 x 5 x 20)", "4000 (20 x 10 x 20)", "8000 (20 x 20 x 20)", "16000 (20 x 40 x 20)", "800.000 (20 x 2000 x 20)" },
            (selectedIndex, selectedText) => RecreateInstancesData(selectedIndex),
            selectedItemIndex: 2);

        ui.AddSeparator();

        ui.CreateLabel("Total positions:");
        _totalPositionsLabel = ui.CreateKeyValueLabel("", () => GetTotalPositionsText());

        ui.AddSeparator();
        ui.AddSeparator();

        ui.CreateCheckBox("Hide middle third meshes", false, isChecked => ShowHideMiddleThird(isChecked));
        ui.CreateCheckBox("Scale first 100 meshes", false, isChecked => ChangeScale(isChecked));
        ui.CreateCheckBox("Translate first 100 meshes", false, isChecked => ChangeTranslation(isChecked));


        ui.AddSeparator();

        ui.CreateCheckBox("Change start index", false, isChecked => ChangeStartInstanceIndex(isChecked));
        ui.CreateCheckBox("Change instances count", false, isChecked => ChangeInstancesCount(isChecked));


        ui.AddSeparator();

        ui.CreateCheckBox("Use transparent colors", false, isChecked => ChangeTransparency(isChecked));
        ui.CreateCheckBox("Use single colors (?): By calling UseSingleObjectColor method, it is possible to render all meshes with single color and not the color that is defined in InstancesData", false, isChecked => ChangeUseSingleObjectColor(isChecked));
        ui.CreateCheckBox("Use solid color material (?): When IsSolidColorMaterial is true, then no shading is applied to materials", false, isChecked =>
        {
            if (_instancedMeshNode != null)
                _instancedMeshNode.IsSolidColorMaterial = isChecked;
        });
        ui.CreateCheckBox("Render only back-face materials", false, isChecked => {
            if (_instancedMeshNode != null)
                _instancedMeshNode.IsBackFaceMaterial = isChecked;
        });
    }
}