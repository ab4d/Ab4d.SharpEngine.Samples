using System.Runtime.InteropServices;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public static partial class Native
{
    public static unsafe class LibC
    {
        private const string LibName = "libc";

        public const int STDIN_FILENO = 0; /* Standard input. */
        public const int STDOUT_FILENO = 1; /* Standard output. */
        public const int STDERR_FILENO = 2; /* Standard error output. */

        /* Structure for poll() */
        [StructLayout(LayoutKind.Sequential)]
        public struct pollfd
        {
            public int fd;
            public short events;
            public short revents;
        };

        /* Functions from libc */
        [DllImport(LibName, SetLastError = true)]
        public static extern int open(string pathname, int flags, int mode);

        [DllImport(LibName, SetLastError = true)]
        public static extern int close(int fd);

        [DllImport(LibName, SetLastError = true)]
        public static extern int ioctl(int fd, uint code, void* arg);

        [DllImport(LibName, SetLastError = true)]
        public static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, IntPtr offset);

        [DllImport(LibName, SetLastError = true)]
        public static extern int munmap(IntPtr addr, IntPtr length);


        [DllImport(LibName, SetLastError = true)]
        public static extern IntPtr memset(IntPtr addr, int value, IntPtr size);

        [DllImport(LibName, SetLastError = true)]
        public static extern int poll(pollfd *fds, IntPtr nfds, int timeout);

        [DllImport(LibName, SetLastError = true)]
        public static extern string? ttyname(int fd);

        /* open() constants */
        public const int O_WRONLY = 1;
        public const int O_RDWR = 2;

        public const int O_NOCTTY = 0x100; /* (00000400 in octal) */
        public const int O_CLOEXEC = 0x80000; /* set close_on_exec (02000000 in octal) */

        /* mmap() constants */
        public const int PROT_READ = 1;
        public const int PROT_WRITE = 2;
        public const int MAP_SHARED = 1;

        /* poll() constants */
        public const short POLLIN = 0x0001;

        /* ioctl() constants for TTY mode query/set */
        public const uint VT_OPENQRY = 0x5600;

        public const uint KDSETMODE = 0x4B3A;
        public const uint KDGETMODE = 0x4B3B;

        public const uint KDGKBMODE = 0x4B44;
        public const uint KDSKBMODE = 0x4B45;

        /* Mode constants for KDSETMODE/KDGETMODE */
        public const uint KD_TEXT = 0x00;
        public const uint KD_GRAPHICS = 0x01;

        /* Mode constants for KDGKBMODE/KDSKBMODE */
        public const uint K_OFF = 0x4;
    }
}
