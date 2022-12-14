// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

//using SharpDX.IO;
//using SharpDX.Mathematics.Interop;
//using SharpDX.Win32;

namespace WinFormsUI.SharpDX.Windows.Desktop
{
    /// <summary>
    /// Internal class to interact with Native Message
    /// </summary>
    internal class Win32Native
    {
        #region From other SharpDX files:

        /// <summary>
        /// Native File access flags.
        /// </summary>
        [Flags]
        public enum NativeFileAccess : uint
        {
            /// <summary>
            /// Read access.
            /// </summary>
            Read = 0x80000000,

            /// <summary>
            /// Write access.
            /// </summary>
            Write = 0x40000000,

            /// <summary>
            /// Read/Write Access,
            /// </summary>
            ReadWrite = Read | Write,

            /// <summary>
            /// Execute access.
            /// </summary>
            Execute = 0x20000000,

            /// <summary>
            /// All access
            /// </summary>
            All = 0x10000000
        }

        /// <summary>
        /// Native file share.
        /// </summary>
        [Flags]
        public enum NativeFileShare : uint
        {
            /// <summary>
            /// None flag.
            /// </summary>
            None = 0x00000000,

            /// <summary>
            /// Enables subsequent open operations on an object to request read access.
            /// Otherwise, other processes cannot open the object if they request read access.
            /// If this flag is not specified, but the object has been opened for read access, the function fails.
            /// </summary>
            Read = 0x00000001,

            /// <summary>
            /// Enables subsequent open operations on an object to request write access.
            /// Otherwise, other processes cannot open the object if they request write access.
            /// If this flag is not specified, but the object has been opened for write access, the function fails.
            /// </summary>
            Write = 0x00000002,

            /// <summary>
            /// Read and Write flags.
            /// </summary>
            ReadWrite = Read | Write,

            /// <summary>
            /// Enables subsequent open operations on an object to request delete access.
            /// Otherwise, other processes cannot open the object if they request delete access.
            /// If this flag is not specified, but the object has been opened for delete access, the function fails.
            /// </summary>
            Delete = 0x00000004
        }

        /// <summary>
        /// Native file creation disposition.
        /// </summary>
        public enum NativeFileMode : uint
        {
            /// <summary>
            /// Creates a new file. The function fails if a specified file exists.
            /// </summary>
            CreateNew = 1,

            /// <summary>
            /// Creates a new file, always.
            /// If a file exists, the function overwrites the file, clears the existing attributes, combines the specified file attributes,
            /// and flags with FILE_ATTRIBUTE_ARCHIVE, but does not set the security descriptor that the SECURITY_ATTRIBUTES structure specifies.
            /// </summary>
            Create = 2,

            /// <summary>
            /// Opens a file. The function fails if the file does not exist.
            /// </summary>
            Open = 3,

            /// <summary>
            /// Opens a file, always.
            /// If a file does not exist, the function creates a file as if dwCreationDisposition is CREATE_NEW.
            /// </summary>
            OpenOrCreate = 4,

            /// <summary>
            /// Opens a file and truncates it so that its size is 0 (zero) bytes. The function fails if the file does not exist.
            /// The calling process must open the file with the GENERIC_WRITE access right.
            /// </summary>
            Truncate = 5
        }

        /// <summary>
        /// Native file attributes.
        /// </summary>
        [Flags]
        public enum NativeFileOptions : uint
        {
            /// <summary>
            /// None attribute.
            /// </summary>
            None = 0x00000000,

            /// <summary>
            /// Read only attribute.
            /// </summary>
            Readonly = 0x00000001,

            /// <summary>
            /// Hidden attribute.
            /// </summary>
            Hidden = 0x00000002,

            /// <summary>
            /// System attribute.
            /// </summary>
            System = 0x00000004,

            /// <summary>
            /// Directory attribute.
            /// </summary>
            Directory = 0x00000010,

            /// <summary>
            /// Archive attribute.
            /// </summary>
            Archive = 0x00000020,

            /// <summary>
            /// Device attribute.
            /// </summary>
            Device = 0x00000040,

            /// <summary>
            /// Normal attribute.
            /// </summary>
            Normal = 0x00000080,

            /// <summary>
            /// Temporary attribute.
            /// </summary>
            Temporary = 0x00000100,

            /// <summary>
            /// Sparse file attribute.
            /// </summary>
            SparseFile = 0x00000200,

            /// <summary>
            /// ReparsePoint attribute.
            /// </summary>
            ReparsePoint = 0x00000400,

            /// <summary>
            /// Compressed attribute.
            /// </summary>
            Compressed = 0x00000800,

            /// <summary>
            /// Offline attribute.
            /// </summary>
            Offline = 0x00001000,

            /// <summary>
            /// Not content indexed attribute.
            /// </summary>
            NotContentIndexed = 0x00002000,

            /// <summary>
            /// Encrypted attribute.
            /// </summary>
            Encrypted = 0x00004000,

            /// <summary>
            /// Write through attribute.
            /// </summary>
            Write_Through = 0x80000000,

            /// <summary>
            /// Overlapped attribute.
            /// </summary>
            Overlapped = 0x40000000,

