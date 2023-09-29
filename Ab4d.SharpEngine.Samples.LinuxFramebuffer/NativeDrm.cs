using System.Runtime.InteropServices;
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;

using __u16 = System.UInt16;
using __u32 = System.UInt32;
using __u64 = System.UInt64;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public static partial class Native
{
    public static unsafe class Drm
    {
        private const string LibName = "libdrm.so.2";

        /* Functions from libdrm */
        [DllImport(LibName, SetLastError = true)]
        public static extern int drmGetCap(int fd, uint64_t capability, uint64_t* value);

        [DllImport(LibName, SetLastError = true)]
        public static extern drmModeRes* drmModeGetResources(int fd);

        [DllImport(LibName, SetLastError = true)]
        public static extern void drmModeFreeResources(drmModeRes *ptr);

        [DllImport(LibName, SetLastError = true)]
        public static extern drmModeConnector* drmModeGetConnectorCurrent(int fd, uint32_t connector_id);

        [DllImport(LibName, SetLastError = true)]
        public static extern void drmModeFreeConnector(drmModeConnector *ptr);

        [DllImport(LibName, SetLastError = true)]
        public static extern drmModeEncoder *drmModeGetEncoder(int fd, uint32_t encoder_id);

        [DllImport(LibName, SetLastError = true)]
        public static extern void drmModeFreeEncoder(drmModeEncoder *ptr);

        [DllImport(LibName, SetLastError = true)]
        public static extern drmModeCrtc *drmModeGetCrtc(int fd, uint32_t crtc_id);

        [DllImport(LibName, SetLastError = true)]
        public static extern void drmModeFreeCrtc(drmModeCrtc *ptr);

        [DllImport(LibName, SetLastError = true)]
        public static extern int drmIoctl(int fd, ulong request, void *arg);

        [DllImport(LibName, SetLastError = true)]
        public static extern int drmModeAddFB(int fd, uint32_t width, uint32_t height, uint8_t depth, uint8_t bpp,
            uint32_t pitch, uint32_t bo_handle, out uint32_t buf_id);

        [DllImport(LibName, SetLastError = true)]
        public static extern int drmModeRmFB(int fd, uint32_t fb_id);

        [DllImport(LibName, SetLastError = true)]
        public static extern int drmModeSetCrtc(int fd, uint32_t crtc_id, uint32_t fb_id, uint32_t x, uint32_t y, uint32_t *connectors, int count, drmModeModeInfo *drm_mode);

        /* ioctl() constants */
        public const uint32_t DRM_IOCTL_MODE_CREATE_DUMB = 0xC02064B2;
        public const uint32_t DRM_IOCTL_MODE_MAP_DUMB = 0xC01064B3;
        public const uint32_t DRM_IOCTL_MODE_DESTROY_DUMB = 0xC00464B4;

        /* DRM structures */
        [StructLayout(LayoutKind.Sequential)]
        public struct drmModeRes
        {
            public int count_fbs;
            public uint32_t *fbs;

            public int count_crtcs;
            public uint32_t *crtcs;

            public int count_connectors;
            public uint32_t *connectors;

            public int count_encoders;
            public uint32_t *encoders;

            public uint32_t min_width, max_width;
            public uint32_t min_height, max_height;
        }

        public enum drmModeConnection
        {
            DRM_MODE_CONNECTED = 1,
            DRM_MODE_DISCONNECTED = 2,
            DRM_MODE_UNKNOWNCONNECTION = 3
        }

        public enum drmModeSubPixel
        {
            DRM_MODE_SUBPIXEL_UNKNOWN = 1,
            DRM_MODE_SUBPIXEL_HORIZONTAL_RGB = 2,
            DRM_MODE_SUBPIXEL_HORIZONTAL_BGR = 3,
            DRM_MODE_SUBPIXEL_VERTICAL_RGB = 4,
            DRM_MODE_SUBPIXEL_VERTICAL_BGR = 5,
            DRM_MODE_SUBPIXEL_NONE = 6
        }

        /* Mode types */
        public const uint32_t DRM_MODE_TYPE_BUILTIN = 1 << 0;
        public const uint32_t DRM_MODE_TYPE_CLOCK_C = (1 << 1) | DRM_MODE_TYPE_BUILTIN;
        public const uint32_t DRM_MODE_TYPE_CRTC_C = (1 << 2) | DRM_MODE_TYPE_BUILTIN;
        public const uint32_t DRM_MODE_TYPE_PREFERRED = 1 << 3;
        public const uint32_t DRM_MODE_TYPE_DEFAULT = 1 << 4;
        public const uint32_t DRM_MODE_TYPE_USERDEF = 1 << 5;
        public const uint32_t DRM_MODE_TYPE_DRIVER = 1 << 6;

        /* Connector type IDs */
        public const uint32_t DRM_MODE_CONNECTOR_Unknown = 0;
        public const uint32_t DRM_MODE_CONNECTOR_VGA = 1;
        public const uint32_t DRM_MODE_CONNECTOR_DVII = 2;
        public const uint32_t DRM_MODE_CONNECTOR_DVID = 3;
        public const uint32_t DRM_MODE_CONNECTOR_DVIA = 4;
        public const uint32_t DRM_MODE_CONNECTOR_Composite = 5;
        public const uint32_t DRM_MODE_CONNECTOR_SVIDEO = 6;
        public const uint32_t DRM_MODE_CONNECTOR_LVDS = 7;
        public const uint32_t DRM_MODE_CONNECTOR_Component = 8;
        public const uint32_t DRM_MODE_CONNECTOR_9PinDIN = 9;
        public const uint32_t DRM_MODE_CONNECTOR_DisplayPort = 10;
        public const uint32_t DRM_MODE_CONNECTOR_HDMIA = 11;
        public const uint32_t DRM_MODE_CONNECTOR_HDMIB = 12;
        public const uint32_t DRM_MODE_CONNECTOR_TV = 13;
        public const uint32_t DRM_MODE_CONNECTOR_eDP = 14;
        public const uint32_t DRM_MODE_CONNECTOR_VIRTUAL = 15;
        public const uint32_t DRM_MODE_CONNECTOR_DSI = 16;
        public const uint32_t DRM_MODE_CONNECTOR_DPI = 17;
        public const uint32_t DRM_MODE_CONNECTOR_WRITEBACK = 18;
        public const uint32_t DRM_MODE_CONNECTOR_SPI = 19;
        public const uint32_t DRM_MODE_CONNECTOR_USB = 20;

        private const int DRM_DISPLAY_MODE_LEN = 32;

        [StructLayout(LayoutKind.Sequential)]
        public struct drmModeModeInfo
        {
            public uint32_t clock;
            public uint16_t hdisplay, hsync_start, hsync_end, htotal, hskew;
            public uint16_t vdisplay, vsync_start, vsync_end, vtotal, vscan;

            public uint32_t vrefresh;

            public uint32_t flags;
            public uint32_t type;
            public fixed byte name[DRM_DISPLAY_MODE_LEN];
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct drmModeConnector
        {
            public uint32_t connector_id;
            public uint32_t encoder_id; /* Encoder currently connected to */
            public uint32_t connector_type;
            public uint32_t connector_type_id;
            public drmModeConnection connection;
            public uint32_t mmWidth, mmHeight; /* HxW in millimeters */
            public drmModeSubPixel subpixel;

            public int count_modes;
            public drmModeModeInfo* modes;

            public int count_props;
            public uint32_t *props; /* List of property ids */
            public uint32_t *prop_values; /* List of property values */

            public int count_encoders;
            public uint32_t *encoders; /*< List of encoder ids */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct drmModeCrtc
        {
            public uint32_t crtc_id;
            public uint32_t buffer_id; /* FB id to connect to 0 = disconnect */

            public uint32_t x, y; /* Position on the framebuffer */
            public uint32_t width, height;
            public int mode_valid;
            public drmModeModeInfo mode;

            public int gamma_size; /*< Number of gamma stops */
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct drmModeEncoder
        {
            public uint32_t encoder_id;
            public uint32_t encoder_type;
            public uint32_t crtc_id;
            public uint32_t possible_crtcs;
            public uint32_t possible_clones;
        };

        /* drmGetCap() constants */
        public const uint64_t DRM_CAP_DUMB_BUFFER = 0x1;

        /* Structures for DRM dumb buffer */
        [StructLayout(LayoutKind.Sequential)]
        public struct drm_mode_create_dumb
        {
            public __u32 height;
            public __u32 width;
            public __u32 bpp;
            public __u32 flags;

            public __u32 handle;
            public __u32 pitch;
            public __u64 size;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct drm_mode_map_dumb
        {
            public __u32 handle;
            public __u32 pad;

            public __u64 offset;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct drm_mode_destroy_dumb
        {
            public __u32 handle;
        };
    }
}
