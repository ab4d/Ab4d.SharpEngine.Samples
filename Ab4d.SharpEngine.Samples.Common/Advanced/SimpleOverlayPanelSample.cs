using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class SimpleOverlayPanelSample : CommonSample
{
    public override string Title { get; } = "Simple Overlay Panel";
    public override string Subtitle { get; } = "This samples shows how to use OverlayPanelBase to show an overlay panel on top of 3D scene.\nOverlay panel can show its own 3D objects. This can be used to create minimaps or some other helper panels.";
    
    private TestOverlayPanel? _testOverlayPanel;

    public SimpleOverlayPanelSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        // Create an instance of TestOverlayPanel and add it to sceneView.
        // The source code for TestOverlayPanel is provided below.
        //
        // isUsingSprites is set to false (this is also the default value).
        // If it was set to true, then SpriteBatch is used to render this overlay panel.
        // This requires waiting for the rendering to finish before the sprite can be drawn. This may be useful when the overlay panel is changed rarely.
        // When isUsingSprites is false,the overlay panel is rendered with the parentSceneView rendering process.
        // This is much more efficient when the overlay panel is changed often because it eliminates the waiting for the overlay panel to be rendered.
        _testOverlayPanel = new TestOverlayPanel(sceneView,
                                                 width: 256, 
                                                 height: 128, 
                                                 alignment: PositionTypes.BottomLeft, 
                                                 positionX: 16, 
                                                 positionY: 16, 
                                                 isUsingSprites: false,  // see comment above
                                                 name: "TestOverlayPanel");

        base.OnSceneViewInitialized(sceneView);
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 300;
        }

        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(200, 200),
        };

        scene.RootNode.Add(wireGridNode);


        var teapotMesh = await base.GetCommonMeshAsync(scene, CommonMeshes.Teapot, 
            position: new Vector3(0, 0, 0),
            positionType: PositionTypes.Bottom,
            finalSize: new Vector3(100, 100, 100));

        scene.RootNode.Add(new MeshModelNode(teapotMesh, StandardMaterials.Silver.SetSpecular(32)));
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        var positions = new int[] { 0, 10, 20 };
        ui.CreateComboBox(positions.Select(p => $"({p}, {p})").ToArray(), (itemIndex, itemText) =>
        {
            _testOverlayPanel!.Position = new Vector2(positions[itemIndex], positions[itemIndex]);
        }, selectedItemIndex: 1, keyText: "Position:", keyTextWidth: 80, width: 110);

        var alignmentOptions = new PositionTypes[] { PositionTypes.TopLeft, PositionTypes.BottomLeft, PositionTypes.TopRight, PositionTypes.BottomRight };
        ui.CreateComboBox(alignmentOptions.Select(o => o.ToString()).ToArray(), (itemIndex, itemText) =>
        {
            _testOverlayPanel!.Alignment = alignmentOptions[itemIndex];
        }, selectedItemIndex: 1, keyText: "Alignment:", keyTextWidth: 80, width: 110);

        var widths  = new int[] { 80, 128, 256, 256 };
        var heights = new int[] { 80, 128, 128, 256 };
        ui.CreateComboBox(Enumerable.Range(0, 4).Select(i => $"{widths[i]} x {heights[i]}").ToArray(), (itemIndex, itemText) =>
        {
            _testOverlayPanel!.Width = widths[itemIndex];
            _testOverlayPanel.Height = heights[itemIndex];
        }, selectedItemIndex: 2, keyText: "Size:", keyTextWidth: 80, width: 110);

        ui.AddSeparator();

        ui.CreateButton("Change box color", () => _testOverlayPanel?.ChangeBoxColor(GetRandomHsvColor3()));
        ui.CreateButton("Transform box color", () => _testOverlayPanel?.TransformBox(yOffset: 10));
        ui.CreateButton("Change background", () => _testOverlayPanel?.ChangeBackground(GetRandomHsvColor3(saturation: 0.2f)));
    }


    public class TestOverlayPanel : OverlayPanelBase
    {
        private BoxModelNode? _boxModelNode;
        private TranslateTransform? _translateTransform;

        public new Vector2 Position
        {
            get => base.Position;
            set => SetPosition(value);
        }
    
        /// <summary>
        /// Gets or sets the alignment of the panel relative to the parent SceneView.
        /// </summary>
        public new PositionTypes Alignment
        {
            get => base.Alignment;
            set => SetAlignment(value);
        }
    
        /// <summary>
        /// Gets or sets the width of the panel.
        /// </summary>
        public new int Width
        {
            get => base.Width;
            set => SetWidth(value);
        }

        /// <summary>
        /// Gets or sets the logical height of the panel.
        /// </summary>
        public new int Height
        {
            get => base.Height;
            set => SetHeight(value);
        }

        public Color3 BoxColor { get; private set; }

        public TestOverlayPanel(SceneView parentSceneView, 
                                int width = 100, 
                                int height = 100, 
                                PositionTypes alignment = PositionTypes.TopLeft,
                                float positionX = 0,
                                float positionY = 0,
                                bool isUsingSprites = false,
                                string? name = null)
            : base(parentSceneView, width, height, alignment, positionX, positionY, adjustSizeByDpiScale: true, isUsingSprites, name)
        {
            SetBackgroundColor(Colors.LightGray);
        }

        protected override void OnCreateScene(Scene scene)
        {
            BoxColor = Colors.Green;
            
            _boxModelNode = new BoxModelNode(new Vector3(0, 0, 0), new Vector3(100, 40, 80), new StandardMaterial(BoxColor), "TestBoxModelNode");

            _translateTransform = new TranslateTransform();
            _boxModelNode.Transform = _translateTransform;

            scene.RootNode.Add(_boxModelNode);

            if (overlayTargetPositionCamera != null)
            {
                overlayTargetPositionCamera.Heading = 30;
                overlayTargetPositionCamera.Attitude = -20;
                overlayTargetPositionCamera.Distance = 400;
            
                overlayTargetPositionCamera.StartRotation(40);
            }
        
            base.OnCreateScene(scene);
        }
    
        public void ChangeBoxColor(Color3 color)
        {
            if (_boxModelNode != null)
            {
                _boxModelNode.Material = new StandardMaterial(color);
                BoxColor = color;
            }
        }
        
        public void TransformBox(float yOffset)
        {
            if (_translateTransform != null)
                _translateTransform.Y += yOffset;
        }
    
        public void ChangeBackground(Color3 color)
        {
            SetBackgroundColor(color.ToColor4());
        }
    }
}