            /// <summary>
            /// No buffering attribute.
            /// </summary>
            NoBuffering = 0x20000000,

            /// <summary>
            /// Random access attribute.
            /// </summary>
            RandomAccess = 0x10000000,

            /// <summary>
            /// Sequential scan attribute.
            /// </summary>
            SequentialScan = 0x08000000,

            /// <summary>
            /// Delete on close attribute.
            /// </summary>
            DeleteOnClose = 0x04000000,

            /// <summary>
            /// Backup semantics attribute.
            /// </summary>
            BackupSemantics = 0x02000000,

            /// <summary>
            /// Post semantics attribute.
            /// </summary>
            PosixSemantics = 0x01000000,

            /// <summary>
            /// Open reparse point attribute.
            /// </summary>
            OpenReparsePoint = 0x00200000,

            /// <summary>
            /// Open no recall attribute.
            /// </summary>
            OpenNoRecall = 0x00100000,

            /// <summary>
            /// First pipe instance attribute.
            /// </summary>
            FirstPipeInstance = 0x00080000
        }
        
        /// <summary>
        /// Interop type for a Point (2 ints).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("X: {X}, Y: {Y}")]
        public struct RawPoint
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RawPoint"/> struct.
            /// </summary>
            /// <param name="x">The X.</param>
            /// <param name="y">The y.</param>
            public RawPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            /// <summary>
            /// Left coordinate.
            /// </summary>
            public int X;

            /// <summary>
            /// Top coordinate.
            /// </summary>
            public int Y;
        }

        /// <summary>
        /// Interop type for a Rectangle (4 ints).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [DebuggerDisplay("Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}")]
        public struct RawRectangle
        {
            public RawRectangle(int left, int top, int right, int bottom)
            {
                Left   = left;
                Top    = top;
                Right  = right;
                Bottom = bottom;
            }

            /// <summary>
            /// The left position.
            /// </summary>
            public int Left;

            /// <summary>
            /// The top position.
            /// </summary>
            public int Top;

            /// <summary>
            /// The right position
            /// </summary>
            public int Right;

            /// <summary>
            /// The bottom position.
            /// </summary>
            public int Bottom;

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
            public bool IsEmpty
            {
                get { return Left == 0 && Top == 0 && Right == 0 && Bottom == 0; }
            }
        }

        #endregion


        [DllImport("kernel32.dll", EntryPoint = "CreateFile", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr Create(
            string fileName,
            NativeFileAccess desiredAccess,
            NativeFileShare shareMode,
            IntPtr securityAttributes,
            NativeFileMode mode,
            NativeFileOptions flagsAndOptions,
            IntPtr templateFile);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct TextMetric
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public RawPoint p;
        }


        [DllImport("user32.dll", EntryPoint = "PeekMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin,
                                              int wMsgFilterMax, int wRemoveMsg);

        [DllImport("user32.dll", EntryPoint = "GetMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin,
                                             int wMsgFilterMax);

        [DllImport("user32.dll", EntryPoint = "TranslateMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int TranslateMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll", EntryPoint = "DispatchMessage"), SuppressUnmanagedCodeSecurity]
        public static extern int DispatchMessage(ref NativeMessage lpMsg);

        public enum WindowLongType : int
        {
            WndProc = (-4),
            HInstance = (-6),
            HwndParent = (-8),
            Style = (-16),
            ExtendedStyle = (-20),
            UserData = (-21),
            Id = (-12)
        }

        public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static IntPtr GetWindowLong(HandleRef hWnd, WindowLongType index)
        {
            if (IntPtr.Size == 4)
            {
                return GetWindowLong32(hWnd, index);
            }
            return GetWindowLong64(hWnd, index);
        }

        [DllImport("user32.dll", EntryPoint = "GetFocus", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindowLong32(HandleRef hwnd, WindowLongType index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindowLong64(HandleRef hwnd, WindowLongType index);

        public static IntPtr SetWindowLong(HandleRef hwnd, WindowLongType index, IntPtr wndProcPtr)
        {
            if (IntPtr.Size == 4)
            {
                return SetWindowLong32(hwnd, index, wndProcPtr);
            }
            return SetWindowLongPtr64(hwnd, index, wndProcPtr);
        }

        [DllImport("user32.dll", EntryPoint = "SetParent", CharSet = CharSet.Unicode)]
        public static extern IntPtr SetParent(HandleRef hWnd, IntPtr hWndParent);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLong32(HandleRef hwnd, WindowLongType index, IntPtr wndProc);


        public static bool ShowWindow(HandleRef hWnd, bool windowVisible)
        {
            return ShowWindow(hWnd, windowVisible ? 1 : 0);
        }

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Unicode)]
        private static extern bool ShowWindow(HandleRef hWnd, int mCmdShow);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLongPtr64(HandleRef hwnd, WindowLongType index, IntPtr wndProc);

        [DllImport("user32.dll", EntryPoint = "CallWindowProc", CharSet = CharSet.Unicode)]
        public static extern IntPtr CallWindowProc(IntPtr wndProc, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetClientRect")]
        public static extern bool GetClientRect(IntPtr hWnd, out RawRectangle lpRect);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}