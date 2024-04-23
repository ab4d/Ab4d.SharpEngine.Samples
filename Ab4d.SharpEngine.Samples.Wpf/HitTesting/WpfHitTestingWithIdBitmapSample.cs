using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using System.Numerics;
using System.Windows;
using System.Windows.Input;

namespace Ab4d.SharpEngine.Samples.Wpf.HitTesting;

public class WpfHitTestingWithIdBitmapSample : HitTestingWithIdBitmapSample
{
    private UIElement? _subscribedElement;

    public WpfHitTestingWithIdBitmapSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        _subscribedElement = eventsSourceElement;

        eventsSourceElement.MouseMove += OnEventsSourceElementOnMouseMove;
    }

    private void OnEventsSourceElementOnMouseMove(object sender, MouseEventArgs args)
    {
        var currentPoint = args.GetPosition(_subscribedElement);
        {
            var mousePosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
            ProcessMouseMove(mousePosition);
        }
    }

    protected override void OnDisposed()
    {
        if (_subscribedElement != null)
            _subscribedElement.MouseMove -= OnEventsSourceElementOnMouseMove;

        base.OnDisposed();
    }
}