using System.Linq;
using System;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.HitTesting;

public class AvaloniaRectangularSelectionSample : RectangularSelectionSample
{
    private InputElement? _subscribedUIElement;

    private Canvas? _rootCanvas;
    private Rectangle? _selectionRectangle;
    private Panel? _baseAvaloniaPanel;

    public AvaloniaRectangularSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        // Because we render a gradient in background RootBorder and we have set MainSceneView.IsHitTestVisible to false
        // we need to subscribe to parent Border control instead of to sharpEngineSceneView.
        if (sharpEngineSceneView is not Control avaloniaControl)
            return;

        var parentBorder = avaloniaControl.Parent as Border;
        if (parentBorder == null)
            return;

        parentBorder.PointerPressed  += EventsSourceElementOnPointerPressed;
        parentBorder.PointerReleased += EventsSourceElementOnPointerReleased;
        parentBorder.PointerMoved    += EventsSourceElementOnPointerMoved;

        _subscribedUIElement = parentBorder;
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateOverlaySelectionRectangle(ICommonSampleUIProvider ui)
    {
        if (ui is not AvaloniaUIProvider avaloniaUIProvider)
            return;

        _baseAvaloniaPanel = avaloniaUIProvider.BaseAvaloniaPanel;

        if (_baseAvaloniaPanel == null)
            return;

        _rootCanvas = new Canvas();
        _baseAvaloniaPanel.Children.Add(_rootCanvas);

        _selectionRectangle = new Rectangle()
        {
            //Fill = new SolidColorBrush(Color.FromArgb(85, 95, 211, 255)),     // #555FD3FF
            //Stroke = new SolidColorBrush(Color.FromArgb(170, 95, 211, 255)),  // #AA5FD3FF,
            Fill = new SolidColorBrush(Color.FromArgb(120, 95, 211, 255)),   // #785FD3FF
            Stroke = new SolidColorBrush(Color.FromArgb(255, 95, 211, 255)), // #FF5FD3FF,
            StrokeThickness = 1,
            IsHitTestVisible = false
        };
    }

    protected override void ShowSelectionRectangle(Vector2 startPosition, Vector2 endPosition)
    {
        if (_selectionRectangle == null || _rootCanvas == null)
            return;

        Canvas.SetLeft(_selectionRectangle, MathF.Min(startPosition.X, endPosition.X));
        Canvas.SetTop(_selectionRectangle, MathF.Min(startPosition.Y, endPosition.Y));

        _selectionRectangle.Width  = MathF.Abs(endPosition.X - startPosition.X);
        _selectionRectangle.Height = MathF.Abs(endPosition.Y - startPosition.Y);

        if (!_rootCanvas.Children.Contains(_selectionRectangle))
            _rootCanvas.Children.Add(_selectionRectangle);

        if (_baseAvaloniaPanel != null)
            _baseAvaloniaPanel.Cursor = new Cursor(StandardCursorType.Cross);
    }

    protected override void HideSelectionRectangle()
    {
        if (_selectionRectangle == null || _rootCanvas == null)
            return;

        _rootCanvas.Children.Remove(_selectionRectangle);

        if (_baseAvaloniaPanel != null)
            _baseAvaloniaPanel.Cursor = null;
    }

    private void UnSubscribeMouseEvents()
    {
        if (_subscribedUIElement == null)
            return;

        _subscribedUIElement.PointerPressed  -= EventsSourceElementOnPointerPressed;
        _subscribedUIElement.PointerReleased -= EventsSourceElementOnPointerReleased;
        _subscribedUIElement.PointerMoved    -= EventsSourceElementOnPointerMoved;

        _subscribedUIElement = null;
    }

    protected override void OnDisposed()
    {
        UnSubscribeMouseEvents();

        HideSelectionRectangle();
        if (_baseAvaloniaPanel != null && _rootCanvas != null)
            _baseAvaloniaPanel.Children.Remove(_rootCanvas);

        base.OnDisposed();
    }

    private void EventsSourceElementOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // PointerPressed is called not only when the mouse button is pressed, but all the time until the button is pressed
        // But we would only like to know when the left mouse button is pressed
        if (isLeftPointerButtonPressed || _subscribedUIElement == null)
            return;

        var currentPoint = e.GetCurrentPoint(_subscribedUIElement);
        isLeftPointerButtonPressed = currentPoint.Properties.IsLeftButtonPressed;

        if (isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessLeftPointerButtonPressed(pointerPosition);
        }
    }

    private void EventsSourceElementOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isLeftPointerButtonPressed || _subscribedUIElement == null) // is already released
            return;

        var currentPoint = e.GetCurrentPoint(_subscribedUIElement);
        isLeftPointerButtonPressed = currentPoint.Properties.IsLeftButtonPressed;

        if (!isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessLeftPointerButtonReleased(pointerPosition);
        }
    }

    private void EventsSourceElementOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_subscribedUIElement == null)
            return;

        var currentPoint = e.GetPosition(_subscribedUIElement);

        if (isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
            ProcessPointerMoved(pointerPosition);
        }
    }    
}