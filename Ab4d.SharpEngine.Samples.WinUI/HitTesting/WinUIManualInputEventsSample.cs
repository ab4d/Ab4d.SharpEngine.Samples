using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.HitTesting;
using Ab4d.SharpEngine.Samples.WinUI.UIProvider;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Ab4d.SharpEngine.Samples.WinUI.HitTesting;

// This class processes WinUI mouse events and routes them to the methods in the common ManualInputEventsSample that is defined in the Ab4d.SharpEngine.Samples.Common project.

public class WinUIManualInputEventsSample : ManualInputEventsSample
{
    private TextBox? _infoTextBox;
    private Border? _rootBorder;
    private Panel? _baseWinUIPanel;

    private UIElement? _subscribedUIElement;

    public WinUIManualInputEventsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView)
    {
        if (sharpEngineSceneView is not UIElement eventsSourceElement)
            return;

        eventsSourceElement.PointerPressed += OnEventsSourceElementOnPointerPressed;
        eventsSourceElement.PointerReleased += OnEventsSourceElementOnPointerReleased;
        eventsSourceElement.PointerMoved += OnEventsSourceElementOnPointerMoved;

        _subscribedUIElement = eventsSourceElement;
    }

    private void UnSubscribeMouseEvents()
    {
        if (_subscribedUIElement == null)
            return;

        _subscribedUIElement.PointerPressed -= OnEventsSourceElementOnPointerPressed;
        _subscribedUIElement.PointerReleased -= OnEventsSourceElementOnPointerReleased;
        _subscribedUIElement.PointerMoved -= OnEventsSourceElementOnPointerMoved;

        _subscribedUIElement = null;
    }

    private void OnEventsSourceElementOnPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        // PointerPressed is called not only when the mouse button is pressed, but all the time until the button is pressed
        // But we would only like ot know when the left mouse button is pressed
        if (isLeftPointerButtonPressed || _subscribedUIElement == null)
            return;

        var currentPoint = args.GetCurrentPoint(_subscribedUIElement);
        isLeftPointerButtonPressed = currentPoint.Properties.IsLeftButtonPressed;

        if (isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessLeftPointerButtonPressed(pointerPosition);
        }
    }

    private void OnEventsSourceElementOnPointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (!isLeftPointerButtonPressed || _subscribedUIElement == null) // is already released
            return;

        var currentPoint = args.GetCurrentPoint(_subscribedUIElement);
        isLeftPointerButtonPressed = currentPoint.Properties.IsLeftButtonPressed;

        if (!isLeftPointerButtonPressed)
        {
            var pointerPosition = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            ProcessLeftPointerButtonReleased(pointerPosition);
        }
    }

    private void OnEventsSourceElementOnPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (_subscribedUIElement == null)
            return;

        var currentPoint = args.GetCurrentPoint(_subscribedUIElement);
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
        if (ui is not WinUIProvider winUIProvider)
            return;

        _rootBorder = new Border()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 260,
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black),
            Background = new SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = 0.8 },
            Margin = new Thickness(5, 5, 0, 5)
        };

        _baseWinUIPanel = winUIProvider.BaseWinUIPanel;
        _baseWinUIPanel.Children.Add(_rootBorder);

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

        if (_baseWinUIPanel != null && _rootBorder != null)
            _baseWinUIPanel.Children.Remove(_rootBorder);

        base.OnDisposed();
    }

    private void OnMouseDraggingCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        isPointerDraggingEnabled = checkBox.IsChecked ?? false;
    }

    private void OnCheckCollisionCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        isCollisionDetectionEnabled = checkBox.IsChecked ?? false;
    }
}