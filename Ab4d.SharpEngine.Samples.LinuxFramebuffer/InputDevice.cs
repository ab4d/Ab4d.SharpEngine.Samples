/*
 * libInput keyboard/mouse processing
 * Based on example from:
 * https://github.com/eyelash/tutorials/blob/master/libinput.c
 */

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public unsafe class InputDevice
{
    private Native.Udev.udev* _udev;
    private Native.LibInput.libinput* _libinput;

    private IntPtr* _libinputIoIface;

    private Native.Xkbc.xkb_state *_xkbState;
    private Native.Xkbc.xkb_keymap *_xkbKeymap;
    private Native.Xkbc.xkb_context *_xkbContext;

    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private bool _isRunning;
    private Thread? _inputThread;

    public double MousePointerX;
    public double MousePointerY;

    /* Logging */
    private readonly ILogger? _logger;

    /* Keyboard event signalling */
    public class KeyPressEventArgs : EventArgs
    {
        public uint KeyCode { get; set; }
    }
    public event EventHandler? KeyPressEventHandler;

    public InputDevice(int screenWidth, int screenHeight, string seatName = "seat0", ILoggerFactory ?loggerFactory = null)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        /* Logger */
        _logger = loggerFactory?.CreateLogger<InputDevice>();

        /* Create udev instance */
        _udev = Native.Udev.udev_new();
        if (_udev == null)
        {
            Cleanup();
            throw new Exception("Failed to create udev!");
        }

        /* Create libinput instance */
        _libinputIoIface = (IntPtr*)Marshal.AllocHGlobal(IntPtr.Size * 2);
        _libinputIoIface[0] = Marshal.GetFunctionPointerForDelegate<Native.LibInput.OpenRestrictedCallbackDelegate>(
            (path, flags, _) => Native.LibC.open(path, flags, 0));
        _libinputIoIface[1] = Marshal.GetFunctionPointerForDelegate<Native.LibInput.CloseRestrictedCallbackDelegate>(
            (fd, _) => Native.LibC.close(fd));

        _libinput = Native.LibInput.libinput_udev_create_context(_libinputIoIface, null, _udev);

        if (_libinput == null)
        {
            Cleanup();
            throw new Exception("Failed to create libinput!");
        }

        /* Assign seat */
        if (Native.LibInput.libinput_udev_assign_seat(_libinput, seatName) == -1)
        {
            Cleanup();
            throw new Exception("Failed to assign libinput seat!");
        }

        /* Create XKB context and state */
        _xkbContext = Native.Xkbc.xkb_context_new(Native.Xkbc.XKB_CONTEXT_NO_FLAGS);
        _xkbKeymap = Native.Xkbc.xkb_keymap_new_from_names(
            _xkbContext,
            null,
            Native.Xkbc.XKB_KEYMAP_COMPILE_NO_FLAGS);
        if (_xkbKeymap == null)
        {
            Cleanup();
            throw new Exception("Failed to load keymap!");
        }
        _xkbState = Native.Xkbc.xkb_state_new(_xkbKeymap);

        /* Start the input event processing thread */
        _isRunning = true;
        _inputThread = new Thread(InputMainLoop);
        _inputThread.Start();
    }

    public void Cleanup()
    {
        /* Stop the input event processing thread */
        if (_inputThread != null)
        {
            /* Signal the loop in thread function to stop, then wait for thread itself to stop */
            _isRunning = false;
            _inputThread.Join();
            _inputThread = null;
        }

        /* Free XKB */
        if (_xkbState != null)
        {
            Native.Xkbc.xkb_state_unref(_xkbState);
            _xkbState = null;
        }

        if (_xkbKeymap != null)
        {
            Native.Xkbc.xkb_keymap_unref(_xkbKeymap);
            _xkbKeymap = null;
        }

        if (_xkbContext != null)
        {
            Native.Xkbc.xkb_context_unref(_xkbContext);
            _xkbContext = null;
        }

        /* Free libinput */
        if (_libinput != null)
        {
            Native.LibInput.libinput_unref(_libinput);
            _libinput = null;
        }

        if (_libinputIoIface != null)
        {
            Marshal.FreeHGlobal((IntPtr)_libinputIoIface);
            _libinputIoIface = null;
        }

        /* Free udev */
        if (_udev != null)
        {
            Native.Udev.udev_unref(_udev);
            _udev = null;
        }
    }


    private void InputMainLoop()
    {
        Native.LibC.pollfd pollFd = new()
        {
            fd = Native.LibInput.libinput_get_fd(_libinput),
            events = Native.LibC.POLLIN,
            revents = 0
        };

        while (_isRunning)
        {
            /* Poll for events */
            if (Native.LibC.poll(&pollFd, (IntPtr)1, 10) < 0)
            {
                _logger?.LogWarning("Polling for events failed!");
                continue;
            }

            /* Dispatch */
            if (Native.LibInput.libinput_dispatch(_libinput) < 0)
            {
                _logger?.LogWarning("Event dispatch failed!");
                continue;
            }

            /* Process events */
            Native.LibInput.libinput_event* evt;
            while ((evt = Native.LibInput.libinput_get_event(_libinput)) != null)
            {

                var type = Native.LibInput.libinput_event_get_type(evt);
                _logger?.LogDebug("Received event of type {type}", type);

                if (type == Native.LibInput.LIBINPUT_EVENT_POINTER_MOTION)
                {
                    var dx = Native.LibInput.libinput_event_pointer_get_dx(evt);
                    var dy = Native.LibInput.libinput_event_pointer_get_dy(evt);
                    _logger?.LogDebug("Pointer motion event: {x}, {y}", dx, dy);
                    MousePointerX += dx;
                    MousePointerY += dy;

                    /* Clamp */
                    MousePointerX = Math.Max(Math.Min(MousePointerX, _screenWidth), 0.0);
                    MousePointerY = Math.Max(Math.Min(MousePointerY, _screenHeight), 0.0);
                }
                if (type == Native.LibInput.LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE)
                {
                    MousePointerX =
                        Native.LibInput.libinput_event_pointer_get_absolute_x_transformed(evt, (uint)_screenWidth);
                    MousePointerY =
                        Native.LibInput.libinput_event_pointer_get_absolute_y_transformed(evt, (uint)_screenHeight);
                    _logger?.LogDebug("Pointer absolute motion event: {x:F0}, {y:F0}", MousePointerX, MousePointerY);
                }
                else if (type == Native.LibInput.LIBINPUT_EVENT_POINTER_BUTTON)
                {
                    _logger?.LogDebug("Pointer button event");
                }
                else if (type == Native.LibInput.LIBINPUT_EVENT_KEYBOARD_KEY)
                {
                    var key = Native.LibInput.libinput_event_keyboard_get_key(evt);
                    var state = Native.LibInput.libinput_event_keyboard_get_key_state(evt);
                    _logger?.LogDebug("Keyboard event: key={key}, state={state}", key, state);

                    /* Update XKB state */
                    Native.Xkbc.xkb_state_update_key(_xkbState, key + 8, state);
                    if (state == Native.LibInput.LIBINPUT_KEY_STATE_PRESSED)
                    {
                        var utf32 = Native.Xkbc.xkb_state_key_get_utf32(_xkbState, key + 8);
                        if (utf32 != 0)
                        {
                            if (utf32 is >= 0x21 and <= 0x7E)
                            {
                                _logger?.LogInformation("The key {char} was pressed.", (char)utf32);
                            }
                            else
                            {
                                _logger?.LogInformation("The key U+{code:X4} was pressed.", utf32);
                            }

                            /* Fire the event */
                            KeyPressEventHandler?.Invoke(this, new KeyPressEventArgs()
                            {
                                KeyCode = utf32
                            });
                        }
                    }
                }

                Native.LibInput.libinput_event_destroy(evt);
            }
        }
    }
}
