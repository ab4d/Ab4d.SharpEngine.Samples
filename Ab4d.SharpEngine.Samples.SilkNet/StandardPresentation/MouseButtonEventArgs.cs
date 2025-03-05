using System;
using Ab4d.SharpEngine.Common;

namespace Ab4d.StandardPresentation
{
    public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);

    public class MouseButtonEventArgs : MouseEventArgs
    {
        /// <summary>Gets the button associated with the event.</summary>
        /// <returns>The button which was pressed.</returns>
        public PointerButtons ChangedButton { get; }

        public MouseButtonEventArgs(PointerButtons changedButton, long timeStamp = 0) 
            : base(timeStamp)
        { 
            ChangedButton = changedButton;
        }
    }
}