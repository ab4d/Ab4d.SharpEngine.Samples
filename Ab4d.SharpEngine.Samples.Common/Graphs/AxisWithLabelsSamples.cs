using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.glTF.Schema;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Graphs;

public class AxisWithLabelsSamples : CommonSample
{
    public override string Title => "AxisWithLabelsNode";
    public override string Subtitle => "AxisWithLabelsNode can render highly customizable axis with title, value labels and line ticks";
    
    private AxisWithLabelsNode _axisNode;

    private bool _isCustomBitmapTextCreator;
    private Vector2? _savedAxisPanelPosition;
    private ICommonSampleUIElement? _customTextCreatorButton;
    private ICommonSampleUIElement? _axisTitleColorComboBox;

    public AxisWithLabelsSamples(ICommonSamplesContext context)
        : base(context)
    {
        // Define _axisNode here so we do not need to define it as nullable 
        // // and then do null checks in the UI event handler
        _axisNode = new AxisWithLabelsNode(axisStartPosition: new Vector3(0, -50, 0), 
                                           axisEndPosition: new Vector3(0, 50, 0), 
                                           axisTitle: "AxisWithLabelsNode");


        ShowCameraAxisPanel = true;
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_savedAxisPanelPosition != null && CameraAxisPanel != null)
            CameraAxisPanel.Position = _savedAxisPanelPosition.Value;

