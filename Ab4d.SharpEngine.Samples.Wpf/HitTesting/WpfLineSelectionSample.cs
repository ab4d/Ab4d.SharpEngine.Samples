using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using System.Numerics;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Ab4d.SharpEngine.Samples.Common.HitTesting;

namespace Ab4d.SharpEngine.Samples.Wpf.HitTesting;

public class WpfLineSelectionSample : LineSelectionSample
{
    private UIElement? _subscribedElement;

    public WpfLineSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        _subscribedElement = eventsSourceElement;

        eventsSourceElement.MouseMove += OnParentBorderOnPointerMoved;
    }

    private void OnParentBorderOnPointerMoved(object? sender, MouseEventArgs args)
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
            _subscribedElement.MouseMove -= OnParentBorderOnPointerMoved;

        base.OnDisposed();
    }
}