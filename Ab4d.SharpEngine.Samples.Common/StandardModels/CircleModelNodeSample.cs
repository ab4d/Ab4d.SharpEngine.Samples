using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class CircleModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "CircleModelNode";

    private int _segmentsCount = 30;

    private float _innerRadius = 0;
    private float _radius = 50;
    private float _startAngle = 0;
    private CircleTextureMappingTypes _textureMappingType = CircleTextureMappingTypes.Rectangular;

    private Vector3 _normal = new Vector3(0, 1, 0);
    private Vector3 _upDirection = new Vector3(0, 0, -1);

    private CircleModelNode? _circleModelNode;

    private StandardMaterial? _gradientMaterial1;
    private StandardMaterial? _gradientMaterial2;
    private StandardMaterial? _gradientMaterial3;

    private ICommonSampleUIElement? _segmentsSlider;
    private ICommonSampleUIElement? _startAngleSlider;
    private ICommonSampleUIElement? _innerRadiusSlider;
    private ICommonSampleUIElement? _radiusSlider;

    private ICommonSampleUIElement? _textureMappingComboBox;
    private ICommonSampleUIElement? _textureImageComboBox;

    private LineNode? _normalLine;
    private LineNode? _upDirectionLine;
    private float _directionLinesLength = 60;

    private bool _showRectangle = false;
    private ICommonSampleUIElement? _buttonRectangle;

    protected int selectedTextureIdx = 0;

    public CircleModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _circleModelNode = new CircleModelNode("SampleCircle")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Radius = _radius,
            InnerRadius = _innerRadius // When InnerRadius is bigger than zero, then the center of the circle is hollow until the InnerRadius.
        };

        // Use MeshFactory.CreateCircleMesh to create a circle mesh, for example:
        //StandardMesh circleMesh = MeshFactory.CreateCircleMesh(centerPosition: new Vector3(0, 0, 0), _normal, _upDirection, radius: 50, _segmentsCount, name: "CircleMesh");

        UpdateModelNode();

        return _circleModelNode;
    }

    protected override void OnCreateScene(Scene scene)
    {
        base.OnCreateScene(scene);

        _normalLine = new LineNode("NormalLine")
        {
            LineColor = Colors.Red,
            LineThickness = 3,
            StartPosition = new Vector3(0, 0, 0),
            EndPosition = new Vector3(0, _directionLinesLength, 0),
            EndLineCap = LineCap.ArrowAnchor
        };

        scene.RootNode.Add(_normalLine);

        _upDirectionLine = new LineNode("UpDirectionLine")
        {
            LineColor = Colors.Green,
            LineThickness = 3,
            StartPosition = new Vector3(0, 0, 0),
            EndPosition = new Vector3(0, 0, -_directionLinesLength),
            EndLineCap = LineCap.ArrowAnchor
        };

        scene.RootNode.Add(_upDirectionLine);
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        _gradientMaterial1?.DisposeWithTexture();
        _gradientMaterial2?.DisposeWithTexture();

        base.OnDisposed();
    }

    protected override void UpdateModelNode()
    {
        if (_circleModelNode == null)
            return;

        _circleModelNode.InnerRadius = _innerRadius;
        _circleModelNode.Radius = _radius;
        _circleModelNode.StartAngle = _startAngle;

        _circleModelNode.TextureMappingType = _textureMappingType;
        _circleModelNode.Segments = _segmentsCount;

        _circleModelNode.Normal = _normal;
        _circleModelNode.UpDirection = _upDirection;

        if (_normalLine != null)
            _normalLine.EndPosition = _normalLine.StartPosition + _normal * _directionLinesLength;

        if (_upDirectionLine != null)
            _upDirectionLine.EndPosition = _upDirectionLine.StartPosition + _upDirection * _directionLinesLength;

        base.UpdateModelNode();
    }

    private Material? SetTextureImage(int textureIndex)
    {
        if (GpuDevice == null || _circleModelNode == null)
            return null;

        switch (textureIndex)
        {
            case 0:
                return modelMaterial; // Use default material (10x10-texture)
            case 1:
            {
                if (_gradientMaterial1 == null)
                {
                    // Create a gradient texture from red to transparent
                    //
                    // IMPORTANT:
                    // When the gradient texture is used for CircleModeNode, then the gradient must be vertical (isHorizontal: false).
                    // This is needed because the texture coordinates of the CircleModelNode for the Y coordinate go from the center to the outer circle edge
                    // (the X coordinate goes around the circle as the point angle goes from 0 to 360).
                    var gradientTexture = TextureFactory.CreateGradientTexture(GpuDevice, Colors.Red, Colors.Transparent, isHorizontal: false);
                    _gradientMaterial1 = new StandardMaterial(gradientTexture, name: "GradientMaterial");
                }

                return _gradientMaterial1;
            }
            case 2:
            {
                if (_gradientMaterial2 == null)
                {
                    // Create the gradient that will show two red circles
                    var gradient = new GradientStop[]
                    {
                        new GradientStop(Colors.Transparent, 0.0f),
                        new GradientStop(Colors.Transparent, 0.35f),
                        new GradientStop(Colors.Red, 0.4f),
                        new GradientStop(Colors.Red, 0.5f),
                        new GradientStop(Colors.Transparent, 0.55f),
                        new GradientStop(Colors.Transparent, 0.70f),
                        new GradientStop(Colors.Red, 0.75f),
                        new GradientStop(Colors.Red, 0.95f),
                        new GradientStop(Colors.Transparent, 1.0f),
                    };

                    // IMPORTANT:
                    // When the gradient texture is used for CircleModeNode, then the gradient must be vertical (isHorizontal: false).
                    // This is needed because the texture coordinates of the CircleModelNode for the Y coordinate go from the center to the outer circle edge
                    // (the X coordinate goes around the circle as the point angle goes from 0 to 360).
                    var gradientTexture = TextureFactory.CreateGradientTexture(GpuDevice, gradient, isHorizontal: false);
                    _gradientMaterial2 = new StandardMaterial(gradientTexture, name: "RedCirclesMaterial");
                }

                return _gradientMaterial2;
            }
            case 3:
            {
                if (_gradientMaterial3 == null)
                {
                    string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Textures/saturn-rings-1x512.png");
                    var gpuImage = TextureLoader.CreateTexture(fileName, GpuDevice);
                    _gradientMaterial3 = new StandardMaterial(gpuImage, name: "SaturnRingsTextureMaterial");
                }

                return _gradientMaterial3;
            }
        }

        return null;
    }

    protected override Material GetMaterial()
    {
        // This handles texture vs. non texture switch (to ensure that materials and such exist)
        var material = base.GetMaterial();

        // Override texture with selected one.
        if (isTextureMaterialChecked)
        {
            material = SetTextureImage(selectedTextureIdx);
        }

        // Apply semi-transparency flag
        if (material is StandardMaterial standardMaterial)
            standardMaterial.Opacity = isSemiTransparentMaterialChecked ? 0.8f : 1.0f;

        Debug.Assert(material != null, nameof(material) + " != null");
        return material;
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", keyTextWidth: 110);

        ui.AddSeparator();


        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1) },
            itemChangedAction: (selectedIndex, selectedVector) =>
            {
                _normal = Vector3.Normalize(selectedVector);
                UpdateModelNode();
            },
            selectedItemIndex: 0,
            width: 120,
            keyText: "Normal: ",
            keyTextWidth: 110).SetColor(Colors.Red);

        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 0, -1), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, -1) },
            itemChangedAction: (selectedIndex, selectedVector) =>
            {
                _upDirection = Vector3.Normalize(selectedVector);
                UpdateModelNode();
            },
            selectedItemIndex: 0,
            width: 120,
            keyText: "UpDirection: ",
            keyTextWidth: 110).SetColor(Colors.Green);

        ui.AddSeparator();


        _radiusSlider = ui.CreateSlider(10, 70,
            () => _radius,
            newValue =>
            {
                _radius = newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Radius:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => newValue.ToString("F1"));

        _innerRadiusSlider = ui.CreateSlider(0, 50,
            () => _innerRadius,
            newValue =>
            {
                _innerRadius = newValue;
                UpdateModelNode();
            },
            100,
            keyText: "InnerRadius:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => newValue.ToString("F1"));

        _startAngleSlider = ui.CreateSlider(0, 360,
            () => _startAngle,
            newValue =>
            {
                _startAngle = newValue;
                UpdateModelNode();
            },
            100,
            keyText: "StartAngle: (?):StartAngle is useful when Segments Count is low.\nFor example, when segments count is 4, the angle may be set to 45\nto get a rectangle that is aligned with axes.",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => newValue.ToString("F1"));

        ui.AddSeparator();


        _segmentsSlider = ui.CreateSlider(3, 50,
            () => _segmentsCount,
            newValue =>
            {
                if (_segmentsCount == (int)newValue)
                    return; // Do no update when only decimal part of the value is changed

                _segmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Segments:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");

        ui.AddSeparator();


        _textureMappingComboBox = ui.CreateComboBox(new string[] { "Rectangular", "RadialFromCenter", "RadialFromInnerRadius" },
            (selectedIndex, selectedText) =>
            {
                _textureMappingType = (CircleTextureMappingTypes)selectedIndex;
                UpdateModelNode();
            },
            selectedItemIndex: 0,
            width: 140,
            keyText: "TextureMapping:",
            keyTextWidth: 110);

        _textureImageComboBox = ui.CreateComboBox(new string[] { "10x10 grid", "Red to Transparent gradient", "Red circles", "Saturn rings" },
            (selectedIndex, selectedText) =>
            {
                selectedTextureIdx = selectedIndex;
                isTextureCheckBox?.SetValue(true);
                UpdateMaterial();
            },
            selectedItemIndex: 0,
            width: 140,
            keyText: "Texture image:",
            keyTextWidth: 110);


        ui.AddSeparator();
        ui.AddSeparator();

        ui.CreateButton("Show radial gradient (?):This button shows how to create a radial gradient with CircleModelNode.\nIt creates a linear gradient texture from red to transparent color,\nchecks the 'Is texture material' CheckBox and\nsets the 'TextureMapping' ComboBox to 'RadialFromInnerRadius'.", () =>
        {
            isTextureCheckBox?.SetValue(true);
            _textureImageComboBox?.SetValue(1); // 1 = 'Red to Transparent gradient'
            _textureMappingComboBox?.SetValue(2); // 2 = RadialFromInnerRadius
        });

        ui.CreateButton("Show Saturn rings", () =>
        {
            isTextureCheckBox?.SetValue(true);

            // Inner diameter of rings is: 184.000 km; Outer diameter of rings is 274.000 km (source: https://wisp.physics.wisc.edu/astro104/lecture22/F15_02.jpg)
            _radiusSlider?.SetValue(50);
            _innerRadiusSlider?.SetValue(33);

            _textureImageComboBox?.SetValue(3); // 3 = 'Saturn rings'
            _textureMappingComboBox?.SetValue(2); // 2 = RadialFromInnerRadius
        });

        const string labelRectangle = "Create rectangle (?):This button calls CreateRectangle method that sets the properties\nof this CircleModelNode so that it renders a rectangle.";
        const string labelCircle =  "Create circle (?):This button (re)sets the properties\nof this CircleModelNode so that it renders a circle.";

        _buttonRectangle = ui.CreateButton(labelRectangle, () =>
        {
            // Toggle the flag
            _showRectangle = !_showRectangle;

            _buttonRectangle!.SetText(_showRectangle ? labelCircle : labelRectangle);

            if (_showRectangle) {
                // CreateRectangle sets the properties of this CircleModelNode so that it renders a rectangle (Segments is set to 4 and StartAngle to 45)
                _circleModelNode!.CreateRectangle(position: new Vector3(0, 0, 0),
                                                  positionType: PositionTypes.Center,
                                                  size: new Vector2(80, 60),
                                                  innerSizeFactor: 0.5f,
                                                  widthDirection: new Vector3(1, 0, 0),
                                                  heightDirection: new Vector3(0, 0, -1));

                // We could also call the following overload of CreateRectangle:
                //_circleModelNode.CreateRectangle(centerPosition: new Vector3(0, 0, 0), size: new Vector2(80, 60), innerSizeFactor: 0.5f);
            }
            else
            {
                // Reset the properties to those of a circle
                _circleModelNode!.Segments = 30;
                _circleModelNode!.InnerRadius = 0;
                _circleModelNode!.Radius = 50;
                _circleModelNode!.StartAngle = 0;
            }

            // Calling CreateRectangle sets the properties on _circleModelNode so a rectangle is created.
            // Set the UI sliders to the new value. We need to first read the new values and then update the UI elements.
            // If we would just update the _radiusSlider, then the _circleModelNode properties would be set from the UI elements that were not updated yet.

            var newRadius      = _circleModelNode.Radius;
            var newInnerRadius = _circleModelNode.InnerRadius;
            var newSegments    = _circleModelNode.Segments;
            var newStartAngle  = _circleModelNode.StartAngle;

            _radiusSlider.SetValue(newRadius);
            _innerRadiusSlider.SetValue(newInnerRadius);
            _segmentsSlider.SetValue(newSegments);
            _startAngleSlider.SetValue(newStartAngle);


            base.UpdateModelNode(); // Update normals and triangles
        });

        ui.AddSeparator();


        ui.CreateLabel("TIP: See also 'Materials / Animated textures'\nsample that uses CircleModelNode with\nanimated textures.");


        AddMeshStatisticsControls(ui);
    }
}
