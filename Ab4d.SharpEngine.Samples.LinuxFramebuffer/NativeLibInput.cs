using System.Runtime.InteropServices;

using uint32_t = System.UInt32;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public static partial class Native
{
    public static unsafe class Udev
    {
        private const string LibName = "libudev.so.1";

        [StructLayout(LayoutKind.Sequential)]
        public struct udev
        {
            /* Opaque struct */
        }

        /* Functions from libudev */
        [DllImport(LibName, SetLastError = true)]
        public static extern udev* udev_new();

        [DllImport(LibName, SetLastError = true)]
        public static extern udev* udev_unref(udev *udev);
    }

    public static unsafe class LibInput
    {
        private const string LibName = "libinput.so.10";

        [StructLayout(LayoutKind.Sequential)]
        public struct libinput
        {
            /* Opaque struct */
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int OpenRestrictedCallbackDelegate(string path, int flags, void* user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CloseRestrictedCallbackDelegate(int fd, void* user_data);

        [StructLayout(LayoutKind.Sequential)]
        public struct libinput_event
        {
            /* Opaque struct */
        }

        /* Functions from libinput */
        [DllImport(LibName, SetLastError = true)]
        public static extern libinput* libinput_udev_create_context(IntPtr* iface, void* user_data, Udev.udev* udev);

        [DllImport(LibName, SetLastError = true)]
        public static extern libinput* libinput_unref(libinput* libinput);

        [DllImport(LibName, SetLastError = true)]
        public static extern int libinput_udev_assign_seat(libinput* libinput, string seat_id);

        [DllImport(LibName, SetLastError = true)]
        public static extern int libinput_get_fd(libinput* libinput);

        [DllImport(LibName, SetLastError = true)]
        public static extern int libinput_dispatch(libinput* libinput);

        [DllImport(LibName, SetLastError = true)]
        public static extern libinput_event* libinput_get_event(libinput* libinput);

        [DllImport(LibName, SetLastError = true)]
        public static extern int libinput_event_get_type(libinput_event* evt);

        [DllImport(LibName, SetLastError = true)]
        public static extern void libinput_event_destroy(libinput_event* evt);

        [DllImport(LibName, SetLastError = true)]
        public static extern double libinput_event_pointer_get_dx(libinput_event* evt);

        [DllImport(LibName, SetLastError = true)]
        public static extern double libinput_event_pointer_get_dy(libinput_event* evt);

        [DllImport(LibName, SetLastError = true)]
        public static extern double libinput_event_pointer_get_absolute_x(libinput_event* evt);

        [DllImport(LibName, SetLastError = true)]
        public static extern double libinput_event_pointer_get_absolute_x_transformed(libinput_event* evt, uint32_t width);

        [DllImport(LibName, SetLastError = true)]
        public static extern double libinput_event_pointer_get_absolute_y(libinput_event* evt);

        [DllImport(LibName, SetLastError = true)]
        public static extern double libinput_event_pointer_get_absolute_y_transformed(libinput_event* evt, uint32_t height);

        [DllImport(LibName, SetLastError = true)]
        public static extern uint32_t libinput_event_keyboard_get_key(libinput_event* evt);

        [DllImport(LibName, SetLastError = true)]
        public static extern uint32_t libinput_event_keyboard_get_key_state(libinput_event* evt);

        /* Event type constants */
        public const int LIBINPUT_EVENT_KEYBOARD_KEY = 300;
        public const int LIBINPUT_EVENT_POINTER_MOTION = 400;
        public const int LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE = 401;
        public const int LIBINPUT_EVENT_POINTER_BUTTON = 402;

        /* Key state constants */
        public const int LIBINPUT_KEY_STATE_RELEASED = 0;
        public const int LIBINPUT_KEY_STATE_PRESSED = 1;
    }

    public static unsafe class Xkbc
    {
        private const string LibName = "libxkbcommon.so.0";

        [StructLayout(LayoutKind.Sequential)]
        public struct xkb_context
        {
            /* Opaque struct */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct xkb_keymap
        {
            /* Opaque struct */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct xkb_rule_names
        {
            /* Opaque struct */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct xkb_state
        {
            /* Opaque struct */
        }

        [DllImport(LibName, SetLastError = true)]
        public static extern xkb_context* xkb_context_new(uint flags);

        [DllImport(LibName, SetLastError = true)]
        public static extern void xkb_context_unref(xkb_context* context);

        [DllImport(LibName, SetLastError = true)]
        public static extern xkb_keymap* xkb_keymap_new_from_names(xkb_context* context, xkb_rule_names* names, uint flags);

        [DllImport(LibName, SetLastError = true)]
        public static extern void xkb_keymap_unref(xkb_keymap* keymap);

        [DllImport(LibName, SetLastError = true)]
        public static extern xkb_state* xkb_state_new(xkb_keymap* keymap);

        [DllImport(LibName, SetLastError = true)]
        public static extern void xkb_state_unref(xkb_state* state);

        [DllImport(LibName, SetLastError = true)]
        public static extern uint xkb_state_update_key(xkb_state* state, uint key, uint direction);

        [DllImport(LibName, SetLastError = true)]
        public static extern uint32_t xkb_state_key_get_utf32(xkb_state* state, uint key);

        public const uint XKB_CONTEXT_NO_FLAGS = 0;

        public const uint XKB_KEYMAP_COMPILE_NO_FLAGS = 0;
    }
}
