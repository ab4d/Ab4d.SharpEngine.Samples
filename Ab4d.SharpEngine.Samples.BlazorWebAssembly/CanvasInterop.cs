using Ab4d.SharpEngine.Browser;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

// IMPORTANT:
// If you change this namespace, then you also need to change the code in sharp-engine.js: interop = exports.Ab4d.SharpEngine.WebGL.CanvasInterop;
// For example, if you change the namespace to MyBlazor, then you need to change the line in sharp-engine.js to interop = exports.MyBlazor.CanvasInterop;
// ReSharper disable once CheckNamespace
namespace Ab4d.SharpEngine.WebGL;

//#pragma warning disable CA1416 // prevented: This call site is reachable on all platforms. 'JSHost.ImportAsync(string, string, CancellationToken)' is only supported on: 'browser'.

[SupportedOSPlatform("browser")]
public partial class CanvasInterop : ICanvasInterop
{
    private static readonly bool IsLoggingInteropEvents = false; // When set to true, then all interop events are logged to the console
    private static readonly bool IsLoggingJavaScript = false; // When set to true, then javascript functions will log to console

    // Update the version in the url to the latest version
    private const string SpectorScriptUrl = "https://cdn.jsdelivr.net/npm/spectorjs@0.9.30/dist/spector.bundle.js";
    
    private static bool _isInitializeCalled;
    
    private static CanvasInterop? _initialInterop;
    private static List<CanvasInterop>? _additionalInteropObjects;
    private static List<string>? _urlRequestsOnDisposedInterop;
    private static int _canvasIndex;
    
    /// <summary>
    /// Returns true when the <see cref="InitializeInterop"/> method was called and successfully initialized the browser interop.
    /// </summary>
    public static bool IsInteropInitialized { get; private set; }

    /// <summary>
    /// Gets the IJSRuntime that is used for javascript interop. This is set when the <see cref="InitializeInterop"/> method is called.
    /// </summary>
    public static Microsoft.JSInterop.IJSRuntime? JS { get; private set; }

    /// <summary>
    /// True when <see cref="InvokeAsync{TValue}(string,object?[])"/> and <see cref="InvokeVoidAsync"/> are supported.
    /// </summary>
    public bool IsInvokeSupported => JS != null;
    
    /// <summary>
    /// Gets the id of the canvas element that is defined in the browser DOM (html or razor file).
    /// </summary>
    public string CanvasId { get; }
    
    /// <summary>
    /// Returns true after the <see cref="InitWebGL"/> method was called, the WebGL context was created and connection with the canvas elements was successfully established.
    /// </summary>
    public bool IsWebGLInitialized { get; private set; }

    /// <summary>
    /// Returns true when WebGL 2.0 is used. When false, then WebGL 1.0 is used. In this case some features may not be available.
    /// </summary>
    public bool IsWebGL2 { get; private set; }

    /// <summary>
    /// True when this CanvasInterop was disposed
    /// </summary>
    public bool IsDisposed { get; private set; }
    
    /// <summary>
    /// Gets the width of the canvas in pixels (defines width of the back buffer that is used for rendering).
    /// </summary>
    public int Width { get; private set; }
    
    /// <summary>
    /// Gets the height of the canvas in pixels (defines width of the back buffer that is used for rendering).
    /// </summary>    
    public int Height { get; private set; }

    /// <summary>
    /// Gets the dpi scale of the canvas.
    /// </summary>    
    public float DpiScale { get; private set; } = 1;

