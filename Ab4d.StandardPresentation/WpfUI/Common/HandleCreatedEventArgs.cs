using System;

namespace Ab4d.StandardPresentation.WpfUI.Common
{
    /// <summary>
    /// HandleCreatedEventHandler is a delegate for the <see cref="HandleCreatedEventArgs"/>.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void HandleCreatedEventHandler(object sender, HandleCreatedEventArgs e);

    /// <summary>
    /// HandleCreatedEventArgs is used for the <see cref="D3DHost.HandleCreated"/> event.
    /// </summary>
    internal class HandleCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// hwmd handle
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handle">hwmd handle</param>
        public HandleCreatedEventArgs(IntPtr handle)
        {
            this.Handle = handle;
        }
    }
}
