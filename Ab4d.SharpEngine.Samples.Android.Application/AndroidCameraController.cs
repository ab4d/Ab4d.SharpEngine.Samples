﻿using System.Numerics;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Android.Content;
using Android.Views;
using AndroidApp1;

namespace AndroidApp1;

public class AndroidCameraController : ManualMouseCameraController
{
    private Activity _activity;
    private CustomScaleListener _myScaleListener;
    private ScaleGestureDetector _scaleGestureDetector;

    private System.Threading.Thread _uiThread;

    public AndroidCameraController(Context androidContext, Activity activity, SceneView sceneView)
        : base(sceneView)
    {
        _activity = activity;
        _uiThread = System.Threading.Thread.CurrentThread;
        
        // Set default rotate (single touch) and move (two finger touch) events:
        RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed;
        MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed;

        // See: https://developer.android.com/training/gestures/scale#java
        _myScaleListener = new CustomScaleListener();
        _scaleGestureDetector = new ScaleGestureDetector(androidContext, _myScaleListener);

        _myScaleListener.SetMouseCameraController(this);
    }

    public bool ProcessTouchEvent(MotionEvent? e)
    {
        if (e == null)
            return false;


        bool isHandled;

        if (ReferenceEquals(System.Threading.Thread.CurrentThread, _uiThread))
        {
            isHandled = ProcessTouchEventInt(e);
        }
        else
        {
            _activity.RunOnUiThread(() =>
            {
                ProcessTouchEventInt(e);
            });

            isHandled = false;
        }

        bool isScaleEvent = _scaleGestureDetector.OnTouchEvent(e);

        return isHandled || isScaleEvent;
    }

    private bool ProcessTouchEventInt(MotionEvent e) 
    {
        bool isHandled;

        //Log.Trace?.Write($"TouchEvent: Action: {e.Action}; pos: {xPos} {yPos}; PointerCount: {e.PointerCount}");

        float xPos = e.GetX();
        float yPos = e.GetY();

        MouseButtons simulatedMouseButtons;

        if (e.PointerCount == 1)
            simulatedMouseButtons = MouseButtons.Left;
        else if (e.PointerCount == 2)
            simulatedMouseButtons = MouseButtons.Left | MouseButtons.Right;
        else
            simulatedMouseButtons = MouseButtons.None;

        if (e.Action == MotionEventActions.Down ||
            e.Action == MotionEventActions.Pointer1Down ||
            e.Action == MotionEventActions.Pointer2Down)
        {
            // Start rotate
            isHandled = base.ProcessMouseDown(new Vector2(xPos, yPos), simulatedMouseButtons, KeyboardModifiers.None);
        }
        else if (e.Action == MotionEventActions.Up ||
                 e.Action == MotionEventActions.Pointer1Up ||
                 e.Action == MotionEventActions.Pointer2Up)
        {
            // End rotate
            // e.PointerCount value shows the value before the finger was lifted so we need to decrease the value by one
            // This means we have only 2 cases:
            if (e.PointerCount == 2)
            {
                if (e.Action == MotionEventActions.Pointer1Up)
                    simulatedMouseButtons = MouseButtons.Right; // 1st is up so only right remains
                else if (e.Action == MotionEventActions.Pointer1Up)
                    simulatedMouseButtons = MouseButtons.Left;  // 2nd is up so only left remains
                else
                    simulatedMouseButtons = MouseButtons.None;
            }
            else
            {
                simulatedMouseButtons = MouseButtons.None;
            }

            isHandled = base.ProcessMouseUp(simulatedMouseButtons, KeyboardModifiers.None);
        }
        else if (e.Action == MotionEventActions.Move)
        {
            // Rotate / move

            isHandled = base.ProcessMouseMove(new Vector2(xPos, yPos), simulatedMouseButtons, KeyboardModifiers.None);
        }
        else
        {
            isHandled = false;
        }

        return isHandled;
    }
}