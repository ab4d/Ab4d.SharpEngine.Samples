using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

namespace Ab4d.SharpEngine.Samples.WinUI.HitTesting;

public class WinUIHitTestingWithIdBitmapSample : HitTestingWithIdBitmapSample
{
    private UIElement? _subscribedElement;

    public WinUIHitTestingWithIdBitmapSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        _subscribedElement = eventsSourceElement;

        eventsSourceElement.PointerMoved += OnEventsSourceElementOnPointerMoved;
    }

    private void OnEventsSourceElementOnPointerMoved(object sender, PointerRoutedEventArgs args)
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
            _subscribedElement.PointerMoved += OnEventsSourceElementOnPointerMoved;

        base.OnDisposed();
    }
}