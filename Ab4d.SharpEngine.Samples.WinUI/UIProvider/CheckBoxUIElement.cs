using Ab4d.SharpEngine.Samples.Common;
using System;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class CheckBoxUIElement : WinUIElement
{
    private CheckBox _checkBox;

    private Action<bool> _checkedChangedAction;

    public CheckBoxUIElement(WinUIProvider winUIProvider, string text, bool isInitiallyChecked, Action<bool> checkedChangedAction)
        : base(winUIProvider)
    {
        _checkedChangedAction = checkedChangedAction;

        var (textToShow, toolTip) = winUIProvider.ParseTextAndToolTip(text);

        _checkBox = new CheckBox()
        {
            Content = textToShow,
            FontSize = winUIProvider.FontSize,
        };

        if (isInitiallyChecked)
            _checkBox.IsChecked = true;

        if (toolTip != null)
            ToolTipService.SetToolTip(_checkBox, toolTip);

        _checkBox.Checked   += (sender, args) => _checkedChangedAction?.Invoke(true);
        _checkBox.Unchecked += (sender, args) => _checkedChangedAction?.Invoke(false);

        Element = _checkBox;
    }

    public override string? GetText() => _checkBox.Content as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _checkBox.Content = text;
        return this;
    }

    protected override void OnSetColor(Color wpfColor)
    {
        // The following does not work in WinUI !!!
        //_checkBox.Foreground = new SolidColorBrush(wpfColor);

        // A workaround from (https://github.com/microsoft/microsoft-ui-xaml/issues/9236)
        // was to find child TextBlock, but this also does not work!!!
        //var tb = FindChildElement<TextBlock>(_checkBox);
        //tb.Foreground = new SolidColorBrush(wpfColor);
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is not bool isChecked)
            throw new ArgumentException($"SetValue for CheckBox expects bool value, but got {newValue?.GetType().Name}");

        _checkBox.IsChecked = isChecked;
    }

    //ChildType? FindChildElement<ChildType>(Microsoft.UI.Xaml.DependencyObject tree) where ChildType : Microsoft.UI.Xaml.DependencyObject
    //{
    //    for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(tree); i++)
    //    {
    //        Microsoft.UI.Xaml.DependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(tree, i);
    //        if (child != null && child is ChildType)
    //        {
    //            return child as ChildType;
    //        }
    //        else
    //        {
    //            ChildType childInSubtree = FindChildElement<ChildType>(child);
    //            if (childInSubtree != null)
    //            {
    //                return childInSubtree;
    //            }
    //        }
    //    }
    //    return null;
    //}
}