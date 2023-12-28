using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Ab4d.SharpEngine.Samples.Wpf.UIProvider;
using Colors = System.Windows.Media.Colors;

namespace Ab4d.SharpEngine.Samples.Wpf.HitTesting;

// This class processes WPF mouse events and routes them to the methods in the common ManualInputEventsSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class WpfManualInputEventsSample : ManualInputEventsSample
{
    private TextBox? _infoTextBox;
    private Border? _rootBorder;
    private Panel? _baseWpfPanel;

    private UIElement? _subscribedUIElement;

    public WpfManualInputEventsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        eventsSourceElement.MouseDown += EventsSourceElementOnMouseDown;
        eventsSourceElement.MouseUp += EventsSourceElementOnMouseUp;
        eventsSourceElement.MouseMove += EventsSourceElementOnMouseMove;

        _subscribedUIElement = eventsSourceElement;
    }

    private void UnSubscribeMouseEvents()
    {
        if (_subscribedUIElement == null)
            return;

        _subscribedUIElement.MouseDown -= EventsSourceElementOnMouseDown;
        _subscribedUIElement.MouseUp -= EventsSourceElementOnMouseUp;
        _subscribedUIElement.MouseMove -= EventsSourceElementOnMouseMove;

        _subscribedUIElement = null;
    }

    private void EventsSourceElementOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // PointerPressed is called not only when the mouse button is pressed, but all the time until the button is pressed
        // But we would only like ot know when the left mouse button is pressed
        if (isLeftMouseButtonPressed || _subscribedUIElement == null)
            return;

        var currentPoint = e.GetPosition(_subscribedUIElement);
        isLeftMouseButtonPressed = e.LeftButton == MouseButtonState.Pressed;

        if (isLeftMouseButtonPressed)
        {
            var mousePosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
            ProcessMouseButtonPress(mousePosition);
        }
    }

    private void EventsSourceElementOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!isLeftMouseButtonPressed || _subscribedUIElement == null) // is already released
            return;

        var currentPoint = e.GetPosition(_subscribedUIElement);
        isLeftMouseButtonPressed = e.LeftButton == MouseButtonState.Pressed;

        if (!isLeftMouseButtonPressed)
        {
            var mousePosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
            ProcessMouseButtonRelease(mousePosition);
        }
    }

    private void EventsSourceElementOnMouseMove(object sender, MouseEventArgs e)
    {
        if (_subscribedUIElement == null)
            return;

        var currentPoint = e.GetPosition(_subscribedUIElement);
        {
            var mousePosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
            ProcessMouseMove(mousePosition);
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
        if (ui is not WpfUIProvider wpfUIProvider)
            return;

        _rootBorder = new Border()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 260,
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
            Margin = new Thickness(5, 5, 0, 5)
        };

        _baseWpfPanel = wpfUIProvider.BaseWpfPanel;

        if (_baseWpfPanel != null)
            _baseWpfPanel.Children.Add(_rootBorder);

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

        checkBox1.Checked += OnMouseDraggingCheckBoxCheckedChanged;
        checkBox1.Unchecked += OnMouseDraggingCheckBoxCheckedChanged;

        stackPanel.Children.Add(checkBox1);


        var checkBox2 = new CheckBox()
        {
            Content = "Check object collision",
            IsChecked = true
        };

        checkBox2.Checked += OnCheckCollisionCheckBoxCheckedChanged;
        checkBox2.Unchecked += OnCheckCollisionCheckBoxCheckedChanged;

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

        if (_baseWpfPanel != null && _rootBorder != null)
            _baseWpfPanel.Children.Remove(_rootBorder);

        base.OnDisposed();
    }

    private void OnMouseDraggingCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        isMouseDraggingEnabled = checkBox.IsChecked ?? false;
    }

    private void OnCheckCollisionCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        isCollisionDetectionEnabled = checkBox.IsChecked ?? false;
    }
}