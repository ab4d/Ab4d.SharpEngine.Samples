using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Maui;

public class PlatformInputHelper : IPlatformInputHelper
{
    public bool IsCurrentPointerButtonAvailable => false;
    public bool IsCurrentKeyboardModifierAvailable => false;

    public PointerButtons GetCurrentPointerButtons()
    {
        throw new NotSupportedException();
    }

    public KeyboardModifiers GetCurrentKeyboardModifiers()
    {
        throw new NotSupportedException();
    }
}