using System;
using System.Runtime.InteropServices;
using System.Security;

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Element should begin with upper-case letter

namespace Cave.Service
{
    /// <summary>
    /// Provides access to libc library functions.
    /// </summary>
    public static class libc
    {
        /// <summary>The native library name (linux libc.so.x, macos libc.dylib.</summary>
        const string library = "libc";
        const CallingConvention callingConvention = CallingConvention.Cdecl;

        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            /// <summary>The open function creates and returns a new file descriptor for the file named by fileName. </summary>
            /// <param name="fileName">The fileName.</param>
            /// <param name="flags">The flags argument controls how the file is to be opened. </param>
            /// <returns>Returns a handle to the opened file.</returns>
            [DllImport(library, CallingConvention = callingConvention, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern int open(string fileName, int flags);

            /// <summary>Closes the specified handle.</summary>
            /// <param name="handle">The handle.</param>
            /// <returns>0 on success, -1 in case of failure.</returns>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern int close(int handle);

            /// <summary>The ioctl function performs the generic I/O operation command on a given handle.</summary>
            /// <param name="handle">The handle.</param>
            /// <param name="cmd">The command.</param>
            /// <param name="data">The data.</param>
            /// <returns>0 on success, -1 in case of failure.</returns>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern int ioctl(int handle, int cmd, IntPtr data);

            /// <summary>map files or devices into memory.</summary>
            /// <param name="address">The address.</param>
            /// <param name="length">The length.</param>
            /// <param name="prot">The protection flags.</param>
            /// <param name="flags">The mapping flags.</param>
            /// <param name="fd">The handle.</param>
            /// <param name="offset">The offset.</param>
            /// <returns>Pointer to the data in memory.</returns>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern IntPtr mmap(IntPtr address, UIntPtr length, PROT prot, MAP flags, int fd, IntPtr offset);

            /// <summary>This function copies num bytes from source to dest. It assumes that the source and destination regions don't overlap; if you need to copy overlapping regions, use memmove instead. See section memmove.</summary>
            /// <param name="dest">The destination.</param>
            /// <param name="src">The source.</param>
            /// <param name="num">The number.</param>
            /// <returns>Returns the given destination pointer.</returns>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern IntPtr memcpy(IntPtr dest, IntPtr src, int num);

            /// <summary>
            /// opens or reopens a connection to Syslog in preparation for submitting messages.
            /// </summary>
            /// <param name="process">Ident is an arbitrary identification string which future syslog invocations will prefix to each message. This is intended to identify the source of the message, and people conventionally set it to the name of the program that will submit the messages.
            /// Please note that the string pointer ident will be retained internally by the Syslog routines. You must not free the memory that ident points to. It is also dangerous to pass a reference to an automatic variable since leaving the scope would mean ending the lifetime of the variable. If you want to change the ident string, you must call openlog again; overwriting the string pointed to by ident is not thread-safe. </param>
            /// <param name="option">SyslogOption.</param>
            /// <param name="facility">SyslogFacility.</param>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern void openlog(IntPtr process, IntPtr option, IntPtr facility);

            /// <summary>
            /// submits a message to the Syslog facility. It does this by writing to the Unix domain socket /dev/log.
            /// </summary>
            /// <param name="priority">SyslogPriority.</param>
            /// <param name="msg">Message to submit.</param>
            [DllImport(library, CallingConvention = callingConvention, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern void syslog(int priority, string msg);

            /// <summary>
            /// closes the current Syslog connection, if there is one. This includes closing the /dev/log socket, if it is open. closelog also sets the identification string for Syslog messages back to the default, if openlog was called with a non-NULL argument to ident. The default identification string is the program name taken from argv[0].
            /// </summary>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern void closelog();

            /// <summary>
            /// When your system has configurable system limits, you can use the sysconf function to find out the value that applies to any particular machine.
            /// </summary>
            /// <param name="parameter">The system config parameter.</param>
            /// <returns>If name is invalid, -1 is returned, and errno is set to EINVAL. Otherwise, the value returned is the value of the system resource and errno is not changed.</returns>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern long sysconf(SysConf parameter);

            /// <summary>
            /// Gets the name of the unix distro.
            /// </summary>
            /// <param name="buf">Pointer to an arry to fill.</param>
            /// <returns>0 on success, -1 in case of failure.</returns>
            [DllImport(library, CallingConvention = callingConvention)]
            public static extern int uname(IntPtr buf);
        }

        /// <summary>Gets the name of the unix distro.</summary>
        /// <returns>The unix distro name.</returns>
        public static string GetUnixName()
        {
            IntPtr buf = Marshal.AllocHGlobal(8192);
            try
            {
                int i = SafeNativeMethods.uname(buf);
                return Marshal.PtrToStringAnsi(buf, i);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <summary>
        /// Memory page protection flags.
        /// </summary>
        [Flags]
        public enum PROT : uint
        {
            /// <summary>page can not be accessed</summary>
            NONE = 0x00,

            /// <summary>page can be read</summary>
            READ = 0x01,

            /// <summary>page can be written</summary>
            WRITE = 0x02,

            /// <summary>page can be executed</summary>
            EXEC = 0x04,

            /// <summary>page may be used for atomic ops</summary>
            SEM = 0x10,

            /// <summary>mprotect flag: extend change to start of growsdown vma</summary>
            GROWSDOWN = 0x01000000,

            /// <summary>mprotect flag: extend change to end of growsup vma</summary>
            GROWSUP = 0x02000000,
        }

        /// <summary>
        /// Memory page mapping flags.
        /// </summary>
        [Flags]
        public enum MAP : uint
        {
            /// <summary>Share changes</summary>
            SHARED = 0x001,

            /// <summary>Changes are private</summary>
            PRIVATE = 0x002,

            /// <summary>Mask for type of mapping</summary>
            TYPE = 0x00f,

            /// <summary>Interpret addr exactly </summary>
            FIXED = 0x010,
        }
    }
}

#pragma warning restore IDE1006
#pragma warning restore SA1300
