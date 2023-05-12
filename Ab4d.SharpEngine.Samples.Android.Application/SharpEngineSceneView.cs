using System;
using Android.Content;
using Android.Views;
using Java.Interop;
using System.Runtime.InteropServices;
using Android.Graphics;
using Android.OS;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Utilities;

namespace AndroidApp1
{
	public class SharpEngineSceneView : SurfaceView, ISurfaceHolderCallback, Choreographer.IFrameCallback
	{
        private static readonly string LogArea = typeof(SceneView).FullName!;

        protected IntPtr nativeAndroidWindow = IntPtr.Zero;

		public Action? RenderSceneAction;
		public Action<IntPtr>? SurfaceCreatedAction;
		public Action? SurfaceDestroyedAction;

		public SharpEngineSceneView (Context context) 
            : base (context)
		{
			Log.Info?.Write(LogArea, $"SharpEngineSceneView created on Android device {Build.Manufacturer} {Build.Model}, cpu: {Build.Hardware}");

			Holder?.AddCallback(this);
			SetWillNotDraw(false);
		}

        void AcquireNativeWindow (ISurfaceHolder holder)
		{
			if (nativeAndroidWindow != IntPtr.Zero)
				NativeMethods.ANativeWindow_release (nativeAndroidWindow);

			if (holder.Surface != null)
                nativeAndroidWindow = NativeMethods.ANativeWindow_fromSurface (JniEnvironment.EnvironmentPointer, holder.Surface.Handle);

			// Use Choreographer to get reliable rendering timer
			// See
			// https://developer.android.com/games/develop/gameloops
			// https://stackoverflow.com/questions/36802850/creating-a-lag-free-2d-game-loop-on-android
			if (Choreographer.Instance != null)
                Choreographer.Instance.PostFrameCallback(this);
		}

		public void SurfaceCreated (ISurfaceHolder holder)
		{
            Log.Info?.Write(LogArea, "SurfaceCreated");

            AcquireNativeWindow (holder);

            SurfaceCreatedAction?.Invoke(nativeAndroidWindow);
        }

		public void SurfaceDestroyed (ISurfaceHolder holder)
		{
            Log.Info?.Write(LogArea, "SurfaceDestroyed");

            if (nativeAndroidWindow != IntPtr.Zero)
				NativeMethods.ANativeWindow_release (nativeAndroidWindow);

			nativeAndroidWindow = IntPtr.Zero;

            SurfaceDestroyedAction?.Invoke();
        }

		public void SurfaceChanged (ISurfaceHolder holder, global::Android.Graphics.Format format, int w, int h)
		{
		}

        public void DoFrame(long frameTimeNanos)
        {
            // mFrameTime = frameTimeNanos;

            Log.Info?.Write(LogArea, "DoFrame");

            Invalidate(); // Call Draw method below

			if (Choreographer.Instance != null)
                Choreographer.Instance.PostFrameCallback(this); // Call DoFrame again on next VSYNC
		}

        public override void Draw(Canvas? canvas)
        {
			RenderSceneAction?.Invoke();
			base.Draw(canvas);
        }
    }

	internal static class NativeMethods
	{
		const string AndroidRuntimeLibrary = "android";

		[DllImport (AndroidRuntimeLibrary)]
		internal static extern IntPtr ANativeWindow_fromSurface (IntPtr jniEnv, IntPtr handle);

		[DllImport (AndroidRuntimeLibrary)]
		internal static extern void ANativeWindow_release (IntPtr window);
	}
}
