using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class WireGridNodeSample : CommonSample
{
    public override string Title => "WireGridNode";

    private WireGridNode? _wireGridNode;

    private string _selectedSizeText = "300 300";
    private int _selectedWidthCellsCount = 30;
    private int _selectedHeightCellsCount = 30;
    private string _selectedMinorColorText = "DimGray";
    private float _selectedMinorLineThickness = 1;
    private string _selectedMajorColorText = "Black";
    private float _selectedMajorLineThickness = 2;
    private int _selectedMajorLinesFrequency = 5;
    private bool _isClosed = true;

    public WireGridNodeSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Grid
        _wireGridNode = new WireGridNode("Wire grid")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Size           = new Vector2(300, 300),

            WidthDirection  = new Vector3(1, 0, 0),  // this is also the default value
            HeightDirection = new Vector3(0, 0, -1), // this is also the default value

            WidthCellsCount  = 30,
            HeightCellsCount = 30,

            MajorLineColor     = Colors.Black,
            MajorLineThickness = 2,

            MinorLineColor     = Colors.DimGray,
            MinorLineThickness = 1,

            MajorLinesFrequency = 5,

            IsClosed = true,
        };

        UpdateWireGridSetting(); // Set setting from controls

        scene.RootNode.Add(_wireGridNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 600;
        }

        ShowCameraAxisPanel = true;
    }

    private void UpdateWireGridSetting()
    {
        if (_wireGridNode == null)
            return;

        // Size
        var sizeParts = _selectedSizeText.Split(' ');

        float width = float.Parse(sizeParts[0]);
        float height = float.Parse(sizeParts[1]);

        _wireGridNode.Size = new Vector2(width, height);

        // WidthCellsCount, HeightCellsCount
        _wireGridNode.WidthCellsCount  = _selectedWidthCellsCount;
        _wireGridNode.HeightCellsCount = _selectedHeightCellsCount;

        // MinorLines
        _wireGridNode.MinorLineColor     = Color4.Parse(_selectedMinorColorText);
        _wireGridNode.MinorLineThickness = _selectedMinorLineThickness;

        // MajorLines
        _wireGridNode.MajorLineColor     = Color4.Parse(_selectedMajorColorText);
        _wireGridNode.MajorLineThickness = _selectedMajorLineThickness;

        // MajorLinesFrequency
        _wireGridNode.MajorLinesFrequency = _selectedMajorLinesFrequency;

        // IsClosed
        _wireGridNode.IsClosed = _isClosed;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", 140);
        ui.CreateKeyValueLabel("WidthDirection:", () => "(1, 0, 0)", 140);
        ui.CreateKeyValueLabel("HeightDirection:", () => "(0, 0, -1)", 140);

        ui.AddSeparator();

        ui.CreateComboBox(new string[] { "100 50", "100 100", "500 100", "300 300", "500 500", "1000 1000" },
            (selectedIndex, selectedText) =>
            {
                _selectedSizeText = selectedText ?? "";
                UpdateWireGridSetting();
            },
            selectedItemIndex: 3,
            width: 100,
            keyText: "Size:",
            keyTextWidth: 140);

        ui.AddSeparator();

        ui.CreateSlider(5, 100, () => _selectedWidthCellsCount, sliderValue =>
            {
                _selectedWidthCellsCount = (int)sliderValue;
                UpdateWireGridSetting();
            },
            width: 100,
            showTicks: false,
            keyText: "WidthCellsCount:",
            keyTextWidth: 140,
            sliderValue => sliderValue.ToString("F0"));
        
        ui.CreateSlider(5, 100, () => _selectedHeightCellsCount, sliderValue =>
            {
                _selectedHeightCellsCount = (int)sliderValue;
                UpdateWireGridSetting();
            },
            width: 100,
            showTicks: false,
            keyText: "HeightCellsCount:",
            keyTextWidth: 140,
            sliderValue => sliderValue.ToString("F0"));

        ui.AddSeparator();

        ui.CreateComboBox(new string[] { "Gray", "DimGray", "Black", "SkyBlue" },
            (selectedIndex, selectedText) =>
            {
                _selectedMinorColorText = selectedText ?? "";
                UpdateWireGridSetting();
            },
            selectedItemIndex: 1,
            width: 100,
            keyText: "LineColor:",
            keyTextWidth: 140);

        ui.CreateSlider(0, 10, () => _selectedMinorLineThickness, sliderValue =>
            {
                _selectedMinorLineThickness = sliderValue;
                UpdateWireGridSetting();
            },
            width: 100,
            showTicks: false,
            keyText: "LinesThickness:",
            keyTextWidth: 140,
            sliderValue => sliderValue.ToString("F1"));

        ui.AddSeparator();

        ui.CreateComboBox(new string[] { "Gray", "DimGray", "Black", "SkyBlue" },
            (selectedIndex, selectedText) =>
            {
                _selectedMajorColorText = selectedText ?? "";
                UpdateWireGridSetting();
            },
            selectedItemIndex: 2,
            width: 100,
            keyText: "MajorLineColor:",
            keyTextWidth: 140);

        ui.CreateSlider(0, 10, () => _selectedMajorLineThickness, sliderValue =>
            {
                _selectedMajorLineThickness = sliderValue;
                UpdateWireGridSetting();
            },
            width: 100,
            showTicks: false,
            keyText: "MajorLinesThickness:",
            keyTextWidth: 140,
            sliderValue => sliderValue.ToString("F1"));

        ui.CreateSlider(0, 15, () => _selectedMajorLinesFrequency, sliderValue =>
            {
                _selectedMajorLinesFrequency = (int)sliderValue;
                UpdateWireGridSetting();
            },
            width: 100,
            showTicks: false,
            keyText: "MajorLinesFrequency:",
            keyTextWidth: 140,
            sliderValue => sliderValue.ToString("F0"));

        ui.AddSeparator();

        ui.CreateCheckBox("IsClosed", true, isChecked =>
        {
            _isClosed = isChecked;
            UpdateWireGridSetting();
        });
    }
}