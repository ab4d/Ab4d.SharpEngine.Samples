using System;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ab4d.SharpEngine.Samples.Wpf.UIProvider;

namespace Ab4d.SharpEngine.Samples.Wpf.HitTesting;

// This class processes WPF mouse events and routes them to the methods in the common RectangularSelectionSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class WpfRectangularSelectionSample : RectangularSelectionSample
{
    private UIElement? _subscribedUIElement;

    private Canvas? _rootCanvas;
    private Rectangle? _selectionRectangle;
    private Panel? _baseWpfPanel;

    public WpfRectangularSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        eventsSourceElement.MouseDown += EventsSourceElementOnMouseDown;
        eventsSourceElement.MouseUp   += EventsSourceElementOnMouseUp;
        eventsSourceElement.MouseMove += EventsSourceElementOnMouseMove;

        _subscribedUIElement = eventsSourceElement;
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateOverlaySelectionRectangle(ICommonSampleUIProvider ui)
    {
        if (ui is not WpfUIProvider wpfUIProvider)
            return;

        _baseWpfPanel = wpfUIProvider.BaseWpfPanel;

        if (_baseWpfPanel == null)
            return;

        _rootCanvas = new Canvas();
        _baseWpfPanel.Children.Add(_rootCanvas);

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

        if (_baseWpfPanel != null)
            _baseWpfPanel.Cursor = Cursors.Cross;
    }

    protected override void HideSelectionRectangle()
    {
        if (_selectionRectangle == null || _rootCanvas == null)
            return;

        _rootCanvas.Children.Remove(_selectionRectangle);

        if (_baseWpfPanel != null)
            _baseWpfPanel.Cursor = null;
    }

    private void UnSubscribeMouseEvents()
    {
        if (_subscribedUIElement == null)
            return;

        _subscribedUIElement.MouseDown -= EventsSourceElementOnMouseDown;
        _subscribedUIElement.MouseUp   -= EventsSourceElementOnMouseUp;
        _subscribedUIElement.MouseMove -= EventsSourceElementOnMouseMove;

        _subscribedUIElement = null;
    }

    protected override void OnDisposed()
    {
        UnSubscribeMouseEvents();

        HideSelectionRectangle();
        if (_baseWpfPanel != null && _rootCanvas != null)
            _baseWpfPanel.Children.Remove(_rootCanvas);

        base.OnDisposed();
    }

    private void EventsSourceElementOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // PointerPressed is called not only when the mouse button is pressed, but all the time until the button is pressed
        // But we would only like to know when the left mouse button is pressed
        if (isLeftPointerButtonPressed || _subscribedUIElement == null)
            return;

        var currentPoint = e.GetPosition(_subscribedUIElement);
        isLeftPointerButtonPressed = e.LeftButton == MouseButtonState.Pressed;

        if (isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
            ProcessLeftPointerButtonPressed(pointerPosition);
        }
    }

    private void EventsSourceElementOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!isLeftPointerButtonPressed || _subscribedUIElement == null) // is already released
            return;

        var currentPoint = e.GetPosition(_subscribedUIElement);
        isLeftPointerButtonPressed = e.LeftButton == MouseButtonState.Pressed;

        if (!isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
            ProcessLeftPointerButtonReleased(pointerPosition);
        }
    }

    private void EventsSourceElementOnMouseMove(object sender, MouseEventArgs e)
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