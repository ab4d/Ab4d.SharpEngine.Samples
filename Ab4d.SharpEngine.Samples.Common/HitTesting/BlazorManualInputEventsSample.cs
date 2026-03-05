using System.Numerics;
using Ab4d.SharpEngine.Browser;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// This class processes Avalonia mouse events and routes them to the methods in the common ManualInputEventsSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class BlazorManualInputEventsSample : ManualInputEventsSample
{
    private ICanvasInterop? _subscribedCanvasInterop;
    private ICommonSampleUIElement? _infoTextBox;

    public BlazorManualInputEventsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        var canvasInterop = sharpEngineSceneView.GpuDevice?.CanvasInterop;

        if (canvasInterop == null)
            return;

        canvasInterop.PointerMoved += OnPointerMoved;
        canvasInterop.PointerDown += OnPointerDown;
        canvasInterop.PointerUp += OnPointerUp;

        _subscribedCanvasInterop = canvasInterop;
    }

    protected override void OnDisposed()
    {
        UnSubscribeMouseEvents();
        base.OnDisposed();
    }

    private void OnPointerMoved(object? sender, MouseMoveEventArgs e)
    {
        var pointerPosition = new Vector2((float)e.MouseX, (float)e.MouseY);
        ProcessPointerMoved(pointerPosition);
    }

    private void OnPointerDown(object? sender, MouseButtonEventArgs e)
    {
        // PointerPressed is called not only when the pointer button is pressed, but all the time until the button is pressed
        // But we would only like to know when the left pointer button is pressed
        if (isLeftPointerButtonPressed)
            return;

        isLeftPointerButtonPressed = e.PressedButtons.HasFlag(PointerButtons.Left);

        if (isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)e.MouseX, (float)e.MouseY);
            ProcessLeftPointerButtonPressed(pointerPosition);
        }
    }
    
    private void OnPointerUp(object? sender, MouseButtonEventArgs e)
    {
        if (!isLeftPointerButtonPressed) // is already released
            return;

        isLeftPointerButtonPressed = e.PressedButtons.HasFlag(PointerButtons.Left);

        if (!isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)e.MouseX, (float)e.MouseY);
            ProcessLeftPointerButtonReleased(pointerPosition);
        }
    }


    private void UnSubscribeMouseEvents()
    {
        if (_subscribedCanvasInterop == null)
            return;

        _subscribedCanvasInterop.PointerMoved -= OnPointerMoved;
        _subscribedCanvasInterop.PointerDown -= OnPointerDown;
        _subscribedCanvasInterop.PointerUp -= OnPointerUp;

        _subscribedCanvasInterop = null;
    }

    protected override void ShowMessage(string message)
    {
        if (_infoTextBox == null)
            return;

        var oldMessages = _infoTextBox.GetText();
        if (oldMessages != null && oldMessages.Length > 2000)
            oldMessages = oldMessages.Substring(0, 2000); // prevent showing very large text

        _infoTextBox.SetText(message + System.Environment.NewLine + oldMessages);
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateCustomUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Mouse dragging", true, isChecked => isPointerDraggingEnabled = isChecked);
        ui.CreateCheckBox("Check object collisions", true, isChecked => isCollisionDetectionEnabled = isChecked);

        ui.AddSeparator();

        _infoTextBox = ui.CreateTextBox(width: 300, height: 500);
    }
}