using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Animations;
using Ab4d.SharpEngine.Samples.WinForms.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.WinForms;

namespace Ab4d.SharpEngine.Samples.WinForms.Titles;

public class IntroductionUserControl : UserControl
{
    private bool PlayAnimationOnStartup = true;       // Set to false to prevent automatically playing the animation
    private bool SkipInitializingSharpEngine = false; // When true, then no SharpEngine object will be created (only Avalonia objects will be shown)
        
    private SharpEngineLogoAnimation? _sharpEngineLogoAnimation;

    public SharpEngineSceneView? MainSceneView;
    public Label InfoLabel;
    public Button? PlayAgainButton;
    private Panel _infoPanel;
    private Panel _sceneViewPanel;

    public IntroductionUserControl()
    {
        this.SuspendLayout();

        this.Dock = DockStyle.Fill;
        
        PlayAgainButton = new Button
        {
            Text = "Play again",
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Visible = false
        };
        PlayAgainButton.Font = new Font(PlayAgainButton.Font.FontFamily, 10);
        PlayAgainButton.Click += PlayAgainButtonOnClick;

        this.Controls.Add(PlayAgainButton);


        _infoPanel = new Panel()
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            BackColor = Color.White
        };

        _sceneViewPanel = new Panel()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        this.Controls.Add(_sceneViewPanel);
        this.Controls.Add(_infoPanel);
        

        InfoLabel = new Label
        {
            Text = "Ab4d.SharpEngine is a blazing fast and cross platform\n3D rendering engine for desktop and mobile .Net applications.",
            TextAlign = ContentAlignment.TopCenter,
            //AutoSize = true,
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Visible = false,
        };
        InfoLabel.Font = new Font(InfoLabel.Font.FontFamily, 16);

        _infoPanel.Controls.Add(InfoLabel);


        if (SkipInitializingSharpEngine)
        {
            ShowStaticSharpEngineLogo();
            ShowInfoTextBlock();

            this.ResumeLayout();

            return;
        }


        // IMPORTANT:
        // We need to add SharpEngineSceneView to Controls after other controls were added,
        // otherwise other controls would not be visible on top of the MainSceneView.
        MainSceneView = new SharpEngineSceneView(PresentationTypes.OverlayTexture)
        {
            BackColor = Color.White,
            Dock = DockStyle.Fill,
        };



#if DEBUG
        // Enable standard validation that provides additional error information when Vulkan SDK is installed on the system.
        MainSceneView.CreateOptions.EnableStandardValidation = true; // Set to false to load Vulkan faster
#endif

        // Logging was already enabled in SamplesWindow constructor
        //Utilities.Log.LogLevel = LogLevels.Warn;
        //Utilities.Log.IsLoggingToDebugOutput = true;

        _sceneViewPanel.Controls.Add(MainSceneView);

        this.ResumeLayout();


        var scene = MainSceneView.Scene;
        var sceneView = MainSceneView.SceneView;

        _sharpEngineLogoAnimation = new SharpEngineLogoAnimation(scene, bitmapIO: null);

        scene.RootNode.Add(_sharpEngineLogoAnimation.LogoPlaneModel);
        scene.RootNode.Add(_sharpEngineLogoAnimation.HashModel);


        var targetPositionCamera = new TargetPositionCamera();
        _sharpEngineLogoAnimation.ResetCamera(targetPositionCamera);
        _sharpEngineLogoAnimation.SetupLights(scene);

        sceneView.Camera = targetPositionCamera;

        MainSceneView.SceneUpdating += MainSceneViewOnSceneUpdating;

        _sharpEngineLogoAnimation.AnimationCompleted += delegate (object? sender2, EventArgs args2)
        {
            ShowInfoTextBlock();
            ShowPlayAgainButton();
        };


        if (PlayAnimationOnStartup)
        {
            // Start with going to first frame
            _sharpEngineLogoAnimation.GotoFirstFrame();

            // Now wait until Application is Idle and then start the animation
            Application.Idle += ApplicationOnIdle;
        }
        else
        {
            _sharpEngineLogoAnimation.GotoLastFrame();

            PlayAgainButton.Text = "Play animation"; // replace "Play again" because animation was not yet played
            ShowPlayAgainButton();
        }
    }

    private void ApplicationOnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= ApplicationOnIdle;

        _sharpEngineLogoAnimation?.StartAnimation();
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        _sharpEngineLogoAnimation?.Dispose();
        MainSceneView?.Dispose();

        base.OnHandleDestroyed(e);
    }

    private void MainSceneViewOnSceneUpdating(object? sender, EventArgs e)
    {
        _sharpEngineLogoAnimation?.UpdateAnimation();
    }

    private void ShowStaticSharpEngineLogo()
    {
        string fileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\Textures\sharp-engine-logo.png";

        var pictureBox = new PictureBox()
        {
            Image = Image.FromFile(fileName),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill
        };
        
        _sceneViewPanel.Controls.Add(pictureBox);
    }

    private void ShowPlayAgainButton()
    {
        if (PlayAgainButton == null)
            return;

        PlayAgainButton.Location = new Point(this.ClientSize.Width - PlayAgainButton.PreferredSize.Width - 10,
                                             this.ClientSize.Height - PlayAgainButton.PreferredSize.Height - 10);

        PlayAgainButton.Visible = true;
    }
    
    private void ShowInfoTextBlock()
    {
        InfoLabel.Location = new Point((_infoPanel.ClientSize.Width - InfoLabel.PreferredSize.Width) / 2,
                                        _infoPanel.ClientSize.Height - InfoLabel.PreferredSize.Height - 10);

        InfoLabel.Visible = true;
    }


    private void PlayAgainButtonOnClick(object? sender, EventArgs e)
    {
        InfoLabel.Visible = false;
        PlayAgainButton!.Visible = false;

        _sharpEngineLogoAnimation?.StartAnimation();
    }
}