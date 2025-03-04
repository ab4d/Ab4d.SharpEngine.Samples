using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Diagnostics;

// This class provides the common UI independent code that is used for DiagnosticsWindow in specific UI project
public class CommonDiagnostics
{
    public const int UpdateStatisticsInterval = 100; // 100 ms = update statistics 10 times per second

    private bool _isManuallyEnabledCollectingStatistics;

    private bool _isGpuDeviceCreatedSubscribed;

    private Queue<double>? _fpsQueue;
    private double _lastFps;

    private bool _isOnSceneRenderedSubscribed;

    private StringBuilder? _renderingStatisticStringBuilder;
    private RenderingStatistics? _lastRenderingStatisticsWithRecorderCommandBuffers;

    private SceneDirtyFlags _lastSceneDirtyFlags;
    private SceneViewDirtyFlags _lastSceneViewDirtyFlags;

    private IBitmapIO _bitmapIO;

    private ISharpEngineSceneView? _sharpEngineSceneView;

    public ISharpEngineSceneView? SharpEngineSceneView
    {
        get { return _sharpEngineSceneView; }
        set
        {
            if (ReferenceEquals(_sharpEngineSceneView, value))
                return;

            ClearLogMessages(); // Clear log messages and warnings from previous DXView

            RegisterSceneView(value);
            _sharpEngineSceneView = value;
        }
    }

    public bool IsSharpEngineDebugBuild { get; }

    public Version SharpEngineVersion { get; }

    public List<Tuple<LogLevels, string>> LogMessages { get; }
    public int NumberOfWarnings { get; private set; }


    public Action<int>? WarningsCountChangedAction;
    public Action? NewLogMessageAddedAction;
    public Action? DeviceInfoChangedAction;
    public Action? OnSceneRenderedAction;
    public Action<string>? ShowMessageBoxAction;


    private Type? _gltfExporterType;
    private object? _gltfExporter;

    private bool? _isGltfExporterAvailable;
    private MethodInfo? _addSceneMethodInfo;
    private MethodInfo? _exportEmbeddedMethodInfo;
    private MethodInfo? _exportBinaryMethodInfo;
    public bool IsGltfExporterAvailable => _isGltfExporterAvailable ?? CheckGltfExporter();

    public CommonDiagnostics(IBitmapIO bitmapIO)
    {
        _bitmapIO = bitmapIO;

        // Set SharpEngine assembly version
        SharpEngineVersion = typeof(VulkanDevice).Assembly.GetName().Version ?? new Version(0, 0);

        // IsDebugVersion field is defined only in Debug version
        var fieldInfo = typeof(VulkanDevice).GetField("IsDebugVersion");
        IsSharpEngineDebugBuild = fieldInfo != null;
        
        LogMessages = new List<Tuple<LogLevels, string>>();
        _fpsQueue   = new Queue<double>(50);
    }

    public string GetSharpEngineInfoText()
    {
        return string.Format("Ab4d.SharpEngine v{0}.{1}.{2}{3}",
            SharpEngineVersion.Major, SharpEngineVersion.Minor, SharpEngineVersion.Build,
            IsSharpEngineDebugBuild ? " (debug build)" : "");
    }

    public void StartShowingStatistics()
    {
        SubscribeOnSceneRendered();
        
        // Enable collecting statistics if it was not done yet
        if (SharpEngineSceneView != null && !SharpEngineSceneView.SceneView.IsCollectingStatistics)
        {
            SharpEngineSceneView.SceneView.IsCollectingStatistics = true;
            _isManuallyEnabledCollectingStatistics = true;

            SharpEngineSceneView.SceneView.Render(forceRender: true); // Force render so we get on statistics and are not showing empty data
        }
    }

    public void EndShowingStatistics()
    {
        UnsubscribeOnSceneRendered();

        if (_isManuallyEnabledCollectingStatistics)
        {
            if (SharpEngineSceneView != null)
                SharpEngineSceneView.SceneView.IsCollectingStatistics = false;

            _isManuallyEnabledCollectingStatistics = true;
        }
    }

    public void RegisterSceneView(ISharpEngineSceneView? sharpEngineSceneView)
    {
        UnregisterCurrentSceneView();

        _sharpEngineSceneView = sharpEngineSceneView;

        if (sharpEngineSceneView == null)
            return;

        sharpEngineSceneView.ViewSizeChanged += SharpEngineSceneViewOnViewSizeChanged;

        sharpEngineSceneView.Disposing += OnSceneViewDisposing;

        if (sharpEngineSceneView.GpuDevice == null)
        {
            sharpEngineSceneView.GpuDeviceCreated += SharpEngineSceneViewOnGpuDeviceCreated;
            _isGpuDeviceCreatedSubscribed = true;
        }

        OnDeviceInfoChanged();

        Log.AddLogListener(OnLogAction);
    }
    
