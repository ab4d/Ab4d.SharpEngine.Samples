using Ab4d.SharpEngine.Samples.BlazorWebAssembly;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common;

public class BlazorSamplesContext : CommonSamplesContext
{
    public static readonly BlazorSamplesContext Current = new BlazorSamplesContext();
    
    private BitmapTextCreator? _bitmapTextCreator;
    private TextBlockFactory? _textBlockFactory;
    private Task<TextBlockFactory>? _textBlockFactoryLoadingTask;

    private BlazorSamplesContext()
        : base(applicationName: "SharpEngine Blazor Samples", bitmapIO: null)
    {
    }

    public void RegisterCurrentSharpEngineSceneView(SharpEngineSceneView? sharpEngineSceneView)
    {
        _textBlockFactory = null; // Reset current TextBlockFactory when the SharpEngineSceneView is changed
        SetCurrentSharpEngineSceneView(sharpEngineSceneView);
    }

    public override async Task<TextBlockFactory> GetTextBlockFactoryAsync()
    {
        // If already loaded, return synchronously
        if (_textBlockFactory != null)
            return _textBlockFactory;
        
        // If loading already started, return the same task
        if (_textBlockFactoryLoadingTask != null)
            return _textBlockFactoryLoadingTask.Result;

        // Start loading and store the task
        _textBlockFactoryLoadingTask = GetTextBlockFactoryIntAsync();

        var textBlockFactory = await _textBlockFactoryLoadingTask;

        _textBlockFactoryLoadingTask = null;
        return textBlockFactory;
    }
    
    private async Task<TextBlockFactory> GetTextBlockFactoryIntAsync()
    {
        if (CurrentSharpEngineSceneView == null)
            throw new InvalidOperationException("Cannot call GetTextBlockFactory when CurrentSharpEngineSceneView is not yet set.");

        if (_textBlockFactory != null && _textBlockFactory.Scene != CurrentSharpEngineSceneView.Scene)
        {
            _textBlockFactory.Dispose();
            _textBlockFactory = null;
        }

        if (_textBlockFactory == null)
        {
            if (GpuDevice == null)
                throw new Exception("Cannot create TextBlockFactory because GpuDevice is null");
            
            var bitmapFont = await BitmapFont.CreateAsync("fonts/roboto_64.fnt", GpuDevice);
            _bitmapTextCreator = await BitmapTextCreator.CreateAsync(CurrentSharpEngineSceneView.Scene, bitmapFont);

            // Create TextBlockFactory that will use the default BitmapTextCreator (get by BitmapTextCreator.GetDefaultBitmapTextCreator).
            _textBlockFactory = new TextBlockFactory(_bitmapTextCreator);
        }

        return _textBlockFactory;
    }
}
