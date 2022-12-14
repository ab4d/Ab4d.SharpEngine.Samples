namespace Ab4d
{
    public delegate void MouseWheelEventHandler(object sender, MouseWheelEventArgs e);

    public class MouseWheelEventArgs : MouseEventArgs
    {
        public float DeltaX { get; }
        public float DeltaY { get; }

        public MouseWheelEventArgs(float deltaX, float deltaY, long timeStamp = 0) 
            : base(timeStamp)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }
    }
}