    public void UnregisterCurrentSceneView()
    {
        if (_sharpEngineSceneView == null)
            return;

        Log.RemoveLogListener(OnLogAction);

        UnsubscribeOnSceneRendered();

        _sharpEngineSceneView.Disposing -= OnSceneViewDisposing;
        _sharpEngineSceneView.ViewSizeChanged -= SharpEngineSceneViewOnViewSizeChanged;

        if (_isGpuDeviceCreatedSubscribed)
        {
            _sharpEngineSceneView.GpuDeviceCreated -= SharpEngineSceneViewOnGpuDeviceCreated;
            _isGpuDeviceCreatedSubscribed = false;
        }

        _sharpEngineSceneView = null;

        OnDeviceInfoChanged();

        EndShowingStatistics();
    }

    private void SharpEngineSceneViewOnViewSizeChanged(object sender, ViewSizeChangedEventArgs e)
    {
        OnDeviceInfoChanged();
    }

    private void SharpEngineSceneViewOnGpuDeviceCreated(object sender, GpuDeviceCreatedEventArgs e)
    {
        if (_sharpEngineSceneView != null)
            _sharpEngineSceneView.GpuDeviceCreated -= SharpEngineSceneViewOnGpuDeviceCreated;

        _isGpuDeviceCreatedSubscribed = false;

        OnDeviceInfoChanged();
    }

    private void OnSceneViewDisposing(object? sender, bool disposing)
    {
        if (disposing)
            UnregisterCurrentSceneView();
    }
    
    public string GetDeviceInfo()
    {
        if (_sharpEngineSceneView == null || !_sharpEngineSceneView.SceneView.BackBuffersInitialized)
            return "SharpEngineSceneView is not initialized";


        string viewInfo;
        var sceneView = _sharpEngineSceneView.SceneView;

        if (sceneView.BackBuffersInitialized)
        {
            viewInfo = $"{sceneView.Width} x {sceneView.Height}";

            if (sceneView.MultisampleCount > 1)
                viewInfo += $" x {sceneView.MultisampleCount}xMSAA";

            var supersamplingCount = sceneView.SupersamplingCount; // number of pixels used for one final pixel
            if (supersamplingCount > 1)
                viewInfo += string.Format(" x {0:0.#}xSSAA", supersamplingCount);

            viewInfo += $" ({_sharpEngineSceneView.PresentationType})";
        }
        else
        {
            viewInfo = "";
        }


        if (_sharpEngineSceneView.GpuDevice != null)
        {
            string deviceInfoText = _sharpEngineSceneView.GpuDevice.GpuName;
            viewInfo = deviceInfoText + Environment.NewLine + viewInfo;
        }

        return viewInfo;
    }

    private void SubscribeOnSceneRendered()
    {
        if (_isOnSceneRenderedSubscribed || _sharpEngineSceneView == null)
            return;

        _sharpEngineSceneView.SceneRendered += SceneViewOnSceneRendered;
        _isOnSceneRenderedSubscribed = true;
    }

    private void UnsubscribeOnSceneRendered()
    {
        if (!_isOnSceneRenderedSubscribed || _sharpEngineSceneView == null)
            return;

        _sharpEngineSceneView.SceneRendered -= SceneViewOnSceneRendered;
        _isOnSceneRenderedSubscribed = false;
    }
    
    private void SceneViewOnSceneRendered(object? sender, EventArgs eventArgs)
    {
        UpdateStatistics();

        OnSceneRenderedAction?.Invoke();
    }

    private bool CheckGltfExporter()
    {
        _gltfExporterType = Type.GetType("Ab4d.SharpEngine.glTF.glTFExporter, Ab4d.SharpEngine.glTF", throwOnError: false);

        var isGltfExporterAvailable = _gltfExporterType != null;

        // The following code can be used to manually load Ab4d.SharpEngine.glTF.dll
        // This can be enabled only after the gltf2Loader is embedded into the Ab4d.SharpEngine.glTF so that we do not need gltf2Loader.dll and Newtonsoft.Json.dll
        //if (!isGltfExporterAvailable)
        //{
        //    var gltfAssemblyFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ab4d.SharpEngine.glTF.dll");

        //    if (System.IO.File.Exists(gltfAssemblyFileName))
        //    {
        //        try
        //        {
        //            var gltfAssembly = System.Reflection.Assembly.LoadFrom(gltfAssemblyFileName);

        //            _gltfExporterType = gltfAssembly.GetType("Ab4d.SharpEngine.glTF.glTFExporter", throwOnError: false);

        //            isGltfExporterAvailable = _gltfExporterType != null;
        //        }
        //        catch
        //        {
        //            // pass
        //        }
        //    }
        //}

        _isGltfExporterAvailable = isGltfExporterAvailable;
        return isGltfExporterAvailable;
    }