        base.OnDisposed();
    }

    protected override void OnCreateScene(Scene scene)
    {
        // _axisNode is created in constructor, so we do not need to define it as nullable 
        // and then do null checks in the UI event handler

        scene.RootNode.Add(_axisNode);

        // Assign a Camera so the AxisWithLabelsNode is subscribed (because _axisNode.UpdateOnCameraChanges is true)
        // to camera changes and this automatically updates text directions for different camera angles.
        _axisNode.Camera = targetPositionCamera;

        // NOTE:
        // Many additional customizations are possible by deriving your class from AxisWithLabelsNode
        // and by overriding the virtual methods. The derived class can also access many protected properties.


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 430;
        }
    }

    private void ChangeBitmapTextCreator()
    {
        if (Scene == null)
            return;

        if (_isCustomBitmapTextCreator)
        {
            // Reset BitmapTextCreator to default bitmap text
            var defaultBitmapTextCreator = BitmapTextCreator.GetDefaultBitmapTextCreator(Scene);

            _axisNode.TitleBitmapTextCreator = defaultBitmapTextCreator;
            _axisNode.LabelsBitmapTextCreator = defaultBitmapTextCreator;

            _axisTitleColorComboBox?.SetValue(0); // Change color back to Black

            _customTextCreatorButton?.SetText("Use custom BitmapTextCreator");
            _isCustomBitmapTextCreator = false;
        }
        else
        {
            string fontFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\BitmapFonts\roboto_black_with_outline_128.fnt");

            if (System.IO.File.Exists(fontFile))
            {
                var bitmapFont = new BitmapFont(fontFile);
                var bitmapTextCreator = new BitmapTextCreator(Scene, bitmapFont, BitmapIO);

                _axisNode.TitleBitmapTextCreator = bitmapTextCreator;
                _axisTitleColorComboBox?.SetValue(2); // Change color from Black to Green color so that the outline is visible
            }


            fontFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\BitmapFonts\roboto_black_128.fnt");

            if (System.IO.File.Exists(fontFile))
            {
                var bitmapFont = new BitmapFont(fontFile);
                var bitmapTextCreator = new BitmapTextCreator(Scene, bitmapFont, BitmapIO);

                _axisNode.LabelsBitmapTextCreator = bitmapTextCreator;
            }

            _customTextCreatorButton?.SetText("Use default BitmapTextCreator");
            _isCustomBitmapTextCreator = true;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left);

        var comboBoxWidth = 80;
        var keyTextWidth = 165;

        
        var possibleLineThicknesses = new float[] { 0.5f, 1, 2, 3 };
        var possibleLineThicknessesTexts = possibleLineThicknesses.Select(v => v.ToString("G")).ToArray();
        
        var possibleColors = new Color4[] { Colors.Black, Colors.Red, Colors.Green, Colors.Blue };
        var possibleColorsTexts = possibleColors.Select(c => c.ToKnownColorString()).ToArray();

        var possibleOrientations = new Vector3[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
        var possibleOrientationTexts = new string[] { "X Axis", "Y Axis", "Z Axis" };
        
        var possibleFontSizes = new float[] { 4, 6, 10, 12 };
        var possibleFontSizeTexts = possibleFontSizes.Select(v => v.ToString("N0")).ToArray();
        
        var possiblePaddings = new float[] { -10, -5, 0, 3, 10, 20, 30 };
        var possiblePaddingTexts = possiblePaddings.Select(v => v.ToString("N0")).ToArray();
        
        var possibleValues = new float[] { -10, 0, 5, 10, 20, 22 };
        var possibleValueTexts = possibleValues.Select(v => v.ToString("N0")).ToArray();
        
        var possibleTickSteps = new float[] { 0.5f, 1, 2, 5, 10 };
        var possibleTickStepsTexts = possibleTickSteps.Select(v => v.ToString("G")).ToArray();
        
        var possibleTicksLengths = new float[] { 0, 1, 2.5f, 5, 10, 20 };
        var possibleTicksLengthsTexts = possibleTicksLengths.Select(v => v == 0 ? "0 (hidden)" : v.ToString("G")).ToArray();


        ui.CreateComboBox(possibleFontSizeTexts,
            (selectedIndex, selectedText) => _axisNode.AxisTitleFontSize = possibleFontSizes[selectedIndex], 
            selectedItemIndex: Array.IndexOf(possibleFontSizes, _axisNode.AxisTitleFontSize),
            comboBoxWidth, keyText: "AxisTitleFontSize:", keyTextWidth);

        _axisTitleColorComboBox = ui.CreateComboBox(possibleColorsTexts, 
            (selectedIndex, selectedText) => _axisNode.AxisTitleColor = possibleColors[selectedIndex],
            selectedItemIndex: 0,
            comboBoxWidth, keyText: "AxisTitleColor:", keyTextWidth);
        
        ui.CreateComboBox(possiblePaddingTexts, 
            (selectedIndex, selectedText) => _axisNode.AxisTitlePadding = possiblePaddings[selectedIndex],
            selectedItemIndex: Array.IndexOf(possiblePaddings, _axisNode.AxisTitlePadding),
            comboBoxWidth, keyText: "AxisTitlePadding:", keyTextWidth);


        ui.CreateComboBox(possibleOrientationTexts,
            (selectedIndex, selectedText) =>
            {
                _axisNode.AxisStartPosition = possibleOrientations[selectedIndex] * -50;
                _axisNode.AxisEndPosition = possibleOrientations[selectedIndex] * 50;
            }, 
            selectedItemIndex: 1,
            comboBoxWidth, keyText: "Orientation:", keyTextWidth);

        ui.CreateComboBox(possibleOrientationTexts,
            (selectedIndex, selectedText) => _axisNode.RightDirectionVector = possibleOrientations[selectedIndex], 
            selectedItemIndex: 0,
            comboBoxWidth, keyText: "RightDirectionVector:", keyTextWidth);

        ui.CreateComboBox(possibleLineThicknessesTexts, 
            (selectedIndex, selectedText) => _axisNode.AxisLineThickness = possibleLineThicknesses[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleLineThicknesses, _axisNode.AxisLineThickness),
            comboBoxWidth, keyText: "AxisLineThickness:", keyTextWidth);
        
        ui.CreateComboBox(possibleColorsTexts, 
            (selectedIndex, selectedText) => _axisNode.AxisLineColor = possibleColors[selectedIndex],
            selectedItemIndex: 0,
            comboBoxWidth, keyText: "AxisLineColor:", keyTextWidth);

   
        ui.CreateComboBox(possibleValueTexts, 
            (selectedIndex, selectedText) => _axisNode.MinimumValue = possibleValues[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleValues, _axisNode.MinimumValue),
            comboBoxWidth, keyText: "MinimumValue:", keyTextWidth);

        ui.CreateComboBox(possibleValueTexts, 
            (selectedIndex, selectedText) => _axisNode.MaximumValue = possibleValues[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleValues, _axisNode.MaximumValue),
            comboBoxWidth, keyText: "MaximumValue:", keyTextWidth);


        var possibleFormatStrings = new string[] { "#,##0", "N2", "$ 0.00" };

        ui.CreateComboBox(possibleFormatStrings,
            (selectedIndex, selectedText) => _axisNode.ValueDisplayFormatString = selectedText!, 
            selectedItemIndex: 0,
            comboBoxWidth, keyText: "ValueDisplayFormatString:", keyTextWidth);


        ui.AddSeparator();

        ui.CreateCheckBox("IsRenderingOnRightSideOfAxis",
            _axisNode.IsRenderingOnRightSideOfAxis,
            (isChecked) => _axisNode.IsRenderingOnRightSideOfAxis = isChecked);
        
        ui.CreateCheckBox("IsRightToLeftText",
            _axisNode.IsRightToLeftText,
            (isChecked) => _axisNode.IsRightToLeftText = isChecked);


        ui.AddSeparator();

        _customTextCreatorButton = ui.CreateButton("Use custom BitmapTextCreator", ChangeBitmapTextCreator);


        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        keyTextWidth = 130;


        ui.CreateComboBox(possibleFontSizeTexts,
            (selectedIndex, selectedText) => _axisNode.ValueLabelsFontSize = possibleFontSizes[selectedIndex], 
            selectedItemIndex: Array.IndexOf(possibleFontSizes, _axisNode.ValueLabelsFontSize),
            comboBoxWidth, keyText: "ValueLabelsFontSize:", keyTextWidth);

        ui.CreateComboBox(possibleColorsTexts, 
            (selectedIndex, selectedText) => _axisNode.ValueLabelsColor = possibleColors[selectedIndex],
            selectedItemIndex: 0,
            comboBoxWidth, keyText: "ValueLabelsColor:", keyTextWidth);



        ui.CreateComboBox(possiblePaddingTexts, 
            (selectedIndex, selectedText) => _axisNode.ValueLabelsPadding = possiblePaddings[selectedIndex],
            selectedItemIndex: Array.IndexOf(possiblePaddings, _axisNode.ValueLabelsPadding),
            comboBoxWidth, keyText: "ValueLabelsPadding:", keyTextWidth);


        ui.AddSeparator();

        ui.CreateComboBox(possibleTickStepsTexts, 
            (selectedIndex, selectedText) => _axisNode.MajorTicksStep = possibleTickSteps[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleTickSteps, _axisNode.MajorTicksStep),
            comboBoxWidth, keyText: "MajorTicksStep:", keyTextWidth);
        
        ui.CreateComboBox(possibleTickStepsTexts, 
            (selectedIndex, selectedText) => _axisNode.MinorTicksStep = possibleTickSteps[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleTickSteps, _axisNode.MinorTicksStep),
            comboBoxWidth, keyText: "MinorTicksStep:", keyTextWidth);


        ui.AddSeparator();


        ui.CreateComboBox(possibleTicksLengthsTexts, 
            (selectedIndex, selectedText) => _axisNode.MajorTicksLength = possibleTicksLengths[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleTicksLengths, _axisNode.MajorTicksLength),
            comboBoxWidth, keyText: "MajorTicksLength:", keyTextWidth);
        
        ui.CreateComboBox(possibleTicksLengthsTexts, 
            (selectedIndex, selectedText) => _axisNode.MinorTicksLength = possibleTicksLengths[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleTicksLengths, _axisNode.MinorTicksLength),
            comboBoxWidth, keyText: "MinorTicksLength:", keyTextWidth);
                
        ui.CreateComboBox(possibleLineThicknessesTexts, 
            (selectedIndex, selectedText) => _axisNode.TicksLineThickness = possibleLineThicknesses[selectedIndex],
            selectedItemIndex: Array.IndexOf(possibleLineThicknesses, _axisNode.TicksLineThickness),
            comboBoxWidth, keyText: "TicksLineThickness:", keyTextWidth);
        
        ui.CreateComboBox(possibleColorsTexts, 
            (selectedIndex, selectedText) => _axisNode.TicksLineColor = possibleColors[selectedIndex],
            selectedItemIndex: 0,
            comboBoxWidth, keyText: "TicksLineColor:", keyTextWidth);

        ui.AddSeparator();

        ui.CreateCheckBox("AdjustFirstLabelPosition (?):When checked, then the first label is moved up.\nThis can prevent overlapping the first label with adjacent axis.\nThe amount of movement is calculated by multiplying font size and the LabelAdjustmentFactor (0.45 by default).", 
            _axisNode.AdjustFirstLabelPosition, 
            isChecked => _axisNode.AdjustFirstLabelPosition = isChecked);
        
        ui.CreateCheckBox("AdjustLastLabelPosition (?):When checked, then the last label is moved down.\nThis can prevent overlapping the last label with adjacent axis.\nThe amount of movement is calculated by multiplying font size and the LabelAdjustmentFactor (0.45 by default).", 
            _axisNode.AdjustLastLabelPosition, 
            isChecked => _axisNode.AdjustLastLabelPosition = isChecked);


        ui.AddSeparator();

        ui.CreateCheckBox("Updating on camera changes (?):When checked, then the text directions are updated on camera changes so that text is correctly shown.", 
            true,
            isChecked => _axisNode.UpdateOnCameraChanges = isChecked);


        if (CameraAxisPanel != null)
        {
            _savedAxisPanelPosition = CameraAxisPanel.Position;
            CameraAxisPanel.Position = new Vector2(400, 10); // CameraAxisPanel is aligned to BottomLeft, so we only need to increase the y position from 10 to 80
        }
    }
}