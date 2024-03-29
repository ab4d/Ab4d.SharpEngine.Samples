﻿using System.Numerics;
using System.Windows;
using System.Windows.Input;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;

namespace Ab4d.SharpEngine.Samples.Wpf.HitTesting;

// This class processes WPF mouse events and routes them to the methods in the common RayPlaneHitTestingSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class WpfRayPlaneHitTestingSample : RayPlaneHitTestingSample
{
    private UIElement? _subscribedElement;

    public WpfRayPlaneHitTestingSample(ICommonSamplesContext context)
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