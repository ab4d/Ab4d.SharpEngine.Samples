using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Ab4d.SharpEngine.Samples.WinUI.HitTesting;

public class WinUILineSelectionSample : LineSelectionSample
{
    private UIElement? _subscribedElement;

    public WinUILineSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        _subscribedElement = eventsSourceElement;

        eventsSourceElement.PointerMoved += OnParentBorderOnPointerMoved;
    }

    private void OnParentBorderOnPointerMoved(object? sender, PointerRoutedEventArgs args)
    {
        var currentPoint = args.GetCurrentPoint(_subscribedElement);
        {
            var mousePosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessMouseMove(mousePosition);
        }
    }

    protected override void OnDisposed()
    {
        if (_subscribedElement != null)
            _subscribedElement.PointerMoved -= OnParentBorderOnPointerMoved;

        base.OnDisposed();
    }

}