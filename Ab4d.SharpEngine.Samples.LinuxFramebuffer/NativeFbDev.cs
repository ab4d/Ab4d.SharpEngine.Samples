using System.Runtime.InteropServices;
using __u32 = System.UInt32;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public static partial class Native
{
    public static unsafe class FbDev
    {
        /* ioctl() constants */
        public const uint FBIOGET_VSCREENINFO = 0x4600;
        public const uint FBIO_WAITFORVSYNC = 0x40044620;

        /* structures for FBIOGET_VSCREENINFO ioctl */
        [StructLayout(LayoutKind.Sequential)]
        public struct fb_bitfield
        {
            public __u32 offset; /* beginning of bitfield */
            public __u32 length; /* length of bitfield */
            public __u32 msb_right; /* != 0 : Most significant bit is right */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct fb_var_screeninfo
        {
            public __u32 xres; /* visible resolution */
            public __u32 yres;
            public __u32 xres_virtual; /* virtual resolution */
            public __u32 yres_virtual;
            public __u32 xoffset; /* offset from virtual to visible */
            public __u32 yoffset; /* resolution */

            public __u32 bits_per_pixel; /* guess what */
            public __u32 grayscale; /* 0 = color, 1 = grayscale, >1 = FOURCC */

            public fb_bitfield red; /* bitfield in fb mem if true color, */
            public fb_bitfield green; /* else only length is significant */
            public fb_bitfield blue;
            public fb_bitfield transp; /* transparency */

            public __u32 nonstd; /* != 0 Non standard pixel format */

            public __u32 activate; /* see FB_ACTIVATE_* */

            public __u32 height; /* height of picture in mm */
            public __u32 width; /* width of picture in mm */

            public __u32 accel_flags; /* (OBSOLETE) see fb_info.flags */

            /* Timing: All values in pixclocks, except pixclock (of course) */
            public __u32 pixclock; /* pixel clock in ps (pico seconds) */
            public __u32 left_margin; /* time from sync to picture	*/
            public __u32 right_margin; /* time from picture to sync	*/
            public __u32 upper_margin; /* time from sync to picture	*/
            public __u32 lower_margin;
            public __u32 hsync_len; /* length of horizontal sync */
            public __u32 vsync_len; /* length of vertical sync */
            public __u32 sync; /* see FB_SYNC_*	*/
            public __u32 vmode; /* see FB_VMODE_* */
            public __u32 rotate; /* angle we rotate counter clockwise */
            public __u32 colorspace; /* colorspace for FOURCC-based modes */
            public fixed __u32 reserved[4]; /* Reserved for future compatibility */
        }
    }
}