    /// <summary>
    /// Returns true when the canvas element is using MSAA (Multisample Anti-Aliasing).
    /// MSAA is can be disabled when the <see cref="InitWebGL"/> is called and the useMultisampleAntiAliasing parameters is set to false.
    /// </summary>
    public bool IsUsingMultisampleAntiAliasing { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the drawing buffer is preserved after rendering operations. This is set by setting the <see cref="EngineCreateOptions.PreserveDrawingBuffer"/> to true before the engine is initialized.
    /// </summary>
    public bool IsPreservingDrawingBuffer { get; private set; }
    
    /// <summary>
    /// Returns true when pointer (mouse, pointer and touch) events are subscribed in javascript.
    /// </summary>
    public bool ArePointerEventsSubscribed { get; private set; }

    /// <summary>
    /// Event that is triggered when this CanvasInterop initializes the WebGL and communication with the canvas.
    /// </summary>
    public event EventHandler? WebGLInitialized;

    public event MouseButtonEventHandler? PointerDown;
    public event MouseButtonEventHandler? PointerUp;
    public event MouseMoveEventHandler? PointerMoved;
    public event MouseMoveEventHandler? PointerEntered;
    public event MouseMoveEventHandler? PointerExited;
    public event MouseWheelEventHandler? MouseWheelChanged;
    
    public event PinchZoomEventHandler? PinchZoomStarted;
    public event EventHandler? PinchZoomEnded;
    public event PinchZoomEventHandler? PinchZoomed;
    
    public event EventHandler? BrowserAnimationFrameUpdated;
    public event CanvasResizedEventHandler? CanvasResized;
    public event EventHandler? ContextLost;

    public event EventHandler? Disposing;

    private bool _isSpectorScriptLoaded;
    private bool _isSpectorCaptureStarted;
    private bool _subscribePointerEventsOnInitialize;
    
    private Dictionary<string, List<(Action<string> onLoaded, Action<string>? onError)>>? _textFileLoadedCallbacks;
    private Dictionary<string, List<(Action<byte[]> onLoaded, Action<string>? onError)>>? _binaryFileLoadedCallbacks;
    private Dictionary<string, List<(Action<RawImageData> onLoaded, Action<string>? onError)>>? _imageBytesLoadedCallbacks;

    public CanvasInterop(bool subscribePointerEvents = true)
    {
        CanvasId = GetNextCanvasId();
        _subscribePointerEventsOnInitialize = subscribePointerEvents;
    }
    
    public CanvasInterop(string canvasId, bool subscribePointerEvents = true)
    {
        CanvasId = canvasId;
        _subscribePointerEventsOnInitialize = subscribePointerEvents;
    }
    
    #region static Initialize method and GetCanvasInterop
    public static async Task InitializeInterop(Microsoft.JSInterop.IJSRuntime jsRuntime, string? sharpEngineJsFileUrl = null)
    {
        if (_isInitializeCalled)
            return;

        _isInitializeCalled = true;

        JS = jsRuntime;

        if (!OperatingSystem.IsBrowser())
            throw new SharpEngineException("Ab4d.SharpEngine.Web can be used only in Blazor WebAssembly.");

        if (IsLoggingInteropEvents)
            Console.WriteLine("Initializing CanvasInterop");

        try
        {
            sharpEngineJsFileUrl ??= "../sharp-engine.js"; // by default the sharp-engine.js is in the wwwroot folder
            await JSHost.ImportAsync(moduleName: "sharp-engine.js", moduleUrl: sharpEngineJsFileUrl);
        }
        catch (Exception ex)
        {
            if (ex is JSException && ex.Message.Contains("Failed to fetch dynamically imported module"))
                throw new SharpEngineException("Cannot load sharp-engine.js. This file is required to setup the SharpEngine for Blazor WebAssembly. Make sure that the file is added to the wwwroot folder and that in csproj the following line exist: <WasmExtraFilesToDeploy Include=\"sharp-engine.js\" />.", ex);
            
            if (ex is JSException && ex.Message.StartsWith("SyntaxError:"))
                throw new SharpEngineException("Error parsing sharp-engine.js file. Please revert to the original file content or check your changes. Error message: " + ex.Message, ex);
            
            throw new SharpEngineException("Error loading sharp-engine.js file: " + ex.Message, ex);
        }
        
        try
        {
            await InitInteropAsync(); // Set interop field so javascript can call functions that are exported from CanvasInterop class (using JSExport attribute)
            
            IsInteropInitialized = true;
        }
        catch (Exception ex)
        {
            throw new SharpEngineException("Error initializing javascript interop: " + ex.Message, ex);
        }
    }

    public static string GetNextCanvasId()
    {
        _canvasIndex++;
        return $"sharpEngineCanvas_{_canvasIndex}";
    }
    
    private static CanvasInterop? GetCanvasInterop(string? canvasId, string requestUrl)
    {
        var canvasInterop = GetCanvasInterop(canvasId, writeWarningIfNotFound: false);

        if (canvasInterop == null)
        {
            bool isRequestedOnDisposedInterop;
            if (_urlRequestsOnDisposedInterop != null)
                isRequestedOnDisposedInterop = _urlRequestsOnDisposedInterop.Remove(requestUrl); // if removed, then this was issued from CanvasInterop that was already disposed
            else
                isRequestedOnDisposedInterop = false;

            string message = $"'{requestUrl}' content received but that CanvasInterop object ('{canvasId}') that started the request";

            if (isRequestedOnDisposedInterop)
                message += " was already disposed.";
            else
                message = "WARNING: " + message + " is not found.";

            Console.WriteLine(message);
        }

        return canvasInterop;
    }

    private static CanvasInterop? GetCanvasInterop(string? canvasId, bool writeWarningIfNotFound = true)
    {
        if (canvasId == null)
            throw new Exception("Cannot find CanvasInterop because canvasId that was provided by the javascript is null");
        
        // Optimize for the most common case: we use only one WebGL canvas 
        // But we support also more than one cases (by using _additionalInteropObjects)
        
        if (_initialInterop != null && _initialInterop.CanvasId.Equals(canvasId, StringComparison.Ordinal))
            return _initialInterop;
        
        
        CanvasInterop? foundInterop = null;
        
        if (_additionalInteropObjects != null)
        {
            foreach (var sharpEngineBrowserInterop in _additionalInteropObjects)
            {
                if (sharpEngineBrowserInterop.CanvasId.Equals(canvasId, StringComparison.Ordinal))
                {
                    foundInterop = sharpEngineBrowserInterop;
                    break;
                }
            }
        }

        if (foundInterop == null && writeWarningIfNotFound)
            Console.WriteLine($"WARNING: CanvasInterop object with canvasId '{canvasId}' not found. Probably it was disposed when a javascript request was not yet finished.");

        return foundInterop;
    }
    #endregion

    public void InitWebGL(bool useMultisampleAntiAliasing = true, bool preserveDrawingBuffer = false, bool preventShowingContextMenu = true)
    {
        CheckIsInitialized(checkIfConnectedToCanvas: false);

        var result = InitWebGLCanvasJs(this.CanvasId, useMultisampleAntiAliasing, preserveDrawingBuffer, _subscribePointerEventsOnInitialize, subscribeRequestAnimationFrame: true, preventShowingContextMenu: preventShowingContextMenu, IsLoggingJavaScript);

        if (string.IsNullOrEmpty(result) || !result.StartsWith("OK:"))
        {
            // canvasId not found or WebGL not available
            if (!string.IsNullOrEmpty(result))
                Console.WriteLine("Error initializing WebGL Canvas: " + result);

            return;
        }

        var dataParts = result.Substring(3).Split(';'); // Skip "OK:" and then split
        this.IsWebGL2 = dataParts[0] == "v2";
        this.Width    = int.Parse(dataParts[1]);
        this.Height   = int.Parse(dataParts[2]);
        this.DpiScale = float.Parse(dataParts[3], CultureInfo.InvariantCulture);
        this.IsUsingMultisampleAntiAliasing = useMultisampleAntiAliasing;
        this.IsPreservingDrawingBuffer = preserveDrawingBuffer;
        
        // Set static instances of CanvasInterop so that the static callback functions from javascript can find the target CanvasInterop
        if (_initialInterop == null)
        {
            _initialInterop = this;
        }
        else
        {
            _additionalInteropObjects ??= new List<CanvasInterop>();
            _additionalInteropObjects.Add(this);
        }
        
        this.ArePointerEventsSubscribed = _subscribePointerEventsOnInitialize;
        this.IsWebGLInitialized = true;

        if (IsLoggingInteropEvents)
            Console.WriteLine($"Initialized WebGL for '{this.CanvasId}': {this.Width} x {this.Height}; dpiScale: {this.DpiScale}");

        if (WebGLInitialized != null)
        {
            WebGLInitialized.Invoke(this, EventArgs.Empty);
            WebGLInitialized = null; // Remove all subscribers to prevent GC links
        }
    }

    private void CheckIsInitialized(bool checkIfConnectedToCanvas = true, [CallerMemberName] string? methodName = null)
    {
        if (!_isInitializeCalled)
            throw new SharpEngineException($"Cannot call {methodName} because InitializeInterop method was not called yet.");

        if (!IsInteropInitialized)
            throw new SharpEngineException($"Cannot call {methodName} because the WebGL canvas was not correctly initialized when calling InitializeInterop method.");
        
        if (checkIfConnectedToCanvas && !IsWebGLInitialized)
            throw new SharpEngineException($"Cannot call {methodName} because the Connect method was not called or it failed to connect to the canvas element.");
    }
    
    public void LoadTextFile(string fileName, Action<string> onLoadedCallback, Action<string>? onLoadErrorCallback)
    {
        if (!IsInteropInitialized)
        {
            WebGLInitialized += (sender, args) => LoadTextFile(fileName, onLoadedCallback, onLoadErrorCallback);
            return;
        }

        // We need to handle multiple requests for the same image file
        // Therefore we store a list of callbacks for each file name
        _textFileLoadedCallbacks ??= new Dictionary<string, List<(Action<string> onLoaded, Action<string>? onError)>>();

        if (_textFileLoadedCallbacks.TryGetValue(fileName, out var callbacks))
        {
            // Request to load this file was already issued - just add another callback action
            callbacks.Add((onLoadedCallback, onLoadErrorCallback));
            return;
        }
        
        // This is the first request to load this file
        callbacks = new List<(Action<string>, Action<string>?)>();
        callbacks.Add((onLoadedCallback, onLoadErrorCallback));

        _textFileLoadedCallbacks.Add(fileName, callbacks);

        LoadTextFileJs(this.CanvasId, fileName);
    }

    public async Task<string> LoadTextFileAsync(string fileName)
    {
        var tcs = new TaskCompletionSource<string>();

        LoadTextFile(fileName, 
            onLoadedCallback: fileContent => tcs.SetResult(fileContent),
            onLoadErrorCallback: errorText => tcs.SetException(new Exception(errorText)));

        return await tcs.Task;
    }
    
    public void LoadBinaryFile(string fileName, Action<byte[]> onLoadedCallback, Action<string>? onLoadErrorCallback)
    {
        if (!IsInteropInitialized)
        {
            WebGLInitialized += (sender, args) => LoadBinaryFile(fileName, onLoadedCallback, onLoadErrorCallback);
            return;
        }

        // We need to handle multiple requests for the same image file
        // Therefore we store a list of callbacks for each file name
        _binaryFileLoadedCallbacks ??= new Dictionary<string, List<(Action<byte[]> onLoaded, Action<string>? onError)>>();

        if (_binaryFileLoadedCallbacks.TryGetValue(fileName, out var callbacks))
        {
            // Request to load this file was already issued - just add another callback action
            callbacks.Add((onLoadedCallback, onLoadErrorCallback));
            return;
        }
         
        // This is the first request to load this file
        callbacks = new List<(Action<byte[]>, Action<string>?)>();
        callbacks.Add((onLoadedCallback, onLoadErrorCallback));

        _binaryFileLoadedCallbacks.Add(fileName, callbacks);

        LoadBinaryFileJs(this.CanvasId, fileName);
    }

    public async Task<byte[]> LoadBinaryFileAsync(string fileName)
    {
        var tcs = new TaskCompletionSource<byte[]>();

        LoadBinaryFile(fileName, 
            onLoadedCallback: fileBytes => tcs.SetResult(fileBytes),
            onLoadErrorCallback: errorText => tcs.SetException(new Exception(errorText)));

        return await tcs.Task;
    }
    
    public void LoadImageBytes(string fileName, Action<RawImageData> onTextureLoadedCallback, Action<string>? onLoadErrorCallback)
    {
        if (!IsInteropInitialized)
        {
            WebGLInitialized += (sender, args) => LoadImageBytes(fileName, onTextureLoadedCallback, onLoadErrorCallback);
            return;
        }

        // We need to handle multiple requests for the same image file
        // Therefore we store a list of callbacks for each file name
        _imageBytesLoadedCallbacks ??= new Dictionary<string, List<(Action<RawImageData> onLoaded, Action<string>? onError)>>();

        if (_imageBytesLoadedCallbacks.TryGetValue(fileName, out var callbacks))
        {
            // Request to load this texture was already issued - just add another callback action
            callbacks.Add((onTextureLoadedCallback, onLoadErrorCallback));
            return;
        }

        // This is the first request to load this file
        callbacks = new List<(Action<RawImageData>, Action<string>?)>();
        callbacks.Add((onTextureLoadedCallback, onLoadErrorCallback));

        _imageBytesLoadedCallbacks.Add(fileName, callbacks);

        LoadImageBytesJs(this.CanvasId, fileName);
    }

    public async Task<RawImageData> LoadImageBytesAsync(string fileName)
    {
        var tcs = new TaskCompletionSource<RawImageData>();

        LoadImageBytes(fileName, 
            onTextureLoadedCallback: rawImageData => tcs.SetResult(rawImageData),
            onLoadErrorCallback: errorText => tcs.SetException(new Exception(errorText)));

        return await tcs.Task;
    }
    
    public void CreateImageFromBytes(byte[] imageBytes, string mimeType, string imageName, Action<RawImageData> onTextureLoadedCallback, Action<string>? onLoadErrorCallback)
    {
        ArgumentNullException.ThrowIfNull(imageName);


        if (!IsInteropInitialized)
        {
            WebGLInitialized += (sender, args) => CreateImageFromBytes(imageBytes, mimeType, imageName, onTextureLoadedCallback, onLoadErrorCallback);
            return;
        }

        // We need to handle multiple requests for the same image file
        // Therefore we store a list of callbacks for each file name
        _imageBytesLoadedCallbacks ??= new Dictionary<string, List<(Action<RawImageData> onLoaded, Action<string>? onError)>>();

        if (_imageBytesLoadedCallbacks.TryGetValue(imageName, out var callbacks))
        {
            // Request to load this texture was already issued - just add another callback action
            callbacks.Add((onTextureLoadedCallback, onLoadErrorCallback));
            return;
        }

        // This is the first request to load this file
        callbacks = new List<(Action<RawImageData>, Action<string>?)>();
        callbacks.Add((onTextureLoadedCallback, onLoadErrorCallback));

        _imageBytesLoadedCallbacks.Add(imageName, callbacks);

        CreateImageFromBytesJS(this.CanvasId, imageBytes, mimeType, imageName);
    }

    public async Task<RawImageData> CreateImageFromBytesAsync(byte[] imageBytes, string mimeType, string imageName)
    {
        var tcs = new TaskCompletionSource<RawImageData>();

        CreateImageFromBytes(imageBytes, mimeType, imageName, 
            onTextureLoadedCallback: rawImageData => tcs.SetResult(rawImageData),
            onLoadErrorCallback: errorText => tcs.SetException(new Exception(errorText)));

        return await tcs.Task;
    }
    
    public void SetCursorStyle(string cursorStyle)
    {
        CheckIsInitialized();
        
        SetCursorStyleJs(this.CanvasId, cursorStyle);
    }

    public void ShowRawBitmap(string canvasId, int width, int height, byte[] pixelData, string? displayStyle)
    {
        ShowRawBitmapJs(canvasId, width, height, pixelData, displayStyle);
    }

    public void SubscribePointerEvents()
    {
        CheckIsInitialized();
        
        SubscribeBrowserEventsJs(this.CanvasId, subscribePointerEvents: true, subscribeRequestAnimationFrame: true);
        ArePointerEventsSubscribed = true;
    }
    
    public void UnsubscribePointerEvents()
    {
        CheckIsInitialized();
        
        UnsubscribeBrowserEventsJs(this.CanvasId, unsubscribePointerEvents: true, unsubscribeRequestAnimationFrame: false);
        ArePointerEventsSubscribed = false;
    }

    public void SetPointerCapture(int pointerId)
    {
        CheckIsInitialized();
        SetPointerCaptureJs(this.CanvasId, pointerId);
    }

    public void ReleasePointerCapture(int pointerId)
    {
        CheckIsInitialized();
        ReleasePointerCaptureJs(this.CanvasId, pointerId);
    }
    
    public async Task<bool> StartSpectorCapture()
    {
        CheckIsInitialized();
        
        if (_isSpectorCaptureStarted)
            return true;
        
        if (!_isSpectorScriptLoaded)
        {
            await JSHost.ImportAsync(moduleName: "spector", moduleUrl: SpectorScriptUrl);
            _isSpectorScriptLoaded = true;
        }
        
        bool success = StartSpectorCaptureJs(this.CanvasId);

        if (success)
            _isSpectorCaptureStarted = true;
        
        return success;
    }

    public void StopSpectorCapture()
    {
        CheckIsInitialized();
        
        if (_isSpectorCaptureStarted)
        {
            StopSpectorCaptureJs();
            _isSpectorCaptureStarted = false;
        }
    }


    /// <summary>
    /// Invokes the specified JavaScript function asynchronously.
    /// </summary>
    /// <param name="identifier">An identifier for the function to invoke.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invocation operation.</returns>
    public async Task InvokeVoidAsync(string identifier, params object?[]? args)
    {
        await InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(identifier, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function asynchronously.
    /// <para>
    /// <see cref="Microsoft.JSInterop.JSRuntime"/> will apply timeouts to this operation based on the value configured in <see cref="Microsoft.JSInterop.JSRuntime.DefaultAsyncTimeout"/>. To dispatch a call with a different timeout, or no timeout,
    /// consider using <see cref="InvokeAsync{TValue}(string, CancellationToken, object[])" />.
    /// </para>
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public async Task<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (JS == null)
            throw new SharpEngineException("Cannot invoke javascript function because IJSRuntime is not initialized. Make sure that InitializeInterop method was called.");

        return await JS.InvokeAsync<TValue>(identifier, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function asynchronously.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="Microsoft.JSInterop.JSRuntime.DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public async Task<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (JS == null)
            throw new SharpEngineException("Cannot invoke javascript function because IJSRuntime is not initialized. Make sure that InitializeInterop method was called.");

        return await JS.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Logs the specified message to the browser console.
    /// When useIJSRuntime is false (by default), then Console.WriteLine is used to log the message. This writes multiline message as multiple log messages.
    /// When useIJSRuntime is true, then the message is logged using JavaScript interop to write the message. In this case the multiline message is logged as a single log message. 
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="useIJSRuntime">true to log the message using JavaScript interop to the browser console; false to log to the standard output. The default is false.</param>
    public void LogMessage(string message, bool useIJSRuntime = false)
    {
        if (useIJSRuntime && IsInvokeSupported)
            _ = InvokeVoidAsync("console.log", message);
        else
            Console.WriteLine(message);
    }

    /// <summary>
    /// Logs the specified error message to the browser console.
    /// When useIJSRuntime is false (by default), then Console.Error.WriteLine is used to log the message. This writes multiline message as multiple log messages.
    /// When useIJSRuntime is true, then the error message is logged using JavaScript interop to write the message. In this case the multiline message is logged as a single log message. 
    /// </summary>
    /// <param name="errorMessage">The error message to log.</param>
    /// <param name="useIJSRuntime">true to log the message using JavaScript interop to the browser console; false to log to the standard output. The default is false.</param>
    public void LogError(string errorMessage, bool useIJSRuntime = false)
    {
        if (useIJSRuntime && IsInvokeSupported)
            _ = InvokeVoidAsync("console.error", errorMessage);
        else
            Console.Error.WriteLine(errorMessage);
    }


    public void Dispose()
    { 
        if (IsDisposed)
            return;

        Disposing?.Invoke(this, EventArgs.Empty);

        DisconnectWebGLCanvasJs(CanvasId);

        ArePointerEventsSubscribed = false;
        IsWebGLInitialized = false;

        if (_textFileLoadedCallbacks != null && _textFileLoadedCallbacks.Count > 0)
        {
            _urlRequestsOnDisposedInterop ??= new List<string>();
            _urlRequestsOnDisposedInterop.AddRange(_textFileLoadedCallbacks.Keys);

            _textFileLoadedCallbacks.Clear();
        }
        
        if (_binaryFileLoadedCallbacks != null && _binaryFileLoadedCallbacks.Count > 0)
        {
            _urlRequestsOnDisposedInterop ??= new List<string>();
            _urlRequestsOnDisposedInterop.AddRange(_binaryFileLoadedCallbacks.Keys);

            _binaryFileLoadedCallbacks.Clear();
        }
        
        if (_imageBytesLoadedCallbacks != null && _imageBytesLoadedCallbacks.Count > 0)
        {
            _urlRequestsOnDisposedInterop ??= new List<string>();
            _urlRequestsOnDisposedInterop.AddRange(_imageBytesLoadedCallbacks.Keys);

            _imageBytesLoadedCallbacks.Clear();
        }

        if (_initialInterop == this)
        {
            _initialInterop = null;

            if (_additionalInteropObjects != null && _additionalInteropObjects.Count > 0)
            {
                // Move first CanvasInterop from _additionalInteropObjects to _initialInterop so it is found faster
                _initialInterop = _additionalInteropObjects[0];
                _additionalInteropObjects.RemoveAt(0);
            }
        }
        else if (_additionalInteropObjects != null)
        {
            _additionalInteropObjects.Remove(this);
        }
        
        if (_additionalInteropObjects != null && _additionalInteropObjects.Count == 0) 
            _additionalInteropObjects = null;

        IsDisposed = true;
    }

    #region OnPointerButtonPressed and other On... methods
    protected void OnPointerButtonPressed(float x, float y, int changedButton, int pressedButtons, int pointerId, int keyboardModifiers)
    {
        if (PointerDown != null)
            PointerDown(this, new MouseButtonEventArgs(x, y, (MouseButton)changedButton, (PointerButtons)pressedButtons, pointerId, (KeyboardModifiers)keyboardModifiers));
    }
    
    protected void OnPointerButtonReleased(float x, float y, int changedButton, int pressedButtons, int pointerId, int keyboardModifiers)
    {
        if (PointerUp != null)
            PointerUp(this, new MouseButtonEventArgs(x, y, (MouseButton)changedButton, (PointerButtons)pressedButtons, pointerId, (KeyboardModifiers)keyboardModifiers));
    }
    
    protected void OnPointerMoved(float x, float y, int buttons, int keyboardModifiers)
    {
        if (PointerMoved != null)
            PointerMoved(this, new MouseMoveEventArgs(x, y, (PointerButtons)buttons, (KeyboardModifiers)keyboardModifiers));
    }
    
    protected void OnPointerEntered(float x, float y, int buttons, int keyboardModifiers)
    {
        if (PointerEntered != null)
            PointerEntered(this, new MouseMoveEventArgs(x, y, (PointerButtons)buttons, (KeyboardModifiers)keyboardModifiers));
    }
    
    protected void OnPointerExited(float x, float y, int buttons, int keyboardModifiers)
    {
        if (PointerExited != null)
            PointerExited(this, new MouseMoveEventArgs(x, y, (PointerButtons)buttons, (KeyboardModifiers)keyboardModifiers));
    }
    
    protected void OnMouseWheelChanged(float deltaX, float deltaY, float x, float y, int buttons, int keyboardModifiers)
    {
        if (MouseWheelChanged != null)
            MouseWheelChanged(this, new MouseWheelEventArgs(deltaX, deltaY, x, y, (PointerButtons)buttons, (KeyboardModifiers)keyboardModifiers));
    }
    
    protected void OnPinchZoomStarted(float distance, float centerX, float centerY)
    {
        if (PinchZoomStarted != null)
            PinchZoomStarted(this, new PinchZoomEventArgs(distance, new Vector2(centerX, centerY)));
    }
    
    protected void OnPinchZoomEnded()
    {
        if (PinchZoomEnded != null)
            PinchZoomEnded(this, EventArgs.Empty);
    }
    
    protected void OnPinchZoomed(float distance, float centerX, float centerY)
    {
        if (PinchZoomed != null)
            PinchZoomed(this, new PinchZoomEventArgs(distance, new Vector2(centerX, centerY)));
    }
    
    protected void OnBrowserAnimationFrameUpdated()
    {
        if (BrowserAnimationFrameUpdated != null)
            BrowserAnimationFrameUpdated(this, EventArgs.Empty);
    }
    
    protected void OnCanvasResized(float width, float height, float devicePixelRatio)
    {
        if (CanvasResized != null)
            CanvasResized(this, new CanvasResizedEventArgs(width, height, devicePixelRatio));
    }
    
    protected void OnContextLost()
    {
        ContextLost?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region JSExport methods: OnFrameUpdateJsCallback, OnPointerMovedJsCallback, ...
    [JSExport]
    private static void OnFrameUpdateJsCallback()
    {
        //if (IsLoggingInteropEvents)
        //    Console.WriteLine("OnFrameUpdate");

        _initialInterop?.OnBrowserAnimationFrameUpdated();
        
        if (_additionalInteropObjects != null)
        {
            foreach (var canvasInterop in _additionalInteropObjects)
                canvasInterop.OnBrowserAnimationFrameUpdated();
        }
    }
    
    [JSExport]
    private static void OnTextFileLoaded(string canvasId, string url, string? fileContent, string? errorText)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnTextFileLoaded '{url}' ({(fileContent?.Length ?? 0):N0} chars) for canvasId: '{canvasId ?? ""}'");

        var canvasInterop = GetCanvasInterop(canvasId, url);

        if (canvasInterop == null ||
            canvasInterop._textFileLoadedCallbacks == null ||
            !canvasInterop._textFileLoadedCallbacks.Remove(url, out var callbacks))
        {
            return;
        }

        if (fileContent == null)
        {
            if (errorText == null)
                errorText = "Error loading text file: " + url;

            // Maybe more than one callback is registered for the same imageUrl
            foreach (var callback in callbacks)
                callback.onError?.Invoke(errorText);
        }
        else
        {
            // Maybe more than one callback is registered for the same imageUrl
            foreach (var callback in callbacks)
                callback.onLoaded(fileContent);
        }
    }
    
    [JSExport]
    private static void OnBinaryFileLoaded(string canvasId, string url, byte[]? fileBytes, string? errorText)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnBinaryFileLoaded '{url}' ({(fileBytes?.Length ?? 0):N0} bytes) for canvasId: '{canvasId ?? ""}'");

        var canvasInterop = GetCanvasInterop(canvasId, url);

        if (canvasInterop == null ||
            canvasInterop._binaryFileLoadedCallbacks == null ||
            !canvasInterop._binaryFileLoadedCallbacks.Remove(url, out var callbacks))
        {
            return;
        }

        if (fileBytes == null)
        {
            if (errorText == null)
                errorText = "Error loading binary file: " + url;

            // Maybe more than one callback is registered for the same imageUrl
            foreach (var callback in callbacks)
                callback.onError?.Invoke(errorText);
        }
        else
        {
            // Maybe more than one callback is registered for the same imageUrl
            foreach (var callback in callbacks)
                callback.onLoaded(fileBytes);
        }
    }
    
    [JSExport]
    private static void OnImageBytesLoaded(string canvasId, string imageUrl, int width, int height, byte[]? imageBytes, string? errorText)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnImageBytesLoaded '{imageUrl}' ({width} x {height}) for canvasId: '{canvasId ?? ""}'");

        var canvasInterop = GetCanvasInterop(canvasId, imageUrl);

        if (canvasInterop == null ||
            canvasInterop._imageBytesLoadedCallbacks == null ||
            !canvasInterop._imageBytesLoadedCallbacks.Remove(imageUrl, out var callbacks))
        {
            return;
        }

        if (imageBytes == null)
        {
            if (errorText == null)
                errorText = "Error loading texture: " + imageUrl;

            // Maybe more than one callback is registered for the same imageUrl
            foreach (var callback in callbacks)
                callback.onError?.Invoke(errorText);
        }
        else
        {
            var rawImageData = new RawImageData(width, height, width * 4, PixelFormat.Rgba, imageBytes, checkTransparency: true);

            // Maybe more than one callback is registered for the same imageUrl
            foreach (var callback in callbacks)
                callback.onLoaded(rawImageData);
        }
    }

    [JSExport]
    private static void OnPointerMovedJsCallback(string? canvasId, float x, float y, int buttons, int keyboardModifiers)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPointerMoved '{canvasId ?? ""}': {x} {y}  Buttons: {buttons}  KeyboardModifiers: {keyboardModifiers}");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPointerMoved(x, y, buttons, keyboardModifiers);
    }

    [JSExport]
    private static void OnPointerDownJsCallback(string? canvasId, float x, float y, int changedButton, int pressedButtons, int pointerId, int keyboardModifiers)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPointerDown button '{canvasId ?? ""}': {changedButton}  KeyboardModifiers: {keyboardModifiers}");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPointerButtonPressed(x, y, changedButton, pressedButtons, pointerId, keyboardModifiers);
    }

