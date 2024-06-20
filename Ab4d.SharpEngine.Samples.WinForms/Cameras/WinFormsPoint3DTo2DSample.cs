using Ab4d.SharpEngine.Samples.Common.Cameras;
using Ab4d.SharpEngine.Samples.Common;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.WinForms.UIProvider;

namespace Ab4d.SharpEngine.Samples.WinForms.Cameras;

public class WinFormsPoint3DTo2DSample : Point3DTo2DSample
{
    private Label? _infoLabel;

    private Point _infoRectOffset = new Point(15, 20);

    private float _dpiScale = 1;

    private UserControl? _baseWinFormsPanel;

    public WinFormsPoint3DTo2DSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnSphereScreenPositionChanged(Vector2 screenPosition)
    {
        if (_baseWinFormsPanel == null)
            return;

        PositionUIElements(screenPosition);
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateCustomUI(ICommonSampleUIProvider ui)
    {
        if (ui is not WinFormsUIProvider winFormsUIProvider)
            return;

        _baseWinFormsPanel = winFormsUIProvider.BaseWinFormsPanel;
        _dpiScale = winFormsUIProvider.DpiScale;
    }

    protected override void OnDisposed()
    {
        if (_baseWinFormsPanel == null)
            return;

        if (_infoLabel != null)
            _baseWinFormsPanel.Controls.Remove(_infoLabel);

        base.OnDisposed();
    }

    [MemberNotNull(nameof(_infoLabel))]
    private void EnsurePositionUIElements()
    {
        if (_infoLabel == null)
        {
            _infoLabel = new Label()
            {
                AutoSize = true,
                BackColor = Color.LightBlue
            };

            if (_baseWinFormsPanel != null)
            {
                _baseWinFormsPanel.Controls.Add(_infoLabel);
                _baseWinFormsPanel.Controls.SetChildIndex(_infoLabel, 0);
            }
        }
    }

    private void PositionUIElements(Vector2 targetPosition)
    {
        EnsurePositionUIElements();

        string positionText = $"{targetPosition.X:F0} {targetPosition.Y:F0}";
        _infoLabel.Text = positionText;

        int x = (int)(targetPosition.X * _dpiScale);
        int y = (int)(targetPosition.Y * _dpiScale);

        _infoLabel.Location = new Point(x + _infoRectOffset.X + 20 - _infoLabel.PreferredWidth / 2, 
                                        y - _infoRectOffset.Y + 6 - _infoLabel.PreferredHeight / 2);
    }
}