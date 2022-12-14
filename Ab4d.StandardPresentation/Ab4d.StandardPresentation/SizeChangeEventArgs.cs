using System;

namespace Ab4d
{
    public delegate void SizeChangeEventHandler(object sender, SizeChangeEventArgs e);

    public class SizeChangeEventArgs : EventArgs
    {
        public float Width { get; }
        public float Height { get; }

        public SizeChangeEventArgs(float width, float height)
        {
            Width  = width;
            Height = height;
        }
    }
}