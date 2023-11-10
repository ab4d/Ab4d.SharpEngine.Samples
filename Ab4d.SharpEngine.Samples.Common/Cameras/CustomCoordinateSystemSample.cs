using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.SceneNodes;
using static System.Formats.Asn1.AsnWriter;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class CustomCoordinateSystemSample : CommonSample
{
    public override string Title => "Custom coordinate system";
    public override string? Subtitle => "This sample shows how to change from default coordinate system (y-up, x-right, z-out of screen) to a custom coordinate system.";

    private ICommonSampleUIElement? _descriptionLabel;

    public CustomCoordinateSystemSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 20, 0), 
            size: new Vector3(80, 40, 60), 
            name: "Gold BoxModel")
        {
            Material = StandardMaterials.Gold,
        };

        scene.RootNode.Add(boxModel);


        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(200, 200),
        };

        scene.RootNode.Add(wireGridNode);

        ShowCameraAxisPanel = true;

        // CAD application usually use z-up right-handed coordinate system:
        // Z axis up, X axis to the right and Y axis into the screen
        ChangeCoordinateSystem(CoordinateSystems.ZUpRightHanded);

        // This is the same as:
        //ChangeCoordinateSystem(CoordinateSystems.ZUpRightHanded);
    }

    protected override void OnDisposed()
    {
        Scene?.SetCoordinateSystem(Scene.DefaultCoordinateSystem);

        // This is the same as:
        //Scene?.SetCoordinateSystem(CoordinateSystems.Default);
        //Scene?.SetCoordinateSystem(CoordinateSystems.YUpRightHanded);

        base.OnDisposed();
    }

    private void ChangeCoordinateSystem(CoordinateSystems newCoordinateSystem)
    {
        if (Scene == null)
            return;

        Scene.SetCoordinateSystem(newCoordinateSystem);

        if (_descriptionLabel != null)
        {
            string description = newCoordinateSystem switch
            {
                CoordinateSystems.YUpRightHanded => "The default coordinate system with Y axis up,\nX axis to the right and Z axis out of the screen.",
                CoordinateSystems.YUpLeftHanded  => "Y axis up, X axis to the right and\nZ axis into the screen.",
                CoordinateSystems.ZUpRightHanded => "Standard CAD coordinate system with Z axis up,\nX axis to the right and Y axis into the screen.",
                CoordinateSystems.ZUpLeftHanded  => "Z axis up, X axis to the right and\nY axis out of the screen.",
                _ => ""
            };

            _descriptionLabel.SetText(description);
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Coordinate system:", isHeader: true);

        var allCoordinateSystems = new string[] { "YUpRightHanded (Default)", "YUpLeftHanded", "ZUpRightHanded (CAD standard)", "ZUpLeftHanded "};
        ui.CreateRadioButtons(allCoordinateSystems, (selectedIndex, selectedText) =>
        {
            var newCoordinateSystem = (CoordinateSystems)selectedIndex;
            ChangeCoordinateSystem(newCoordinateSystem);
        }, selectedItemIndex: 2);

        _descriptionLabel = ui.CreateLabel("").SetStyle("italic");

        if (Scene != null)
            ChangeCoordinateSystem(Scene.GetUsedCoordinateSystem()); // This will update the _descriptionLabel
    }
}