    [JSExport]
    private static void OnPointerUpJsCallback(string? canvasId, float x, float y, int changedButton, int pressedButtons, int pointerId, int keyboardModifiers)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPointerUp button '{canvasId ?? ""}': {changedButton}  KeyboardModifiers: {keyboardModifiers}");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPointerButtonReleased(x, y, changedButton, pressedButtons, pointerId, keyboardModifiers);
    }
    
    [JSExport]
    private static void OnPointerEnterJsCallback(string? canvasId, float x, float y, int pressedButtons, int keyboardModifiers)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPointerEnter at {x} {y}");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPointerEntered(x, y, pressedButtons, keyboardModifiers);
    }
    
    [JSExport]
    private static void OnPointerLeaveJsCallback(string? canvasId, float x, float y, int pressedButtons, int keyboardModifiers)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPointerLeave at {x} {y}");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPointerExited(x, y, pressedButtons, keyboardModifiers);
    }

    [JSExport]
    private static void OnMouseWheelJsCallback(string? canvasId, float deltaX, float deltaY, float x, float y, int buttons, int keyboardModifiers)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnMouseWheel '{canvasId ?? ""}': {deltaX} {deltaY}  KeyboardModifiers: {keyboardModifiers}");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnMouseWheelChanged(deltaX, deltaY, x, y, buttons, keyboardModifiers);
    }

    [JSExport]
    private static void OnPinchZoomStartedJsCallback(string? canvasId, float distance, float centerX, float centerY)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPinchZoomStarted '{canvasId ?? ""}': distance: {distance} around ({centerX} {centerY})");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPinchZoomStarted(distance, centerX, centerY);
    }

    [JSExport]
    private static void OnPinchZoomEndedJsCallback(string? canvasId)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPinchZoomEnded '{canvasId ?? ""}'");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPinchZoomEnded();
    }

    [JSExport]
    private static void OnPinchZoomJsCallback(string? canvasId, float distance, float centerX, float centerY)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnPinchZoom '{canvasId ?? ""}': distance: {distance} around ({centerX} {centerY})");

        var canvasInterop = GetCanvasInterop(canvasId);
        canvasInterop?.OnPinchZoomed(distance, centerX, centerY);
    }


    [JSExport]
    private static void OnCanvasResizedJsCallback(string? canvasId, float width, float height, float devicePixelRatio)
    {
        if (IsLoggingInteropEvents)
            Console.WriteLine($"OnCanvasResized '{canvasId ?? ""}': {width} {height} {devicePixelRatio}");

        var canvasInterop = GetCanvasInterop(canvasId);

        if (canvasInterop != null)
        {
            canvasInterop.Width = (int)width;
            canvasInterop.Height = (int)height;
            canvasInterop.DpiScale = devicePixelRatio;

            canvasInterop.OnCanvasResized(width, height, devicePixelRatio);
        }
    }
    
    [JSExport]
    private static void OnContextLostJsCallback(string? canvasId)
    {
        Console.WriteLine($"OnContextLostJsCallback '{canvasId ?? ""}'");

        var canvasInterop = GetCanvasInterop(canvasId);

        if (canvasInterop != null)
            canvasInterop.OnContextLost();
    }
    #endregion
    
    #region JSImport methods: InitInteropAsync, InitWebGLCanvasJs, SubscribeBrowserEventsJs, ...

    [JSImport("initInteropAsync", "sharp-engine.js")]
    private static partial Task InitInteropAsync();

    // Returns the string in the format: "OK:width;height;dpiScale" or error text (if text does not start with "OK:")
    // It is not possible (at least in .Net 9) to pass an objects from JS to .Net
    // It was possible to encode width and height into an int, but we also need dpiScale, so we need to pass it as a string.
    [JSImport("initWebGLCanvas", "sharp-engine.js")]
    private static partial string InitWebGLCanvasJs(string canvasId, bool useMSAA, bool preserveDrawingBuffer, bool subscribePointerEvents, bool subscribeRequestAnimationFrame, bool preventShowingContextMenu, bool enableJavaScriptLogging);

    [JSImport("loadTextFile", "sharp-engine.js")]
    private static partial void LoadTextFileJs(string canvasId, string url);
    
    [JSImport("loadBinaryFile", "sharp-engine.js")]
    private static partial void LoadBinaryFileJs(string canvasId, string url);
    
    [JSImport("loadImageBytes", "sharp-engine.js")]
    private static partial void LoadImageBytesJs(string canvasId, string imageUrl);
    
    [JSImport("createImageFromBytes", "sharp-engine.js")]
    private static partial void CreateImageFromBytesJS(string canvasId, byte[] imageBytes, string mimeType, string imageName);
    
    [JSImport("subscribeBrowserEvents", "sharp-engine.js")]
    private static partial void SubscribeBrowserEventsJs(string canvasId, bool subscribePointerEvents, bool subscribeRequestAnimationFrame);
    
    [JSImport("unsubscribeBrowserEvents", "sharp-engine.js")]
    private static partial void UnsubscribeBrowserEventsJs(string canvasId, bool unsubscribePointerEvents, bool unsubscribeRequestAnimationFrame);

    [JSImport("getCanvasSize", "sharp-engine.js")]
    private static partial int GetCanvasSizeJs(string canvasId, bool useDpiScale);
    
    [JSImport("setCursorStyle", "sharp-engine.js")]
    private static partial int SetCursorStyleJs(string canvasId, string cursorStyle);
    
    [JSImport("setPointerCapture", "sharp-engine.js")]
    private static partial int SetPointerCaptureJs(string canvasId, int pointerId);
    
    [JSImport("releasePointerCapture", "sharp-engine.js")]
    private static partial int ReleasePointerCaptureJs(string canvasId, int pointerId);

    [JSImport("startSpectorCapture", "sharp-engine.js")]
    private static partial bool StartSpectorCaptureJs(string canvasId);

    [JSImport("stopSpectorCapture", "sharp-engine.js")]
    private static partial void StopSpectorCaptureJs();
        
    [JSImport("disconnectWebGLCanvas", "sharp-engine.js")]
    public static partial bool DisconnectWebGLCanvasJs(string canvasId);
    
    [JSImport("showRawBitmap", "sharp-engine.js")]
    public static partial bool ShowRawBitmapJs(string canvasId, int width, int height, byte[] pixelData, string? displayStyle);
    #endregion    
}

//#pragma warning restore CA1416
