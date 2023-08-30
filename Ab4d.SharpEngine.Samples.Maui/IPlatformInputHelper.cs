using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Maui;

public interface IPlatformInputHelper
{
    bool IsCurrentMouseButtonAvailable { get; }
    bool IsCurrentKeyboardModifierAvailable { get; }

    MouseButtons GetCurrentMouseButtons();
    KeyboardModifiers GetCurrentKeyboardModifiers();
}