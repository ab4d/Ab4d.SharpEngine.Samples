using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Wpf;
using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Vulkan;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Size = System.Windows.Size;

namespace Ab4d.SharpEngine.Samples.Wpf.Common;

public class WpfElementOverlay : IDisposable
{
    private readonly SharpEngineSceneView _sharpEngineSceneView;

    private readonly FrameworkElement _wpfElement;

    private SpriteBatch? _spriteBatch;
    private GpuImage? _renderedWpfElementGpuImage;

    private RenderTargetBitmap? _wpfElementBitmap;

    public FrameworkElement? ParentElement;
    private byte[]? _textureBytes;

    public WpfElementOverlay(FrameworkElement wpfElement, SharpEngineSceneView sharpEngineSceneView)
    {
        _wpfElement = wpfElement;
        _sharpEngineSceneView = sharpEngineSceneView;

        if (sharpEngineSceneView.SceneView.IsInitialized)
        {
            Update();
        }
        else
        {
            _sharpEngineSceneView.SceneViewInitialized += (sender, args) =>
            {
                Update();
            };
        }

        // When some overlay controls are right or bottom aligned, then we need to update their positions when the size of the view is changed
        _sharpEngineSceneView.SizeChanged += (sender, args) =>
        {
            UpdateSpriteBatch();
        };
    }

    public void Update()
    {
        var gpuDevice = _sharpEngineSceneView.Scene.GpuDevice;
        if (gpuDevice == null)
            return;
        

        // Render to a bigger bitmap when using DPI scale and Super-sampling
        int bitmapDpi = (int)(96 * _sharpEngineSceneView.SceneView.DpiScaleX * _sharpEngineSceneView.SceneView.SupersamplingFactor);
        
        // If we do not call UpdateLayout, then initially CameraNavigationCircles is not visible (only after changing the camera)
        _wpfElement.UpdateLayout();
        
        
        _wpfElementBitmap = RenderToBitmap(_wpfElement, null, bitmapDpi, _wpfElementBitmap);

        if (_wpfElementBitmap == null)
            return;

        
        var width = _wpfElementBitmap.PixelWidth;
        var height = _wpfElementBitmap.PixelHeight;
        
        if (width == 0 || height == 0)
            return;
        
        
        int bitmapStride = width * 4; // 4 bytes per pixel
        
        if (_textureBytes == null || _textureBytes.Length != height * bitmapStride)
            _textureBytes = new byte[height * bitmapStride];

        _wpfElementBitmap.CopyPixels(_textureBytes, bitmapStride, 0);
        

        if (_renderedWpfElementGpuImage != null)
            _renderedWpfElementGpuImage.Dispose();
        
        _renderedWpfElementGpuImage = CreateTexture(gpuDevice, _textureBytes, width, height, Ab4d.Vulkan.Format.B8G8R8A8Unorm, isPreMultipliedAlpha: true, hasTransparentPixels: true, name: "RenderedWpfElement");

        UpdateSpriteBatch();
        _sharpEngineSceneView.SceneView.NotifyChange(SceneViewDirtyFlags.SpritesChanged);
    }

    private void UpdateSpriteBatch()
    {
        if (_spriteBatch == null)
        {
            _spriteBatch = _sharpEngineSceneView.SceneView.CreateOverlaySpriteBatch(_wpfElement.GetType().Name + "OverlaySprite");
            _spriteBatch.IsUsingDpiScale = true;
        }

        var positionedElement = ParentElement ?? _wpfElement;

        var (destinationPosition, destinationSize) = GetWpfObjectPosition(positionedElement, parentWpfElement: _sharpEngineSceneView);
        UpdateSpriteBatch(_spriteBatch, destinationPosition, destinationSize);
    }

    private void UpdateSpriteBatch(SpriteBatch spriteBatch, Vector2 destinationPosition, Vector2 destinationSize)
    {
        if (_renderedWpfElementGpuImage == null)
            return;
        
        spriteBatch.Begin(useAbsoluteCoordinates: true);

        spriteBatch.SetSpriteTexture(_renderedWpfElementGpuImage);

        spriteBatch.DrawSprite(topLeftPosition: destinationPosition, spriteSize: destinationSize);
        
        spriteBatch.End();
    }

    private void DisposeTexturesAndShaderResourceViews()
    {
        if (_renderedWpfElementGpuImage != null)
        {
            _renderedWpfElementGpuImage.Dispose();
            _renderedWpfElementGpuImage = null;
        }
    }

    public void Dispose()
    {
        DisposeTexturesAndShaderResourceViews();

        if (_spriteBatch != null)
        {
            _sharpEngineSceneView.SceneView.RemoveSpriteBatch(_spriteBatch);
            _spriteBatch = null;
        }

        _textureBytes = null;
    }
    
    public static GpuImage CreateTexture(VulkanDevice gpuDevice, byte[] textureData, int width, int height, Ab4d.Vulkan.Format format = Ab4d.Vulkan.Format.B8G8R8A8Unorm, bool isPreMultipliedAlpha = true, bool hasTransparentPixels = true, string? name = null)
    {
        var rawImageData = new RawImageData(width, height, 4 * width, format, textureData, checkTransparency: false);
        var gpuImage = new GpuImage(gpuDevice, rawImageData, generateMipMaps: false, name)
        {
            IsPreMultipliedAlpha = true,
            HasTransparentPixels = true,
        };
            
        return gpuImage;
    }
    
