using Ab4d.SharpEngine.Utilities;
using Android.Views;

namespace AndroidDemo;

public class MyScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
{
    private ManualPointerCameraController? _pointerCameraController;

    private float _lastScaleFactor;

    public void SetMouseCameraController(ManualPointerCameraController pointerCameraController)
    {
        _pointerCameraController = pointerCameraController;
    }

    public override bool OnScaleBegin(ScaleGestureDetector? detector)
    {
        if (detector == null)
            return false;

        _lastScaleFactor = 1;

        return base.OnScaleBegin(detector);
    }

    public override bool OnScale(ScaleGestureDetector? detector)
    {
        if (detector == null)
            return false;

        //System.Diagnostics.Debug.WriteLine($"OnScale: CurrentSpanX: {detector.CurrentSpanX}; CurrentSpanY: {detector.CurrentSpanY};  ScaleFactor: {detector.ScaleFactor}");

        if (_pointerCameraController != null)
        {
            float oneStepScaleFactor = detector.ScaleFactor / _lastScaleFactor;
            _lastScaleFactor = detector.ScaleFactor;

            //_mouseCameraController.ProcessMouseWheel(new Vector2(detector.FocusX, detector.FocusY), detector.ScaleFactor);
            _pointerCameraController.ChangeCameraDistance(1f / oneStepScaleFactor);

            //System.Diagnostics.Debug.WriteLine($"ChangeCameraDistance({oneStepScaleFactor})");
        }

        return base.OnScale(detector);
    }
}