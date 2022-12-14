using System;

namespace Ab4d
{
    public delegate void MouseEventHandler(object sender, MouseEventArgs e);

    public class MouseEventArgs : EventArgs
    {
        public long TimeStamp { get; }

        public MouseEventArgs(long timeStamp = 0)
        {
            if (timeStamp == 0)
                timeStamp = DateTime.Now.Ticks;

            this.TimeStamp = timeStamp;
        }
    }
}