using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Maui;

public interface IPlatformInputHelper
{
    bool IsCurrentPointerButtonAvailable { get; }
    bool IsCurrentKeyboardModifierAvailable { get; }

    PointerButtons GetCurrentPointerButtons();
    KeyboardModifiers GetCurrentKeyboardModifiers();
}