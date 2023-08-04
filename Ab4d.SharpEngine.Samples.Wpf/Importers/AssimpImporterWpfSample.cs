using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Importers;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using System.Windows;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Wpf.Importers;

public class AssimpImporterWpfSample : AssimpImporterSample
{
    private DragAndDropHelper? _dragAndDropHelper;

    public AssimpImporterWpfSample(ICommonSamplesContext context)
        : base(context)
    {
        
    }

    // Drag and drop is platform specific function and needs to be implemented on per-platform sample
    protected override bool SetupDragAndDrop(ICommonSampleUIProvider ui)
    {
        if (context.CurrentSharpEngineSceneView == null || context.CurrentSharpEngineSceneView is not FrameworkElement rootFrameworkElement)
            return false; // no drag and drop not supported
         
        _dragAndDropHelper = new DragAndDropHelper(rootFrameworkElement, ".*");
        _dragAndDropHelper.FileDropped += OnDragAndDropHelperOnFileDropped;

        ui.CreateStackPanel(PositionTypes.Top | PositionTypes.Left, addBorder: false, isSemiTransparent: false).SetMargin(10, 65, 0, 0);
        ui.CreateLabel("Drag and drop file here to open it");

        return true; // drag and drop supported
    }

    void OnDragAndDropHelperOnFileDropped(object? sender, FileDroppedEventArgs args)
    {
        ImportFile(args.FileName);
    }

    protected override void OnDisposed()
    {
        if (_dragAndDropHelper != null)
        {
            _dragAndDropHelper.FileDropped -= OnDragAndDropHelperOnFileDropped;
            _dragAndDropHelper.Dispose();
        }

        base.OnDisposed();
    }
}