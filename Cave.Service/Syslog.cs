using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Cave.Logging;

namespace Cave.Service
{
    /// <summary>
    /// Provides access to the *nix logging deamon.
    /// </summary>
    public static class Syslog
    {
        static IntPtr processNamePtr;
        static object syncRoot = new object();

        /// <summary>
        /// Starts logging to the logging deamon.
        /// </summary>
        public static void Init()
        {
            Init(SyslogOption.Pid | SyslogOption.NoDelay, SyslogFacility.Local1);
        }

        /// <summary>
        /// Starts logging to the logging deamon.
        /// </summary>
        /// <param name="option">The syslog option.</param>
        /// <param name="facility">The syslog facility.</param>
        public static void Init(SyslogOption option, SyslogFacility facility)
        {
            if (processNamePtr != IntPtr.Zero)
            {
                return;
            }

            string l_ProcessName = Process.GetCurrentProcess().ProcessName;
            processNamePtr = Marshal.StringToHGlobalAnsi(l_ProcessName);
            libc.SafeNativeMethods.openlog(processNamePtr, new IntPtr((int)option), new IntPtr((int)facility));
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="severity">The syslog severity.</param>
        /// <param name="facility">The syslog facility.</param>
        /// <param name="msg">The message tring to log.</param>
        public static void Write(SyslogSeverity severity, SyslogFacility facility, string msg)
        {
            lock (syncRoot)
            {
                if (processNamePtr == IntPtr.Zero)
                {
                    return;
                }

                int l_Priority = (((int)facility) << 3) | ((int)severity);
                libc.SafeNativeMethods.syslog(l_Priority, msg);
            }
        }

        /// <summary>
        /// Closes the connection to the logging deamon.
        /// </summary>
        public static void Close()
        {
            lock (syncRoot)
            {
                if (processNamePtr != IntPtr.Zero)
                {
                    libc.SafeNativeMethods.closelog();
                    if (processNamePtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(processNamePtr);
                    }

                    processNamePtr = IntPtr.Zero;
                }
            }
        }
    }
}
