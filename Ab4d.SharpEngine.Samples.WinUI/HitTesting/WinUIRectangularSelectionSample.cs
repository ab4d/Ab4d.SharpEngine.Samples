using System;
using System.Numerics;
using Windows.UI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Ab4d.SharpEngine.Samples.WinUI.UIProvider;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Ab4d.SharpEngine.Samples.WinUI.HitTesting;

public class WinUIRectangularSelectionSample : RectangularSelectionSample
{
    private UIElement? _subscribedUIElement;

    private Canvas? _rootCanvas;
    private Rectangle? _selectionRectangle;
    private Panel? _baseWinUIPanel;

    public WinUIRectangularSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        eventsSourceElement.PointerPressed  += EventsSourceElementOnPointerPressed;
        eventsSourceElement.PointerReleased += EventsSourceElementOnPointerReleased;
        eventsSourceElement.PointerMoved    += EventsSourceElementOnPointerMoved;

        _subscribedUIElement = eventsSourceElement;
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateOverlaySelectionRectangle(ICommonSampleUIProvider ui)
    {
        if (ui is not WinUIProvider winUIUIProvider)
            return;

        _baseWinUIPanel = winUIUIProvider.BaseWinUIPanel;

        if (_baseWinUIPanel == null)
            return;

        _rootCanvas = new Canvas();
        _baseWinUIPanel.Children.Add(_rootCanvas);

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

        //if (_baseWinUIPanel != null)
        //    _baseWinUIPanel.Cursor = Cursors.Cross;
    }

    protected override void HideSelectionRectangle()
    {
        if (_selectionRectangle == null || _rootCanvas == null)
            return;

        _rootCanvas.Children.Remove(_selectionRectangle);

        //if (_baseWinUIPanel != null)
        //    _baseWinUIPanel.Cursor = null;
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
        if (_baseWinUIPanel != null && _rootCanvas != null)
            _baseWinUIPanel.Children.Remove(_rootCanvas);

        base.OnDisposed();
    }

    private void EventsSourceElementOnPointerPressed(object sender, PointerRoutedEventArgs e)
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

    private void EventsSourceElementOnPointerReleased(object sender, PointerRoutedEventArgs e)
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

    private void EventsSourceElementOnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_subscribedUIElement == null)
            return;

        var currentPoint = e.GetCurrentPoint(_subscribedUIElement);

        if (isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessPointerMoved(pointerPosition);
        }
    }
}