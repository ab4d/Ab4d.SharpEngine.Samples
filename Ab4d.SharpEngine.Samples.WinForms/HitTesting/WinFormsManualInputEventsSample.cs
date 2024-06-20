using System.Numerics;
using System.Windows;
using System.Windows.Forms;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Ab4d.SharpEngine.Samples.WinForms.UIProvider;
using MouseButtons = System.Windows.Forms.MouseButtons;

namespace Ab4d.SharpEngine.Samples.WinForms.HitTesting;

// This class processes WinForms mouse events and routes them to the methods in the common ManualInputEventsSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class WinFormsManualInputEventsSample : ManualInputEventsSample
{
    private Control? _subscribedControl;
    private float _dpiScale = 1;
    private ICommonSampleUIElement? _intoTextBox;

    public WinFormsManualInputEventsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not Control control)
            return;

        control.MouseDown += ControlOnMouseDown;
        control.MouseUp += ControlOnMouseUp;
        control.MouseMove += OnMouseMove;

        _subscribedControl = control;
    }

    private void UnSubscribeMouseEvents()
    {
        if (_subscribedControl == null)
            return;

        _subscribedControl.MouseDown -= ControlOnMouseDown;
        _subscribedControl.MouseUp -= ControlOnMouseUp;
        _subscribedControl.MouseMove -= OnMouseMove;

        _subscribedControl = null;
    }
    
    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_subscribedControl == null)
            return;

        var mousePosition = new Vector2((float)e.Location.X / _dpiScale, (float)e.Location.Y / _dpiScale);
        ProcessPointerMoved(mousePosition);
    }

    private void ControlOnMouseDown(object? sender, MouseEventArgs e)
    {
        if (isLeftPointerButtonPressed || _subscribedControl == null)
            return;

        isLeftPointerButtonPressed = e.Button.HasFlag(MouseButtons.Left);

        if (isLeftPointerButtonPressed)
        {
            var mousePosition = new Vector2((float)e.Location.X / _dpiScale, (float)e.Location.Y / _dpiScale);
            ProcessLeftPointerButtonPressed(mousePosition);
        }
    }
    
    private void ControlOnMouseUp(object? sender, MouseEventArgs e)
    {
        if (!isLeftPointerButtonPressed || _subscribedControl == null) // is already released
            return;

        var isLeftMouseButtonReleased = e.Button.HasFlag(MouseButtons.Left);

        if (isLeftMouseButtonReleased)
        {
            var mousePosition = new Vector2((float)e.Location.X / _dpiScale, (float)e.Location.Y / _dpiScale);
            ProcessLeftPointerButtonReleased(mousePosition);
            isLeftPointerButtonPressed = false;
        }
    }

    protected override void ShowMessage(string message)
    {
        if (_intoTextBox == null)
            return;

        var oldMessages = _intoTextBox.GetText();
        if (oldMessages != null && oldMessages.Length > 2000)
            oldMessages = oldMessages.Substring(0, 2000); // prevent showing very large text

        _intoTextBox.SetText(message + System.Environment.NewLine + oldMessages);
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateCustomUI(ICommonSampleUIProvider ui)
    {
        if (ui is not WinFormsUIProvider winFormsUIProvider)
            return;

        _dpiScale = winFormsUIProvider.DpiScale;


        ui.CreateStackPanel(PositionTypes.BottomRight);

        ui.CreateCheckBox("Mouse dragging", isInitiallyChecked: true, isChecked => isPointerDraggingEnabled = isChecked);
        ui.CreateCheckBox("Check object collision", isInitiallyChecked: true, isChecked => isCollisionDetectionEnabled = isChecked);

        ui.AddSeparator();

        float textBoxHeight;
        if (winFormsUIProvider.BaseWinFormsPanel.PreferredSize.Height > 500)
            textBoxHeight = winFormsUIProvider.BaseWinFormsPanel.PreferredSize.Height - 100;
        else
            textBoxHeight = 400;

        _intoTextBox = ui.CreateTextBox(width: 300, textBoxHeight);
    }

    protected override void OnDisposed()
    {
        UnSubscribeMouseEvents();

        base.OnDisposed();
    }
}