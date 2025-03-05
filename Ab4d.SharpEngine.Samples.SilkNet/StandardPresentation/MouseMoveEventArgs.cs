namespace Ab4d.StandardPresentation
{
    public delegate void MouseMoveEventHandler(object sender, MouseMoveEventArgs e);

    public class MouseMoveEventArgs : MouseEventArgs
    {
        public float X { get; }
        public float Y { get; }

        public MouseMoveEventArgs(float x, float y, long timeStamp = 0) 
            : base(timeStamp)
        {
            X = x;
            Y = y;
        }
    }
}