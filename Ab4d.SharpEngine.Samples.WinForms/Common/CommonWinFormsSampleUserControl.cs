using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.WinForms.UIProvider;
using Ab4d.SharpEngine.WinForms;

namespace Ab4d.SharpEngine.Samples.WinForms.Common;

public class CommonWinFormsSampleUserControl : UserControl
{
    private CommonSample? _currentCommonSample;
    private CommonSample? _lastInitializedSample;
    private PointerCameraController? _pointerCameraController;
    private InputEventsManager _inputEventsManager;

    private bool _isLoaded;

    private WinFormsUIProvider _winFormsUIProvider;

    public SharpEngineSceneView MainSceneView;
    public Label TitleLabel;
    public Label SubtitleLabel;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CommonSample? CurrentCommonSample
    {
        get => _currentCommonSample;
        set
        {
            _currentCommonSample = value;

            if (_isLoaded)
                InitializeCommonSample();
        }
    }

    public CommonWinFormsSampleUserControl()
    {
        TitleLabel = new Label();
        TitleLabel.Font = new Font(TitleLabel.Font.FontFamily, 14, FontStyle.Bold);
        TitleLabel.AutoSize = true;
        TitleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        TitleLabel.Location = new Point(10, 10);
        this.Controls.Add(TitleLabel);


        SubtitleLabel = new Label();
        SubtitleLabel.Font = new Font(TitleLabel.Font.FontFamily, 10, FontStyle.Regular);
        SubtitleLabel.AutoSize = true;
        SubtitleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        SubtitleLabel.Location = new Point(13, 43);
        this.Controls.Add(SubtitleLabel);


        // IMPORTANT:
        // We need to add SharpEngineSceneView to Controls after other controls were added,
        // otherwise other controls would not be visible on top of the MainSceneView.
        MainSceneView = new SharpEngineSceneView(PresentationTypes.OverlayTexture);
        MainSceneView.Dock = DockStyle.Fill;

        // By default, enable Vulkan's standard validation (this may slightly reduce performance)
        MainSceneView.CreateOptions.EnableStandardValidation = true;

        // Logging was already enabled in SamplesWindow constructor
        //Utilities.Log.LogLevel = LogLevels.Warn;
        //Utilities.Log.IsLoggingToDebugOutput = true;

        this.Controls.Add(MainSceneView);


        _winFormsUIProvider = new WinFormsUIProvider(this, MainSceneView);

        MainSceneView.GpuDeviceCreated += MainSceneViewOnGpuDeviceCreated;

        // In case when VulkanDevice cannot be created, show an error message
        // If this is not handled by the user, then SharpEngineSceneView will show its own error message
        MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
        {
            MessageBox.Show("Vulkan device creation error:\r\n" + args.Exception.Message); // Show error message
            args.IsHandled = true;  // Prevent showing error by SharpEngineSceneView
        };

        _inputEventsManager = new InputEventsManager(MainSceneView);


        this.SizeChanged += OnSizeChanged;

        this.HandleDestroyed += OnHandleDestroyed;
    }

    private void OnHandleDestroyed(object? sender, EventArgs e)
    {
        if (!MainSceneView.IsDisposed)
            MainSceneView.Dispose();
    }


    /// <inheritdoc />
    protected override void OnLoad(EventArgs e)
    {
        InitializeCommonSample();

        if (_pointerCameraController == null) // if _pointerCameraController is not null, then InitializePointerCameraController was already called from InitializeCommonSample
        {
            _pointerCameraController ??= new PointerCameraController(MainSceneView, eventsSourceElement: MainSceneView);

            if (_currentCommonSample != null)
                _currentCommonSample.InitializePointerCameraController(_pointerCameraController);
        }

        _isLoaded = true;

        base.OnLoad(e);
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        TitleLabel.MaximumSize = new Size(this.ClientSize.Width - 10, this.ClientSize.Height);
        SubtitleLabel.MaximumSize = new Size(this.ClientSize.Width - 10, this.ClientSize.Height);
    }

    private void ResetSample()
    {
        TitleLabel.Text = null;
        SubtitleLabel.Text = null;

        this.SuspendLayout();

        for (var i = this.Controls.Count - 1; i >= 0; i--)
        {
            var oneControl = this.Controls[i];
            if (oneControl != MainSceneView && oneControl != TitleLabel && oneControl != SubtitleLabel)
                this.Controls.RemoveAt(i);
        }

        this.ResumeLayout();


        MainSceneView.Scene.RootNode.Clear();
        MainSceneView.Scene.Lights.Clear();
        MainSceneView.Visible = false;

        _lastInitializedSample = null;
    }

    private void InitializeCommonSample()
    {
        if (_lastInitializedSample == _currentCommonSample)
            return; // already initialized

        ResetSample();

        if (_currentCommonSample == null)
            return;

        _currentCommonSample.InitializeSharpEngineView(MainSceneView);

        _currentCommonSample.InitializeInputEventsManager(_inputEventsManager);

        // Prevent updating the control while recreating sample controls
        SuspendDrawing(this);
        _currentCommonSample.CreateUI(_winFormsUIProvider);
        ResumeDrawing(this);
        
        // Set Title and Subtitle after initializing UI, because they can be changed there
        TitleLabel.Text = _currentCommonSample.Title;
        SubtitleLabel.Text = _currentCommonSample.Subtitle;

        //MainSceneView.Scene.SetCoordinateSystem(CoordinateSystems.ZUpRightHanded);

        if (_pointerCameraController != null)
            _currentCommonSample.InitializePointerCameraController(_pointerCameraController);

        // Show MainSceneView - this will also render the scene
        MainSceneView.Visible = true;

        _lastInitializedSample = _currentCommonSample;
    }

    private void MainSceneViewOnGpuDeviceCreated(object sender, GpuDeviceCreatedEventArgs e)
    {

    }


    // I was not able to use SuspendLayout and ResumeLayout to nicely re-render the controls for new sample.
    // Then I found the solution here: https://stackoverflow.com/questions/487661/how-do-i-suspend-painting-for-a-control-and-its-children
    // If you have a nicer solution without PInvoice, please provide a PR.
    
    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

    private const int WM_SETREDRAW = 11; 
    
    public static void SuspendDrawing( Control parent )
    {
        SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
    }

    public static void ResumeDrawing( Control parent )
    {
        SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
        parent.Refresh();
    }
}