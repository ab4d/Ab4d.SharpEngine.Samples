using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class TransparencySortingSample : CommonSample
{
    public override string Title => "Transparency sorting";
    public override string Subtitle => "Transparency sorting sorts the semi-transparent objects so that farther objects are rendered before closer objects.\nInstead of transparency-sorting, it is also possible to disable depth writing. See tooltip by (?) for more info.";

    private float _opacity = 0.2f;
    private GroupNode _transparentObjectsGroupNode;

    public TransparencySortingSample(ICommonSamplesContext context)
        : base(context)
    {
        _transparentObjectsGroupNode = new GroupNode("TransparentObjects");

        MoveCameraConditions = PointerAndKeyboardConditions.Disabled; // disable mouse move
    }

    protected override void OnCreateScene(Scene scene)
    {
        RecreateTransparentObjects();

        scene.RootNode.Add(_transparentObjectsGroupNode);

        scene.SetAmbientLight(0.2f);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading  = 130; // start from back where sorting errors are most obvious
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.Distance = 500;
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Always;
            targetPositionCamera.StartRotation(45);
        }
    }

    private void RecreateTransparentObjects()
    {
        _transparentObjectsGroupNode.Clear();

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var greenRatio = x / 4.0f;
                var semiTransparentMaterial = new StandardMaterial(new Color3(0.5f, greenRatio, 1 - greenRatio)) { Opacity = _opacity };
                var position = new Vector3(-100 + 50 * x, 0, -100 + 50 * y);

                var boxModel = new BoxModelNode(position, size: new Vector3(30, 30, 30), semiTransparentMaterial, $"Box_{x}_{y}")
                {
                    // Note that when rendering semi-transparent objects and when alpha clipping is not used,
                    // it is better to set BackMaterial to the same material as Material instead of setting material.IsTwoSided to true
                    // because this always rendered the back side before the front side and the inner triangles are correctly visible.
                    BackMaterial = semiTransparentMaterial
                };
                _transparentObjectsGroupNode.Add(boxModel);
            }
        }
    }
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateSlider(0, 1, () => _opacity,
            newValue =>
            {
                _opacity = newValue;
                RecreateTransparentObjects();
            },
            width: 140,
            keyText: "Opacity",
            formatShownValueFunc: (sliderValue) => sliderValue.ToString("N2"));

        ui.AddSeparator();

        ui.CreateCheckBox(
@"IsTransparencySortingEnabled (?):Transparent objects are rendered by combining the existing pixel color
(background color or color of previously rendered objects)
with the color of the transparent object.
Because of this, the order of rendering transparent objects is important.

Also, be default, rendering transparent objects writes their distance to the depth buffer.
This prevent objects behind transparent object to be rendered.

By sorting the transparent objets by their distance to the camera and rendering
the farthest objects before closer objects, the objects are rendered correctly.

But there are exceptions:
- Some objects also require sorting of triangles (see next sample),
- Object distance is taken by measuring the distance from the bounding box's center to the camera.
  But when objects are different sizes, this may not be correct. In this case manual sorting may need be done.
  This is done by disabling transparency sorting and manually adjusting the order of problematic objects.", true, isChecked =>
        {
            if (Scene != null)
                Scene.IsTransparencySortingEnabled = isChecked;
        });

        ui.CreateCheckBox(
@"Disable depth write (?):By default, when a transparent object is rendered it also write its depth to the depth buffer.
This prevents rendering objects that are rendered after that object and are farther away from the camera to be rendered.

When this CheckBox is checked, then this will prevent writing to depth buffer for all transparent objects
(set Scene.DefaultTransparentDepthStencilState to DepthRead instead of DepthReadWrite).

This can helps to prevent some transparency problems and works well event without transparency sorting.

However, this does not work correctly in all of the cases because objects that are farther away from the camera 
can be rendered after closer objects, and this can affect the final color of the pixels.
You can observe that by unchecking the 'IsTransparencySortingEnabled' and 'Disable depth write' Checkboxes.
Then rotate the camera around and you will see that the final color will be set from the boxes farther away from the camera.
When the transparent objects are sorted, then the final color is correct.", false, isChecked =>
        {
            if (Scene != null)
            {
#if VULKAN
                Scene.DefaultTransparentDepthStencilState = isChecked ? Ab4d.SharpEngine.Utilities.CommonStatesManager.DepthRead : Ab4d.SharpEngine.Utilities.CommonStatesManager.DepthReadWrite;
#elif WEB_GL
                Scene.EnableDepthWriteForTransparentObjects = !isChecked;
#endif
            }
        });

        ui.AddSeparator();

        ui.CreateCheckBox("Rotate camera", true, isChecked =>
        {
            if (isChecked)
                targetPositionCamera?.StartRotation(45);
            else
                targetPositionCamera?.StopRotation();
        });

        ui.CreateButton("Dump RenderingLayers to Output (?):Dump the RenderingLayers to the IDE's Output window.\nThis shows that the order of boxes is different depending on the camera angle.\nClick this button, rotate the camera around and click the button again\nto see the different order in the output window.", () => Scene?.DumpRenderingLayers());
    }
}