    /// <summary>
    /// Renders FrameworkElement specified with objectToRender to bitmap with specified backgroundBrush and dpi.
    /// The size of the created bitmap is the same as the size of the objectToRender.
    /// </summary>
    /// <param name="objectToRender">FrameworkElement to render</param>
    /// <param name="backgroundBrush">brush used for background or null to have no background. Default value is null.</param>
    /// <param name="dpi">DPI setting for the rendered bitmap. Default value is 96</param>
    /// <param name="renderTargetBitmapToReuse">when not null and when its size is the same as the size of objectToRender, then the renderTargetBitmapToReuse is cleared and used again to improve memory usage.</param>
    /// <returns>RenderTargetBitmap</returns>
    public static RenderTargetBitmap? RenderToBitmap(FrameworkElement? objectToRender, Brush? backgroundBrush = null, int dpi = 96, RenderTargetBitmap? renderTargetBitmapToReuse = null)
    {
        if (objectToRender == null)
            return null;


        // When Left or Top margin is set on objectToRender, then the object is cropped -
        // the bitmap size is the same as without margin, but the object start at (Left, Top).
        // Therefore we clear the Margin in this case.
        var savedMargin = objectToRender.Margin;
        bool isMarginReset = (savedMargin.Left > 0 || savedMargin.Top > 0);

        if (isMarginReset)
            objectToRender.Margin = new Thickness();

        
        objectToRender.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        objectToRender.Arrange(new Rect(0, 0, objectToRender.DesiredSize.Width, objectToRender.DesiredSize.Height));

        double objectWidth  = objectToRender.ActualWidth;
        double objectHeight = objectToRender.ActualHeight;
        
        int objectDpiWidth  = Convert.ToInt32(objectWidth * dpi / 96);
        int objectDpiHeight = Convert.ToInt32(objectHeight * dpi / 96);


        RenderTargetBitmap renderTargetBitmap;

        // Try to reuse RenderTargetBitmap
        if (renderTargetBitmapToReuse == null ||
            renderTargetBitmapToReuse.PixelWidth != objectDpiWidth || 
            renderTargetBitmapToReuse.PixelHeight != objectDpiHeight)
        {
            renderTargetBitmap = new RenderTargetBitmap(objectDpiWidth, objectDpiHeight, dpi, dpi, PixelFormats.Pbgra32);
        }
        else
        {
            // We can reuse the RenderTargetBitmap
            renderTargetBitmapToReuse.Clear();
            renderTargetBitmap = renderTargetBitmapToReuse;
        }

        // Render background
        if (backgroundBrush != null && !ReferenceEquals(backgroundBrush, Brushes.Transparent))
        {
            var backgroundRect = new System.Windows.Shapes.Rectangle();
            backgroundRect.Width = objectWidth;
            backgroundRect.Height = objectHeight;
            backgroundRect.Fill = backgroundBrush;
            backgroundRect.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            backgroundRect.Arrange(new Rect(0, 0, objectWidth, objectHeight));

            renderTargetBitmap.Render(backgroundRect);
        }

        // Render object
        renderTargetBitmap.Render(objectToRender);


        if (isMarginReset)
        {
            // Reset marigin
            objectToRender.Margin = savedMargin;
        }
        
        // Update layout after resetting Margin and also because we called objectToRender.Arrange with position (0,0).
        objectToRender.UpdateLayout();

        return renderTargetBitmap;
    }        
    
    public static (Vector2, Vector2) GetWpfObjectPosition(FrameworkElement wpfElement, FrameworkElement parentWpfElement)
    {
        float parentWidth = (float)parentWpfElement.ActualWidth;
        float parentHeight = (float)parentWpfElement.ActualHeight;

        float elementWidth = (float)wpfElement.ActualWidth;
        float elementHeight = (float)wpfElement.ActualHeight;

        var wpfElementMargin = wpfElement.Margin;

        float left = (float)wpfElementMargin.Left;
        float right = (float)wpfElementMargin.Right;
        float top = (float)wpfElementMargin.Top;
        float bottom = (float)wpfElementMargin.Bottom;


        float elementX, elementY;

        switch (wpfElement.HorizontalAlignment)
        {
            case HorizontalAlignment.Center:
                elementX = parentWidth - (left - right - elementWidth) / 2 + left;
                break;

            case HorizontalAlignment.Right:
                elementX = parentWidth - elementWidth - right;
                break;

            case HorizontalAlignment.Left:
            case HorizontalAlignment.Stretch:
            default:
                elementX = left;
                break;
        }

        switch (wpfElement.VerticalAlignment)
        {
            case VerticalAlignment.Center:
                elementY = ((float)parentHeight - top - bottom - elementHeight) / 2 + top;
                break;

            case VerticalAlignment.Bottom:
                elementY = (float)parentHeight - (float)elementHeight - bottom;
                break;

            case VerticalAlignment.Top:
            case VerticalAlignment.Stretch:
            default:
                elementY = top;
                break;
        }

        return new (new Vector2(elementX, elementY), new Vector2(elementWidth, elementHeight));
    }
}
