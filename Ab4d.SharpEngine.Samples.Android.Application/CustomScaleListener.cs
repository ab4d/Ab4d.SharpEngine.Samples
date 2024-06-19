using Ab4d.SharpEngine.Utilities;
using Android.Views;

namespace AndroidApp1;

public class CustomScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
{
    private ManualPointerCameraController? _pointerCameraController;

    private float _startScaleFactor;
    private float _lastScaleFactor;

    public void SetPointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        _pointerCameraController = pointerCameraController;
    }

    public override bool OnScaleBegin(ScaleGestureDetector? detector)
    {
        if (detector == null)
            return false;

        _startScaleFactor = detector.ScaleFactor;
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