using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class SpecularMaterialSample : CommonSample
{
    public override string Title => "Specular materials";
    public override string Subtitle => "StandardMaterials with different combinations of SpecularPower and SpecularColor";

    public SpecularMaterialSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var textBlockFactory = context.GetTextBlockFactory();
        //textBlockFactory.BackgroundColor = Colors.LightYellow;
        //textBlockFactory.BorderThickness = 1;
        //textBlockFactory.BorderColor     = Colors.DimGray;
        textBlockFactory.FontSize        = 20;


        var specularPowers = new float[] { 128, 64, 32, 16, 8, 4 };

        for (int i = 0; i < specularPowers.Length; i++)
        {
            float specularPower = specularPowers[i];

            var specularPowerTextNode = textBlockFactory.CreateTextBlock($"{specularPower:F0}", new Vector3(-350, i * 100 - 270, 0), positionType: PositionTypes.Right, textAttitude: 90);
            scene.RootNode.Add(specularPowerTextNode);

            for (float specularColorFactor = 0; specularColorFactor <= 1; specularColorFactor += 0.2f)
            {
                var specularMaterial = new StandardMaterial(Colors.Silver)
                {
                    SpecularPower = specularPower,
                    SpecularColor = new Color3(specularColorFactor)
                };

                var position = new Vector3(specularColorFactor * 500 - 250, i * 100 - 270, 0);

                var sphereModelNode = new SphereModelNode(centerPosition: position, radius: 40, specularMaterial);

                scene.RootNode.Add(sphereModelNode);
            }
        }


        var specularColorTextNode1 = textBlockFactory.CreateTextBlock("black (#000000) ...", new Vector3(-330, -350, 0), positionType: PositionTypes.Left, textAttitude: 90);
        scene.RootNode.Add(specularColorTextNode1);
        
        var specularColorTextNode2 = textBlockFactory.CreateTextBlock("... white (#FFFFFF)", new Vector3(330, -350, 0), positionType: PositionTypes.Right, textAttitude: 90);
        scene.RootNode.Add(specularColorTextNode2);

        var label1 = textBlockFactory.CreateTextBlock("SpecularPower:", new Vector3(-350, 280, 0), positionType: PositionTypes.Right, textAttitude: 90);
        scene.RootNode.Add(label1);

        var label2 = textBlockFactory.CreateTextBlock("SpecularColor:", new Vector3(-350, -350, 0), positionType: PositionTypes.Right, textAttitude: 90);
        scene.RootNode.Add(label2);

        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 20;
            targetPositionCamera.Attitude = 0;
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Never;
        }

        scene.Lights.Clear();
        scene.Lights.Add(new AmbientLight(0.4f));
        scene.Lights.Add(new PointLight(position: new Vector3(0, 0, 500)));
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        var rootStackPanel = ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateButton("Add PointLight", () =>
        {
            if (Scene != null)
            {
                var pointLight = new PointLight(position: new Vector3(Random.Shared.NextSingle() * 1000 - 500, Random.Shared.NextSingle() * 1000 - 500, 200));
                Scene.Lights.Add(pointLight);
            }
        });
        
        ui.CreateButton("Remove PointLight", () =>
        {
            if (Scene != null && Scene.Lights.Count > 1) // preserve AmbientLight
                Scene.Lights.RemoveAt(Scene.Lights.Count - 1);
        });
    }
}