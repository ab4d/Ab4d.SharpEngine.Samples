using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

namespace Ab4d.SharpEngine.Samples.WinUI.HitTesting;

public class WinUIHitTestingSample : HitTestingSample
{
    public WinUIHitTestingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        eventsSourceElement.PointerMoved += delegate (object sender, PointerRoutedEventArgs args)
        {
            var currentPoint = args.GetCurrentPoint(eventsSourceElement);
            {
                var mousePosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
                ProcessMouseMove(mousePosition);
            }
        };
    }
}