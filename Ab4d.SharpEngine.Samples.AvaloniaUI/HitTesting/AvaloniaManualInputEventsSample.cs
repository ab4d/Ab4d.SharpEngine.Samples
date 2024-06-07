using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.HitTesting;

// This class processes Avalonia mouse events and routes them to the methods in the common ManualInputEventsSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class AvaloniaManualInputEventsSample : ManualInputEventsSample
{
    private TextBox? _infoTextBox;
    private Border? _rootBorder;
    private Panel? _baseAvaloniaPanel;

    private InputElement? _subscribedElement;

    public AvaloniaManualInputEventsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        // Because we render a gradient in background RootBorder and we have set MainSceneView.IsHitTestVisible to false
        // we need to subscribe to parent Border control instead of to sharpEngineSceneView.
        if (sharpEngineSceneView is not Control avaloniaControl)
            return;

        var parentBorder = avaloniaControl.Parent as Border;
        if (parentBorder == null)
            return;

        parentBorder.PointerPressed += EventsSourceElementOnPointerPressed;
        parentBorder.PointerReleased += EventsSourceElementOnPointerReleased;
        parentBorder.PointerMoved += EventsSourceElementOnPointerMoved;

        _subscribedElement = parentBorder;
    }

    private void UnSubscribeMouseEvents()
    {
        if (_subscribedElement == null)
            return;

        _subscribedElement.PointerPressed -= EventsSourceElementOnPointerPressed;
        _subscribedElement.PointerReleased -= EventsSourceElementOnPointerReleased;
        _subscribedElement.PointerMoved -= EventsSourceElementOnPointerMoved;

        _subscribedElement = null;
    }

    private void EventsSourceElementOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // PointerPressed is called not only when the pointer button is pressed, but all the time until the button is pressed
        // But we would only like to know when the left pointer button is pressed
        if (isLeftPointerButtonPressed || _subscribedElement == null)
            return;

        var currentPoint = e.GetCurrentPoint(_subscribedElement);
        isLeftPointerButtonPressed = currentPoint.Properties.IsLeftButtonPressed;

        if (isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessLeftPointerButtonPressed(pointerPosition);
        }
    }

    private void EventsSourceElementOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isLeftPointerButtonPressed || _subscribedElement == null) // is already released
            return;

        var currentPoint = e.GetCurrentPoint(_subscribedElement);
        isLeftPointerButtonPressed = currentPoint.Properties.IsLeftButtonPressed;

        if (!isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessLeftPointerButtonReleased(pointerPosition);
        }
    }

    private void EventsSourceElementOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_subscribedElement == null)
            return;

        var currentPoint = e.GetCurrentPoint(_subscribedElement);
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessPointerMoved(pointerPosition);
        }
    }

    protected override void ShowMessage(string message)
    {
        if (_infoTextBox == null)
            return;

        var oldMessages = _infoTextBox.Text;
        if (oldMessages != null && oldMessages.Length > 2000)
            oldMessages = oldMessages.Substring(0, 2000); // prevent showing very large text

        _infoTextBox.Text = message + System.Environment.NewLine + oldMessages;
    }

    // This sample creates custom UI because we need a Grid with custom rows to show the InfoTextBox
    protected override void CreateCustomUI(ICommonSampleUIProvider ui)
    {
        if (ui is not AvaloniaUIProvider avaloniaUIProvider)
            return;

        _rootBorder = new Border()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 260,
            BorderThickness = new Thickness(2),
            BorderBrush = Brushes.Black,
            Background = new SolidColorBrush(Avalonia.Media.Colors.White) { Opacity = 0.8 },
            Margin = new Thickness(5, 5, 0, 5)
        };

        _baseAvaloniaPanel = avaloniaUIProvider.BaseAvaloniaPanel;

        if (_baseAvaloniaPanel != null)
            _baseAvaloniaPanel.Children.Add(_rootBorder);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

        _rootBorder.Child = grid;


        var stackPanel = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(5, 0, 0, 10)
        };


        var checkBox1 = new CheckBox()
        {
            Content = "Mouse dragging",
            IsChecked = true
        };

        checkBox1.IsCheckedChanged += OnMouseDraggingCheckBoxCheckedChanged;

        stackPanel.Children.Add(checkBox1);


        var checkBox2 = new CheckBox()
        {
            Content = "Check object collision",
            IsChecked = true
        };

        checkBox2.IsCheckedChanged += OnCheckCollisionCheckBoxCheckedChanged;

        stackPanel.Children.Add(checkBox2);


        _infoTextBox = new TextBox()
        {
            Width = 250,
            FontSize = 11,
            FontFamily = new FontFamily("Consolas"),
            AcceptsReturn = true,
        };

        ScrollViewer.SetVerticalScrollBarVisibility(_infoTextBox, ScrollBarVisibility.Auto);


        Grid.SetRow(stackPanel, 0);
        grid.Children.Add(stackPanel);

        Grid.SetRow(_infoTextBox, 1);
        grid.Children.Add(_infoTextBox);
    }

    protected override void OnDisposed()
    {
        UnSubscribeMouseEvents();

        if (_baseAvaloniaPanel != null && _rootBorder != null)
            _baseAvaloniaPanel.Children.Remove(_rootBorder);

        base.OnDisposed();
    }

    private void OnMouseDraggingCheckBoxCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox) 
            isPointerDraggingEnabled = checkBox.IsChecked ?? false;
    }

    private void OnCheckCollisionCheckBoxCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox) 
            isCollisionDetectionEnabled = checkBox.IsChecked ?? false;
    }
}