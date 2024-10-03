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
		public Action? SurfaceSizeChangedAction;

		public SceneView? SceneView { get; set; }

		public SharpEngineSceneView (Context context) 
            : base (context)
		{
			Log.Info?.Write(LogArea, $"SharpEngineSceneView created on Android device {Build.Manufacturer} {Build.Model}, cpu: {Build.Hardware}");

            // ReSharper disable VirtualMemberCallInConstructor
            Holder?.AddCallback(this);
            SetWillNotDraw(false);
            // ReSharper restore VirtualMemberCallInConstructor
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
            Log.Info?.Write(LogArea, $"SurfaceChanged {w} x {h} {format}");
        }

        public void DoFrame(long frameTimeNanos)
        {
            Invalidate(); // Call Draw method below

			if (Choreographer.Instance != null)
                Choreographer.Instance.PostFrameCallback(this); // Call DoFrame again on next VSYNC
		}

        public override void Draw(Canvas? canvas)
        {
			if (canvas == null)
				return;

			if (SceneView != null && (SceneView.Width != canvas.Width || SceneView.Height != canvas.Height))
            {
                Log.Info?.Write(LogArea, $"SurfaceSizeChanged: SceneView: {SceneView.Width} x {SceneView.Height}; Surface: {canvas.Width} x {canvas.Height}");
                SurfaceSizeChangedAction?.Invoke();
            }

            RenderSceneAction?.Invoke();
			base.Draw(canvas);
        }
    }

	internal static partial class NativeMethods
	{
		const string AndroidRuntimeLibrary = "android";

#if NET7_0_OR_GREATER
		// Using LibraryImport instead of DllImport. This uses compiler time source code generator and allows AOT.
		// See https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation

		[LibraryImport(AndroidRuntimeLibrary)]
		internal static partial IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr handle);

		[LibraryImport(AndroidRuntimeLibrary)]
		internal static partial void ANativeWindow_release(IntPtr window);
#else
		[DllImport (AndroidRuntimeLibrary)]
		internal static extern IntPtr ANativeWindow_fromSurface (IntPtr jniEnv, IntPtr handle);

		[DllImport (AndroidRuntimeLibrary)]
        internal static extern void ANativeWindow_release (IntPtr window);
#endif
	}
}
