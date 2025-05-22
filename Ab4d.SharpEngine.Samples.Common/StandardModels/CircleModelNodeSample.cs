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
    
    private ICommonSampleUIElement? _textureMappingComboBox;
    private ICommonSampleUIElement? _textureImageComboBox;
    private ICommonSampleUIElement? _animationButton;
    
    private DateTime _animationStartTime;
    private GpuImage? _animatedGradientTexture;
    private GradientStop[]? _animatedGradientStops;
    private SceneView? _subscribedSceneView;
    
    private LineNode? _normalLine;
    private LineNode? _upDirectionLine;
    private float _directionLinesLength = 60;
    


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
    
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.SceneUpdating += SceneViewOnSceneUpdating;
        _subscribedSceneView = sceneView;

        base.OnSceneViewInitialized(sceneView);
    }
    
    private void SceneViewOnSceneUpdating(object? sender, EventArgs e)
    {
         if (_animationStartTime != DateTime.MinValue)
            UpdateAnimatedGradient();
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        _gradientMaterial1?.DisposeWithTexture();
        _gradientMaterial2?.DisposeWithTexture();
        _gradientMaterial3?.DisposeWithTexture();
        
        if (_subscribedSceneView != null)
        {
            _subscribedSceneView.SceneUpdating -= SceneViewOnSceneUpdating;
            _subscribedSceneView = null;
        }
        
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

    private void SetTextureImage(int textureIndex)
    {
        if (GpuDevice == null || _circleModelNode == null)
            return;

        Material? newMaterial = null;
        
        _animationStartTime = DateTime.MinValue; // This will stop the animation
        
        if (textureIndex == 0)
        {
            newMaterial = modelMaterial; // Use default material
        }
        else if (textureIndex == 1)
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
            
            newMaterial = _gradientMaterial1;
        }
        else if (textureIndex == 2)
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
            
            newMaterial = _gradientMaterial2;
        }
        else if (textureIndex == 3)
        {
            _gradientMaterial3 ??= new StandardMaterial("AnimatedGradientMaterial");
            UpdateAnimatedGradient(); // This will also create the _animatedGradientTexture
            
            newMaterial = _gradientMaterial3;
        }


        _circleModelNode.Material = newMaterial; 
        
        if (_circleModelNode.BackMaterial != null) // Also update BackMaterial if it was also used before
            _circleModelNode.BackMaterial = newMaterial;
    }
    
    private void UpdateAnimatedGradient()
    {
        if (GpuDevice == null)
            return;
        
        var elapsedSeconds = (DateTime.Now - _animationStartTime).TotalSeconds;

        // gradientProgress defines the center of the red circle
        // the red circle thickness is 0.2 and there is also a transition from transparent to red for 0.1
        // go from 0.2 to 0.8 over 1 second
        var gradientProgress = 0.2f + (float)(elapsedSeconds % 1) * 0.6f; 

        _animatedGradientStops ??= new GradientStop[4];

        _animatedGradientStops[0] = new GradientStop(Colors.Transparent, gradientProgress - 0.2f);
        _animatedGradientStops[1] = new GradientStop(Colors.Red,         gradientProgress - 0.1f);
        _animatedGradientStops[2] = new GradientStop(Colors.Red,         gradientProgress + 0.1f);
        _animatedGradientStops[3] = new GradientStop(Colors.Transparent, gradientProgress + 0.2f);

        if (_animatedGradientTexture != null)
            _animatedGradientTexture.Dispose();

        _animatedGradientTexture = TextureFactory.CreateGradientTexture(GpuDevice, _animatedGradientStops, isHorizontal: false, name: "AnimatedGradientTexture");
        
        if (_gradientMaterial3 != null)
            _gradientMaterial3.DiffuseTexture = _animatedGradientTexture;
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", keyTextWidth: 110);
        
        ui.AddSeparator();
        
        
        ui.CreateSlider(10, 70,
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
        
        ui.CreateSlider(0, 50,
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
        
        ui.CreateSlider(0, 360,
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

        
        ui.CreateSlider(3, 40,
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

        AddMeshStatisticsControls(ui);

        ui.AddSeparator();
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
        
        ui.AddSeparator();
        
        _textureImageComboBox = ui.CreateComboBox(new string[] { "10x10 grid", "Red to Transparent gradient", "Red circles", "Animated circles" },
            (selectedIndex, selectedText) =>
            {
                SetTextureImage(selectedIndex);
                isTextureCheckBox?.SetValue(true);
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
        
        ui.CreateButton("Show two red circles", () =>
        {
            isTextureCheckBox?.SetValue(true);
            _textureImageComboBox?.SetValue(2); // 1 = 'Red circles'
            _textureMappingComboBox?.SetValue(2); // 2 = RadialFromInnerRadius
        });
        
        ui.AddSeparator();
        
        _animationButton = ui.CreateButton("Start gradient animation", () =>
        {
            if (_animationStartTime == DateTime.MinValue)
            {
                isTextureCheckBox?.SetValue(true);
                _textureImageComboBox?.SetValue(3);   // 1 = 'Animated circles'
                _textureMappingComboBox?.SetValue(2); // 2 = RadialFromInnerRadius
                
                _animationStartTime = DateTime.Now; // Start animation

                _animationButton?.SetText("Stop gradient animation");
            }
            else
            {
                _animationStartTime = DateTime.MinValue; // Stop animation
                _animationButton?.SetText("Start gradient animation");
            }
        });
    }
}