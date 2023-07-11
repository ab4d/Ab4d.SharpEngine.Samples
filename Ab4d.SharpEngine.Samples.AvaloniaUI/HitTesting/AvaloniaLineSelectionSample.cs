using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Avalonia.Controls;
using Avalonia.Input;
using System.Numerics;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.HitTesting;

public class AvaloniaLineSelectionSample : LineSelectionSample
{
    private InputElement? _subscribedElement;

    public AvaloniaLineSelectionSample(ICommonSamplesContext context)
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

        _subscribedElement = parentBorder;

        parentBorder.PointerMoved += OnParentBorderOnPointerMoved;
    }

    private void OnParentBorderOnPointerMoved(object? sender, PointerEventArgs args)
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
            _subscribedElement.PointerMoved -= OnParentBorderOnPointerMoved;

        base.OnDisposed();
    }
}