    public bool ExportScene(Scene scene, SceneView sceneView, string fileName) // sceneView is not yet used, but may be used in the future to export the camera
    {
        if (_gltfExporterType == null)
            return false;

        // Use Reflection to call 
        if (_gltfExporter == null)
        {
            _gltfExporter = Activator.CreateInstance(_gltfExporterType);

            if (_gltfExporter == null)
                return false;

            _addSceneMethodInfo = _gltfExporter.GetType().GetMethod("AddScene");
            _exportEmbeddedMethodInfo = _gltfExporter.GetType().GetMethod("ExportEmbedded", BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(string)});
            _exportBinaryMethodInfo = _gltfExporter.GetType().GetMethod("ExportBinary", BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(string)});
        }

        // Call "AddScene(scene)"
        _addSceneMethodInfo!.Invoke(_gltfExporter, new object?[] { scene });

        // Call "ExportEmbedded(fileName)"
        if (fileName.EndsWith("glb", StringComparison.OrdinalIgnoreCase))
            _exportBinaryMethodInfo!.Invoke(_gltfExporter, new object?[] { fileName });
        else
            _exportEmbeddedMethodInfo!.Invoke(_gltfExporter, new object?[] { fileName });

        return true;
    }

    private void UpdateStatistics()
    {
        if (SharpEngineSceneView == null || SharpEngineSceneView.SceneView.Statistics == null)
            return;

        var renderingStatistics = SharpEngineSceneView.SceneView.Statistics;

        double frameTime = renderingStatistics.UpdateTimeMs + renderingStatistics.TotalRenderTimeMs;
        double fps = 1000 / frameTime;


        // Update average fps
        int averageResultsCount = 2000 / UpdateStatisticsInterval; // 2 seconds for default update interval (100) => averageResultsCount = 20 - every 20 statistical results we calculate an average
        if (averageResultsCount <= 1)
            averageResultsCount = 1;

        _fpsQueue ??= new Queue<double>(averageResultsCount);

        if (_fpsQueue.Count == averageResultsCount)
            _fpsQueue.Dequeue(); // dump the result that is farthest away

        _fpsQueue.Enqueue(fps);

        _lastFps = fps;


        if (renderingStatistics.DrawCallsCount > 0)
        {
            // We store last RenderingStatistics that has any draw calls
            _lastRenderingStatisticsWithRecorderCommandBuffers = renderingStatistics.Clone();
        }

        if (SharpEngineSceneView?.SceneView.RenderingContext != null)
        {
            _lastSceneViewDirtyFlags = SharpEngineSceneView.SceneView.RenderingContext.SceneViewDirtyFlags;
            _lastSceneDirtyFlags     = SharpEngineSceneView.SceneView.RenderingContext.SceneDirtyFlags;
        }
    }

    public string GetRenderingStatisticsText()
    {
        if (SharpEngineSceneView == null || SharpEngineSceneView.SceneView.Statistics == null)
            return "";


        string averageFpsText;

        if (_fpsQueue != null && _fpsQueue.Count >= 10)
        {
            double averageFps = _fpsQueue.Average();
            averageFpsText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "; avrg: {0:0.0}", averageFps);
        }
        else
        {
            averageFpsText = "";
        }

        string fpsText = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} FPS{1}", _lastFps, averageFpsText);


        string statisticsText;

        try
        {
            statisticsText = GetRenderingStatisticsDetails(SharpEngineSceneView.SceneView.Statistics, fpsText);
        }
        catch (Exception ex)
        {
            statisticsText = "Error getting rendering statistics:\r\n" + ex.Message;
            if (ex.InnerException != null)
                statisticsText += Environment.NewLine + ex.InnerException.Message;
        }

        return statisticsText;
    }


    // Returns true if camera rotation was started
    public bool ToggleCameraRotation()
    {
        if (SharpEngineSceneView == null)
            return false;

        var camera = SharpEngineSceneView.SceneView.Camera;

        if (camera == null)
            return false;

        var rotatingCamera = camera as IRotatingCamera;

        if (rotatingCamera == null)
        {
            OnShowMessageBox($"The used camera {camera.GetType().Name} does not support IRotatingCamera interface and cannot be animated");
            return false;
        }

        if (rotatingCamera.IsRotating)
        {
            rotatingCamera.StopRotation();
            return false;
        }
         
        rotatingCamera.StartRotation(50, 0); 
        return true;
    }

    public void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private string GetRenderingStatisticsDetails(RenderingStatistics renderingStatistics, string? fpsText)
    {
        if (fpsText == null)
            fpsText = "";

        if (fpsText.Length > 0 && !fpsText.Contains("("))
            fpsText = '(' + fpsText + ')';

        if (_renderingStatisticStringBuilder == null)
            _renderingStatisticStringBuilder = new StringBuilder();
        else
            _renderingStatisticStringBuilder.Clear();


        string commandBuffersRecordingTime;
        if (renderingStatistics.CommandBuffersRecordingTimeMs > 0.01)
            commandBuffersRecordingTime = $"CommandBuffersRecording: {renderingStatistics.CommandBuffersRecordingTimeMs:0.00} ms" + Environment.NewLine;
        else
            commandBuffersRecordingTime = "";

        string waitUntilRenderedTime;
        if (renderingStatistics.WaitUntilRenderedTimeMs > 0.01)
            waitUntilRenderedTime = $"WaitUntilRenderedTime: {renderingStatistics.WaitUntilRenderedTimeMs:0.00} ms" + Environment.NewLine;
        else
            waitUntilRenderedTime = "";

        string stagingUsageTime;
        if (renderingStatistics.StagingUsageTimeMs > 0.01)
            stagingUsageTime = $"StagingUsageTime: {renderingStatistics.StagingUsageTimeMs:0.00} ms" + Environment.NewLine;
        else
            stagingUsageTime = "";
        
        string frameCopyTime;
        if (renderingStatistics.FrameCopyTimeMs > 0.01)
            frameCopyTime = $"FrameCopyTime: {renderingStatistics.FrameCopyTimeMs:0.00} ms" + Environment.NewLine;
        else
            frameCopyTime = "";

        _renderingStatisticStringBuilder.AppendFormat(
            System.Globalization.CultureInfo.InvariantCulture,
@"Frame number: {0:#,##0}
CommandBuffer version: {1}
RenderingLayers version: {2}
Frame time: {3:0.00} ms {4}
UpdateTime: {5:0.00} ms
PrepareRenderTime: {6:0.00} ms
{7}CompleteRenderTime: {8:0.00} ms
{9}{10}{11}UpdatedBuffers: Count: {12}; Size: {13}",
            renderingStatistics.FrameNumber,
            renderingStatistics.CommandBuffersRecordedCount,
            renderingStatistics.RenderingLayersRecreateCount,
            renderingStatistics.UpdateTimeMs + renderingStatistics.TotalRenderTimeMs,
            fpsText,
            renderingStatistics.UpdateTimeMs,
            renderingStatistics.PrepareRenderTimeMs,
            commandBuffersRecordingTime,
            renderingStatistics.CompleteRenderTimeMs,
            waitUntilRenderedTime,
            stagingUsageTime,
            frameCopyTime,
            renderingStatistics.UpdatedBuffersCount,
            FormatMemorySize(renderingStatistics.UpdatedBuffersSize));


        if (renderingStatistics.Other.Count > 0)
        {
            foreach (var keyValuePair in renderingStatistics.Other)
            {
                var oneValue = keyValuePair.Value;

                if (oneValue is null)
                    continue;

                string oneValueText;

                if (oneValue is string stringValue)
                    oneValueText = stringValue;
                else if (oneValue is float || oneValue is double)
                    oneValueText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}{1}", oneValue, keyValuePair.Key.Contains("Time", StringComparison.OrdinalIgnoreCase) ? " ms" : "");
                else
                    oneValueText = oneValue.ToString()!;

                _renderingStatisticStringBuilder.AppendLine().Append(keyValuePair.Key).Append(": ").Append(oneValueText);
            }
        }


        if (renderingStatistics.DrawCallsCount > 0)
        {
            _renderingStatisticStringBuilder.AppendFormat(
                System.Globalization.CultureInfo.InvariantCulture,
@"
CommandBuffersRecordingTime: {0:0.00} ms
DrawCallsCount: {1:#,##0}
DrawnVerticesCount: {2:#,##0}
DrawnIndicesCount: {3:#,##0}
VertexBuffersChangesCount: {4:#,##0}
IndexBuffersChangesCount: {5:#,##0}
DescriptorSetChangesCount: {6:#,##0}
PushConstantsChangesCount: {7:#,##0}
PipelineChangesCount: {8:#,##0}",
                renderingStatistics.CommandBuffersRecordingTimeMs,
                renderingStatistics.DrawCallsCount,
                renderingStatistics.DrawnVerticesCount,
                renderingStatistics.DrawnIndicesCount,
                renderingStatistics.VertexBuffersChangesCount,
                renderingStatistics.IndexBuffersChangesCount,
                renderingStatistics.DescriptorSetChangesCount,
                renderingStatistics.PushConstantsChangesCount,
                renderingStatistics.PipelineChangesCount);
        }
        else if (_lastRenderingStatisticsWithRecorderCommandBuffers != null)
        {
            _renderingStatisticStringBuilder.AppendFormat(
                System.Globalization.CultureInfo.InvariantCulture,
@"

Command buffer recorded in frame {0:#,##0}:
CommandBuffersRecordingTime: {1:0.00} ms
DrawCallsCount: {2:#,##0}
DrawnVerticesCount: {3:#,##0}
DrawnIndicesCount: {4:#,##0}
VertexBuffersChangesCount: {5:#,##0}
IndexBuffersChangesCount: {6:#,##0}
DescriptorSetChangesCount: {7:#,##0}
PipelineChangesCount: {8:#,##0}",
                _lastRenderingStatisticsWithRecorderCommandBuffers.FrameNumber,
                _lastRenderingStatisticsWithRecorderCommandBuffers.CommandBuffersRecordingTimeMs,
                _lastRenderingStatisticsWithRecorderCommandBuffers.DrawCallsCount,
                _lastRenderingStatisticsWithRecorderCommandBuffers.DrawnVerticesCount,
                _lastRenderingStatisticsWithRecorderCommandBuffers.DrawnIndicesCount,
                _lastRenderingStatisticsWithRecorderCommandBuffers.VertexBuffersChangesCount,
                _lastRenderingStatisticsWithRecorderCommandBuffers.IndexBuffersChangesCount,
                _lastRenderingStatisticsWithRecorderCommandBuffers.DescriptorSetChangesCount,
                _lastRenderingStatisticsWithRecorderCommandBuffers.PipelineChangesCount);
        }

        _renderingStatisticStringBuilder.AppendFormat("\r\n\r\nSceneViewDirtyFlags: {0}\r\nSceneDirtyFlags: {1}", _lastSceneViewDirtyFlags, _lastSceneDirtyFlags);

        return _renderingStatisticStringBuilder.ToString();
    }

    private static string FormatMemorySize(long size)
    {
        if (size >= 1024 * 1024 * 1024)
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:#,##0.##} GB", (double)size / (1024 * 1024 * 1024));

        if (size >= 1024 * 1024)
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##} MB", (double)size / (1024 * 1024));

        if (size >= 1024)
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.#} KB", (double)size / 1024);

        return string.Format("{0} B", size);
    }

    public string GetSceneNodesDumpString()
    {
        if (SharpEngineSceneView == null)
            return "";

        string dumpText;
        try
        {
            dumpText = GetSceneNodesInfo(SharpEngineSceneView.Scene);
        }
        catch (Exception ex)
        {
            dumpText = "Exception occurred when calling Scene.GetSceneNodesInfo:\r\n" + ex.Message;
        }

        dumpText += "\r\n\r\nLights:\r\n";

        foreach (var light in SharpEngineSceneView.Scene.Lights)
        {
            dumpText += "  " + light.ToString();
            dumpText += Environment.NewLine;
        }

        return dumpText;
    }

    private string GetSceneNodesInfo(Scene scene)
    {
        return scene.GetSceneNodesInfo(showLocalBoundingBox: true); // all other parameters are already true by default
    }

    public string GetUsedMaterialsDumpString()
    {
        if (SharpEngineSceneView == null)
            return "";

        string dumpText;

        try
        {
            dumpText = SharpEngineSceneView.Scene.GetUsedMaterialsInfo();
        }
        catch (Exception ex)
        {
            dumpText = "Exception occurred when calling Scene.GetUsedMaterialsDumpString:\r\n" + ex.Message;
        }

        return dumpText;
    }

    public string GetRenderingStepsDumpString()
    {
        if (SharpEngineSceneView == null)
            return "";

        string dumpText;

        try
        {
            dumpText = SharpEngineSceneView.SceneView.GetRenderingStepsInfo();
        }
        catch (Exception ex)
        {
            dumpText = "Exception occurred when calling SceneView.GetRenderingStepsDumpString:\r\n" + ex.Message;
        }

        return dumpText;
    }

    public string GetRenderingLayersDumpString()
    {
        if (SharpEngineSceneView == null)
            return "";

        string dumpText;

        try
        {
            dumpText = GetRenderingLayersInfo(SharpEngineSceneView.Scene);
        }
        catch (Exception ex)
        {
            dumpText = "Exception occurred when calling Scene.GetRenderingLayersInfo:\r\n" + ex.Message;
        }

        return dumpText;
    }

    private string GetRenderingLayersInfo(Scene scene)
    {
        return scene.GetRenderingLayersInfo(dumpEmptyRenderingLayers: false, showSortedValue: true, showNativeHandles: true);
    }

    public string GetMemoryDumpString()
    {
        if (SharpEngineSceneView == null)
            return "";

        string fullMemoryUsageDumpString;

        try
        {
            fullMemoryUsageDumpString = SharpEngineSceneView.Scene.GetFullMemoryUsageInfo(dumpAllActiveAllocations: true);
        }
        catch (Exception ex)
        {
            fullMemoryUsageDumpString = "Exception occurred when calling Scene.GetFullMemoryUsageDumpString:\r\n" + ex.Message;
        }

        return fullMemoryUsageDumpString;
    }

    public string GetResourcesDumpString(bool groupByType)
    {
        if (SharpEngineSceneView == null || SharpEngineSceneView.GpuDevice == null)
            return "";

        string reportText;
        if (groupByType)
        {
            reportText = "\r\nResources (classes derived from ComponentBase):\r\n" +
                         SharpEngineSceneView.GpuDevice.GetResourcesReportString(showFullTypeName: false, groupByTypeName: true, groupByIsDisposed: false);
        }
        else
        {
            reportText = "\r\nResources (classes derived from ComponentBase):\r\n" +
                         SharpEngineSceneView.GpuDevice.GetResourcesReportString(showFullTypeName: false, groupByTypeName: false, groupByIsDisposed: false);
        }

        return reportText;
    }

    public string GetResourcesForDelayedDisposalDumpString()
    {
        if (SharpEngineSceneView == null || SharpEngineSceneView.GpuDevice == null)
            return "";

        var reportText = SharpEngineSceneView.GpuDevice.GetResourcesForDelayedDisposalString();

        if (string.IsNullOrEmpty(reportText))
            reportText = "No resources scheduled to be disposed";
        else
            reportText = "Resources scheduled to be disposed:\r\n" + reportText;

        return reportText;
    }

    public string GetEngineSettingsDump(string indent = "")
    {
        if (SharpEngineSceneView == null)
            return "";

        var sb = new StringBuilder();
        
        if (SharpEngineSceneView.GpuDevice != null)
        {
            DumpObjectProperties(SharpEngineSceneView.GpuDevice.CreateOptions, sb, indent);
            sb.AppendLine();
            
            DumpObjectProperties(SharpEngineSceneView.GpuDevice, sb, indent);
            sb.AppendLine();
            
            DumpObjectProperties(SharpEngineSceneView.GpuDevice.PhysicalDeviceDetails, sb, indent);
            sb.AppendLine();

            sb.AppendLine($"{indent}Memory types:");
            var memoryTypes = SharpEngineSceneView.GpuDevice.PhysicalDeviceDetails.MemoryTypes;
            for (var i = 0; i < memoryTypes.Length; i++)
                sb.AppendLine($"{indent}  [{i}]: heap {memoryTypes[i].HeapIndex}: {memoryTypes[i].PropertyFlags}");
            sb.AppendLine();
            
            // Manually dump some properties of EngineRuntimeOptions because this class is static and have no instance properties
            sb.AppendLine($"{indent}EngineRuntimeOptions properties:");
            sb.AppendLine($"{indent}  InitialBufferMemoryBlockSize: {EngineRuntimeOptions.InitialBufferMemoryBlockSize}");
            sb.AppendLine($"{indent}  InitialImageMemoryBlockSize: {EngineRuntimeOptions.InitialImageMemoryBlockSize}");
            sb.AppendLine($"{indent}  MaxAllocatedMemoryBlockSize: {EngineRuntimeOptions.MaxAllocatedMemoryBlockSize}");
            sb.AppendLine($"{indent}  ReuseDisposedMemoryBlockIndexes: {EngineRuntimeOptions.ReuseDisposedMemoryBlockIndexes}");
            sb.AppendLine($"{indent}  MeshTriangleIndicesCountRequiredForComplexGeometry: {EngineRuntimeOptions.MeshTriangleIndicesCountRequiredForComplexGeometry}");
            sb.AppendLine($"{indent}  LinePositionsCountRequiredForComplexGeometry: {EngineRuntimeOptions.LinePositionsCountRequiredForComplexGeometry}");
            sb.AppendLine($"{indent}  FramesCountToReleaseEmptyMemoryBlock: {EngineRuntimeOptions.FramesCountToReleaseEmptyMemoryBlock}");
            sb.AppendLine();
        }

        DumpObjectProperties(SharpEngineSceneView.Scene, sb, indent);
        sb.AppendLine();

        DumpObjectProperties(SharpEngineSceneView.SceneView, sb, indent);
        sb.AppendLine();

        DumpObjectProperties(SharpEngineSceneView, sb, indent);
        sb.AppendLine();

        return sb.ToString();
    }

    private void DumpObjectProperties(object? objectToDump, StringBuilder sb, string indent)
    {
        if (objectToDump == null)
        {
            sb.AppendLine("null");
            return;
        }

        var type = objectToDump.GetType();

        sb.AppendLine($"{indent}{type.Name} properties:");

        try
        {
            var allProperties = type.GetProperties().OrderBy(p => p.Name).ToList();

            foreach (var propertyInfo in allProperties)
            {
                if (propertyInfo.PropertyType.IsValueType || 
                    propertyInfo.PropertyType == typeof(string) ||
                    (propertyInfo.PropertyType.Assembly.FullName != null &&
                     propertyInfo.PropertyType.Assembly.FullName.StartsWith("Ab4d."))) // Only show referenced objects for types that are declared in this class
                {
                    string valueText;

                    try
                    {
                        var propertyValue = propertyInfo.GetValue(objectToDump, null);

                        if (propertyValue == null)
                            valueText = "<null>";
                        else if (propertyInfo.PropertyType == typeof(string))
                            valueText = '"' + (string)propertyValue + '"';
                        else
                            valueText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", propertyValue);
                    }
                    catch (Exception e)
                    {
                        valueText = "ERROR: " + e.Message;
                    }

                    sb.AppendLine(indent + "  " + propertyInfo.Name + ": " + valueText);
                }
                else if (propertyInfo.PropertyType == typeof(string[]) || propertyInfo.PropertyType == typeof(List<string>))
                {
                    string valueText;

                    try
                    {
                        var propertyValue = propertyInfo.GetValue(objectToDump, null);

                        if (propertyValue != null)
                        {
                            if (propertyInfo.PropertyType == typeof(string[]))
                                valueText = string.Join(',', ((string[])propertyValue).Select(s => '"' + s + '"'));
                            else
                                valueText = string.Join(',', ((List<string>)propertyValue).Select(s => '"' + s + '"'));
                        }
                        else
                        {
                            valueText = "<null>";
                        }
                    }
                    catch (Exception e)
                    {
                        valueText = "ERROR: " + e.Message;
                    }

                    sb.AppendLine($"{indent}  {propertyInfo.Name}: {{{valueText}}}");
                }
            }
        }
        catch (Exception ex)
        {
            sb.Append(indent).Append("Error: ").AppendLine(ex.Message);
        }
    }

    public string GetSystemInfoDumpString()
    {
        if (SharpEngineSceneView == null || SharpEngineSceneView.GpuDevice == null)
            return "";

        string systemInfoText = GetSystemInfo(SharpEngineSceneView.GpuDevice);
        return systemInfoText;
    }

    private void AddObjectFields(StringBuilder sb, object objectToDump, bool sortFields)
    {
        var allFields = objectToDump.GetType().GetFields();

        if (sortFields)
            allFields = allFields.OrderBy(f => f.Name).ToArray();

        foreach (var fieldInfo in allFields)
        {
            if (fieldInfo.FieldType.IsValueType && !fieldInfo.FieldType.IsPublic) // skip fixed buffers because they cannot be displayed (by observing what are the properties of a fixed buffer I saw that they have IsValueType = true and IsPublic = false (private value type)
                continue;

            sb.AppendFormat("  {0}: ", fieldInfo.Name);

            var oneValue = fieldInfo.GetValue(objectToDump);

            if (oneValue == null)
            {
                sb.AppendFormat("<null>\r\n", oneValue);
            }
            else if (fieldInfo.FieldType.IsArray)
            {
                var array = (Array)oneValue;
                for (int i = 0; i < array.Length; i++)
                    sb.Append(array.GetValue(i)).Append(" ");

                sb.AppendLine();
            }
            else if (fieldInfo.FieldType == typeof(Bool32))
            {
                var bool32 = (Bool32)oneValue;
                sb.AppendFormat("{0}\r\n", bool32.Value == 1 ? "true" : "false");
            }
            else
            {
                sb.AppendFormat("{0}\r\n", oneValue);
            }
        }
    }

    public string GetSystemInfo(VulkanDevice? vulkanDevice)
    {
        if (vulkanDevice == null)
            return "VulkanDevice is null";

        var sb = new StringBuilder();

        try
        {
            var assembly = typeof(VulkanInstance).Assembly;
            var assemblyName = assembly.GetName();

            string versionText;
            if (assemblyName != null)
            {
                versionText = "v";

                // Try to get full version info with version suffix, for example "0.7.1-20211214.alpha1"
                var informationVersion = assembly.GetCustomAttributes(false).OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault();
                if (informationVersion != null)
                    versionText += informationVersion.InformationalVersion; // if this is not available then just show Version
                else if (assemblyName.Version != null)
                    versionText += assemblyName.Version.ToString();
                else
                    versionText += "<null>";
            }
            else
            {
                versionText = "";
            }

            sb.AppendLine($"Ab4d.SharpEngine {versionText} System Info:");
            sb.AppendFormat("DateTime: {0:ddd}, {0:yyyy-MM-dd} {0:hh:mm:ss}{1}", DateTime.Now, Environment.NewLine);
            sb.AppendFormat("Is64BitOS: {0}; Is64BitProcess: {1}; ProcessorCount: {2}; RuntimeIdentifier: {3}; OSDescription: {4}; OSArchitecture: {5}; FrameworkDescription: {6}; ProcessArchitecture: {7}",
                Environment.Is64BitOperatingSystem, Environment.Is64BitProcess, Environment.ProcessorCount,
                RuntimeInformation.RuntimeIdentifier, RuntimeInformation.OSDescription, RuntimeInformation.OSArchitecture, RuntimeInformation.FrameworkDescription, RuntimeInformation.ProcessArchitecture);
            sb.AppendLine();
            sb.AppendLine();


            sb.AppendLine("All graphics cards (PhysicalDevices):");

            var allPhysicalDeviceDetails = vulkanDevice.VulkanInstance.AllPhysicalDeviceDetails;
            for (var i = 0; i < allPhysicalDeviceDetails.Length; i++)
            {
                var physicalDeviceDetail = allPhysicalDeviceDetails[i];

                sb.Append($"{i}: {physicalDeviceDetail.DeviceName} ({physicalDeviceDetail.DeviceProperties.DeviceType}, DeviceId: {physicalDeviceDetail.DeviceProperties.DeviceID}, DeviceLUID: ");

                if (physicalDeviceDetail.IsDeviceLUIDValid)
                    sb.Append(physicalDeviceDetail.DeviceLUID);
                else
                    sb.Append("unknown");


                sb.Append(", DeviceUUID: ");

                if (physicalDeviceDetail.IsDeviceUUIDValid && physicalDeviceDetail.DeviceUUID != null)
                    sb.Append(string.Join("", physicalDeviceDetail.DeviceUUID.Select(n => n.ToString("x"))));
                else
                    sb.AppendLine("unknown");

                sb.AppendLine(")");
            }

            sb.AppendLine();
            sb.Append("Selected PhysicalDevice: ").AppendLine(vulkanDevice.PhysicalDeviceDetails.DeviceName);

            sb.AppendLine("PhysicalDeviceDetails.Features:");
            AddObjectFields(sb, vulkanDevice.PhysicalDeviceDetails.PossibleFeatures, sortFields: true);

            if (vulkanDevice.PhysicalDeviceDetails.IsLineRasterizationExtensionSupported)
            {
                sb.AppendLine("LineRasterizationFeatures:");
                AddObjectFields(sb, vulkanDevice.PhysicalDeviceDetails.PossibleLineRasterizationFeatures, sortFields: true);
            }
            else
            {
                sb.AppendLine("LineRasterizationFeatures: NOT SUPPORTED");
            }

            if (vulkanDevice.DefaultSurfaceDetails != null)
            {
                sb.AppendLine("\r\n\r\nDefaultSurfaceDetails.SurfaceCapabilities:");
                AddObjectFields(sb, vulkanDevice.DefaultSurfaceDetails.SurfaceCapabilities, sortFields: true);
            }

            sb.AppendLine("\r\n\r\nPhysicalDeviceDetails.DeviceProperties.Limits:");
            AddObjectFields(sb, vulkanDevice.PhysicalDeviceDetails.DeviceProperties.Limits, sortFields: true);

            // Now display DeviceProperties.limits that are defined as fixed arrays (see comments in PhysicalDeviceLimitsEx)
            AddObjectFields(sb, vulkanDevice.PhysicalDeviceDetails.PhysicalDeviceLimitsEx, sortFields: false);
        }
        catch (Exception ex)
        {
            sb.AppendLine("Error getting system info: \r\n" + ex.Message);
        }

        return sb.ToString();
    }

    public string GetCameraDetailsDumpString()
    {
        if (SharpEngineSceneView == null)
            return "";

        var cameraInfoDumpString = SharpEngineSceneView.SceneView.GetCameraInfo(showMatrices: true);
        return cameraInfoDumpString;
    }
    
    public void SaveRenderedSceneToDesktop()
    {
        if (SharpEngineSceneView == null || SharpEngineSceneView.GpuDevice == null)
            return;

        var renderedRawImageData = SharpEngineSceneView.SceneView.RenderToRawImageData(renderNewFrame: false);

        var folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var fileName = System.IO.Path.Combine(folder, "SharpEngineRenderedBitmap.png");

        SharpEngineSceneView.GpuDevice.DefaultBitmapIO.SaveBitmap(renderedRawImageData, fileName);
    }

    public string GetFullSceneDumpString()
    {
        if (SharpEngineSceneView == null)
            return "";


        var sb = new StringBuilder();

        if (SharpEngineSceneView.GpuDevice != null)
        {
            try
            {
                string systemInfoText;
                try
                {
                    systemInfoText = GetSystemInfo(SharpEngineSceneView.GpuDevice);
                }
                catch (Exception ex)
                {
                    systemInfoText = "Error getting system info: \r\n" + ex.Message;
                }

                sb.AppendLine("System info:").AppendLine(systemInfoText);
            }
            catch (Exception ex)
            {
                sb.AppendLine("Error writing system info:").AppendLine(ex.Message);
            }
        }


        try
        {
            var sharpEngineSettingsDump = GetEngineSettingsDump("  ");

            sb.AppendLine("Engine settings:").AppendLine(sharpEngineSettingsDump);


            string dumpText;
            try
            {
                dumpText = GetSceneNodesInfo(SharpEngineSceneView.Scene);
                sb.AppendLine("SceneNodes:").AppendLine(dumpText);
            }
            catch (Exception ex)
            {
                sb.AppendLine("Exception calling Scene.GetSceneNodesDumpString:").AppendLine(ex.Message);
            }


            string lightText = "";
            foreach (var light in SharpEngineSceneView.Scene.Lights)
                lightText += "  " + light.ToString() + Environment.NewLine;

            sb.AppendLine("Lights:").AppendLine(lightText);


            try
            {
                dumpText = GetRenderingLayersInfo(SharpEngineSceneView.Scene);
                sb.AppendLine("RenderingLayers:").AppendLine(dumpText);
            }
            catch (Exception ex)
            {
                sb.AppendLine("Exception occurred when calling Scene.GetRenderingLayersDumpString:").AppendLine(ex.Message);
            }


            var cameraInfoDumpString = SharpEngineSceneView.SceneView.GetCameraInfo(showMatrices: true);
            sb.AppendLine("Camera info:").AppendLine(cameraInfoDumpString);


            //var renderedToBitmap = SharpEngineSceneView.SceneView.RenderToBitmap(renderNewFrame: false);
            //string renderedBitmapBase64String = GetRenderedBitmapBase64String(renderedBitmap);

            //AppendDumpText("Rendered bitmap:", "<html><body>\r\n<img src=\"data:image/png;base64,\r\n" +
            //                                   renderedBitmapBase64String +
            //                                   "\" />\r\n</body></html>\r\n");
        }
        catch (Exception ex)
        {
            sb.AppendLine("Error writing scene dump:").AppendLine(ex.Message);
        }

        return sb.ToString();
    }

    public void ClearLogMessages()
    {
        if (NumberOfWarnings > 0)
        {
            LogMessages.Clear();
            NumberOfWarnings = 0;

            if (WarningsCountChangedAction != null)
                WarningsCountChangedAction(0);
        }

        if (NewLogMessageAddedAction != null)
            NewLogMessageAddedAction();
    }

    private void OnLogAction(LogLevels logLevel, string message)
    {
        LogMessages.Add(new Tuple<LogLevels, string>(logLevel, message));

        // We count number of warnings separately because _logMessages can be deleted
        if (logLevel >= LogLevels.Warn)
        {
            NumberOfWarnings++;

            if (WarningsCountChangedAction != null)
                WarningsCountChangedAction(NumberOfWarnings);
        }

        if (NewLogMessageAddedAction != null)
            NewLogMessageAddedAction();
    }

    private void OnDeviceInfoChanged()
    {
        DeviceInfoChangedAction?.Invoke();
    }

    private void OnShowMessageBox(string message)
    {
        ShowMessageBoxAction?.Invoke(message);
    }
}