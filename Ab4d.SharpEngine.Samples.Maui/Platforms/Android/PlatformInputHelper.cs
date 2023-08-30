using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Maui;

public class PlatformInputHelper : IPlatformInputHelper
{
    public bool IsCurrentMouseButtonAvailable => false;
    public bool IsCurrentKeyboardModifierAvailable => false;

    public MouseButtons GetCurrentMouseButtons()
    {
        throw new NotSupportedException();
    }

    public KeyboardModifiers GetCurrentKeyboardModifiers()
    {
        throw new NotSupportedException();
    }
}