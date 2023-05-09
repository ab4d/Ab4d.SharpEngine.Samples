using Ab4d.SharpEngine.Samples.Common.Cameras;
using Ab4d.SharpEngine.Samples.Common;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Ab4d.SharpEngine.Samples.Wpf.UIProvider;

namespace Ab4d.SharpEngine.Samples.Wpf.Cameras;

public class WpfPoint3DTo2DSample : Point3DTo2DSample
{
    private Canvas? _rootCanvas;
    private Rectangle? _infoRectangle;
    private Line? _targetLine;
    private TextBlock? _infoTextBlock;

    private Vector2 _infoRectOffset = new Vector2(130, 50);

    public WpfPoint3DTo2DSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnSphereScreenPositionChanged(Vector2 screenPosition)
    {
        if (_rootCanvas == null)
            return;

        PositionUIElements(screenPosition);
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateCustomUI(ICommonSampleUIProvider ui)
    {
        if (ui is not WpfUIProvider wpfUIProvider)
            return;

        var baseWpfPanel = wpfUIProvider.BaseWpfPanel;

        _rootCanvas = new Canvas();
        _rootCanvas.IsHitTestVisible = false;

        baseWpfPanel.Children.Add(_rootCanvas);
    }
    
    protected override void OnDisposed()
    {
        if (_rootCanvas == null)
            return;

        if (_infoRectangle != null)
            _rootCanvas.Children.Remove(_infoRectangle);

        if (_targetLine != null)
            _rootCanvas.Children.Remove(_targetLine);

        if (_infoTextBlock != null)
            _rootCanvas.Children.Remove(_infoTextBlock);

        base.OnDisposed();
    }

    [MemberNotNull(nameof(_infoRectangle))]
    [MemberNotNull(nameof(_targetLine))]
    [MemberNotNull(nameof(_infoTextBlock))]
    private void EnsurePositionUIElements()
    {
        if (_infoRectangle == null)
        {
            _infoRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                Width = 80,
                Height = 30,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
            };

            if (_rootCanvas != null)
                _rootCanvas.Children.Add(_infoRectangle);
        }

        if (_targetLine == null)
        {
            _targetLine = new Line()
            {
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = 0,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            if (_rootCanvas != null)
                _rootCanvas.Children.Add(_targetLine);
        }

        if (_infoTextBlock == null)
        {
            _infoTextBlock = new TextBlock();

            if (_rootCanvas != null)
                _rootCanvas.Children.Add(_infoTextBlock);
        }
    }

    private void PositionUIElements(Vector2 targetPosition)
    {
        EnsurePositionUIElements();

        Canvas.SetLeft(_infoRectangle, targetPosition.X + _infoRectOffset.X - _infoRectangle.Width / 2);
        Canvas.SetTop(_infoRectangle, targetPosition.Y - _infoRectOffset.Y - _infoRectangle.Height / 2);

        Canvas.SetLeft(_infoTextBlock, targetPosition.X + _infoRectOffset.X + 20 - _infoRectangle.Width / 2);
        Canvas.SetTop(_infoTextBlock, targetPosition.Y - _infoRectOffset.Y + 6 - _infoRectangle.Height / 2);

        _targetLine.X1 = targetPosition.X;
        _targetLine.Y1 = targetPosition.Y;
        _targetLine.X2 = targetPosition.X + _infoRectOffset.X + 1 - _infoRectangle.Width / 2;
        _targetLine.Y2 = targetPosition.Y - _infoRectOffset.Y - 1 + _infoRectangle.Height / 2;

        string positionText = $"{targetPosition.X:F0} {targetPosition.Y:F0}";
        _infoTextBlock.Text = positionText;
    }
}