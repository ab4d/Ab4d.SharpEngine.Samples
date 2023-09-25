# Off-screen Vulkan rendering with Linux framebuffer display

This example Linux-specific program demonstrates the use of
`Ab4d.SharpEngine` with off-screen Vulkan renderer, coupled with
(separate) display based on the Linux framebuffer device.

The Vulkan renderer can be either hardware (a physical device) or
software (e.g., a `llvmpipe` software device), and might not directly
expose any presentation surfaces - and hence cannot be used directly
for display. The rendered frames are transferred from the renderer
device, and copied over to the display framebuffer - which can be either
FbDev (`/dev/fbX`) or based on DRM/KMS (`/dev/dri/cardX`).

Due to the overhead of CPU-based copy between the Vulkan renderer and the
display framebuffer, this approach is less perfomant than using the Vulkan
device for rendering and presentation; on the other hand, it allows the
renderer and display device to be completely de-coupled, and might be an
attractive choice for applications running on embedded hardware.

For the sake of completeness, we also include a basic keyboard/mouse
input processing based on `udev` + `libinput`.


## Prerequisites

The low-level code in this example program interfaces with the following
native shared libraries:

* `libc`
* `libdrm.so.2`
* `libudev.so.1`
* `libinput.so.10`
* `libxkbcommon.so.0`

The last three shared libraries are used for the keyboard/mouse input,
and may, depending on the environment, need to be installed.

For example, on a Debian/Ubuntu based systems, they can be installed by:

```
apt install libudev1 libinput10 libxkbcommon0
```

## Running the program

The program can be ran either locally from console (TTY) or remotely,
for example from a `ssh` session. In either case, before running the
program, the system should be switched to console, if necessary (if
there is a graphical session running, e.g. Xorg or Wayland session).
This can be done via Ctr+Alt+Fx key (if graphical session is active,
TTY0 and TTY1 are usually taken, so you should use F3 key).

If the program starts successfully, it should show a spinning yellow
cube. Its execution can be terminated locally via ESC key (provided the
keyboard input is working; see subsequent section(s) for more details),
or, if running remotely, via SIGINT signal (i.e., it can be interrupted
gracefully from `ssh` session via Ctrl+C, or via `kill -SIGINT`).

The program uses first available Vulkan device for off-screen rendering;
in case multiple devices are available, a particular one can be selected
using the `--vulkan-device-index N` command-line option, where `N` is
the index of the device to use.

The program tries to find first available output device by default,
preferring the DRM/KMS over the FbDev (see below for details). The first
available device might not be the correct one; to specify the device to
use or force specific display backend, use the `--output-device /dev/fbX`
(for FbDeb) or `--output-device /dev/dri/cardX` (for DRM/KMS) command-line
switch.


## FbDev based display

The legacy FB display uses the FbDev device, called `/dev/fbX`, where
`X` is a number (typically starting at 0).

Typically, the `/dev/fbX` device is not accessible to user by default.
If the program exits with `Permission denied` error while trying to
use FbDev device, you might need to adjust permissions on `/dev/fbX`
to make it readable/writable by the user (either adjust permissions,
or change the ownership. as necessary).

When using the FbDev display, the program supports only native
framebuffer's resolution. Depending on particular driver/implementation,
there might be no synchronization mechanism available, and using this
display backend might result in tearing.

Note that even if the program is exited cleanly, the console remains
displaying the last rendered frame. To clear the display and restore
the console, use the `clear` command.


## DRM/KMS based display

The DRM/KMS offers modern low-level display interface. The program
implements double-buffered approach, alleviating potential tearing
issues of the FbDev based display.

The devices used by DRM/KMS are called `/dev/dri/cardX`, and can
be selected using the `--output-device` command-line switch.

The program attempts to find and use the first connected display
connector on the device. Currently, there is no option to select a
particular connector.

By default, the program uses the preferred mode (resolution) of the
active connector on the device. If multiple modes are supported, a
different one can be chosen by specifying `--width`  and/or `--height`
command line switch(es). In this case, the program will attempt to use
the first mode that matches the specified width and/or height (which
are in pixels).


## Input processing

The program implements basic keyboard event processing; so that it
can be exited by ESC key being pressed. For demonstration purposes,
processing of mouse cursor movements is also implemented, with software
mouse cursor display. The latter is enabled by default, but can be
disabled using `--show-mouse-cursor false` command-line switch.

The input support requires the corresponding shared libraries (`libudev.so.1`,
`libinput.so.10`, `libxkbcommon.so.0`) to be available, and requires
the user to have read/write access to the `/dev/input/*` devices. This
is typically achieved by ensuring that the user is member of the `input`
user group.

User not having access to input devices due to insufficient permissions
will not result in an error, only in lack of input events to be processed.


## Console mode switching

When the program is ran, the console remains active in the background,
and keyboard events are echoed to it. It is therefore possible to
accidentally type and run a command, or terminate the program by
pressing Ctrl+C.

To address this, the program can switch the console from text to
graphical mode (and then restore it back to text mode during clean-up).
This mode can be enabled using the `--console-mode-switch` command-line
switch. It is disabled by default, because it disables kernel keyboard
processing in the console, which means that in the case when program's
input processing is not working, it may be impossible for user to
locally exit/terminate the program.

Therefore, it is recommended to first try running the program with
this option disabled; once you confirm that keyboard input is working
in the program (i.e., you can exit the program by pressing ESC key
locally), you can try enabling it.


## Tested setup

The example has been developed on and tested against the following setups
and renderers:

* VirtualBox VM, Fedora 38
  * llvmpipe (LLVM 16.0.6, 256 bits)

* Dell XPS 15 9560 notebook, Fedora 39 Beta
  * Intel(R) HD Graphics 630 (KBL GT2)
  * NVIDIA GeForce GTX 1050
  * llvmpipe (LLVM 16.0.6, 256 bits)

* Raspberry Pi 4 8GB, Debian 12 Bookworm
   * V3D 4.2
   * llvmpipe (LLVM 15.0.6, 128 bits)


## Limitations

The example is by no means complete, and aims to demonstrate just the
basic concepts that should be expanded in the actual application.

The code currently supports only 32-bit depth on display framebuffer
(B8G8R8A8 or R8G8B8A8, where alpha byte is typically unused). It also
assumes that the framebuffer's memory is linear and continuous (i.e.,
that the stride corresponds to display width, without any padding). The
same assumption is made for the memory layout of the rendered frame
(as it is copied directly to the framebuffer's memory, instead of
line-by-line).

The input handling code is also just a skeleton, and does not handle
mouse button events and modifiers, which would be necessary to implement
scene manipulation.
