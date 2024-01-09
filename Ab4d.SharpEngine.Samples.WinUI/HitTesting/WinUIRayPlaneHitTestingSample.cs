using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

namespace Ab4d.SharpEngine.Samples.WinUI.HitTesting;

// This class processes WinUI mouse events and routes them to the methods in the common RayPlaneHitTestingSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class WinUIRayPlaneHitTestingSample : RayPlaneHitTestingSample
{
    private UIElement? _subscribedElement;

    public WinUIRayPlaneHitTestingSample(ICommonSamplesContext context)
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