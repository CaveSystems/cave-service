using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Cave.Logging;

namespace Cave.Service
{
    /// <summary>
    /// Provides access to the *nix logging deamon
    /// </summary>
    public static class Syslog
    {
        static IntPtr m_ProcessNamePtr;
        static object m_SyncRoot = new object();

        /// <summary>
        /// Opens logging to the logging deamon
        /// </summary>
        public static void Init()
        {
            Init(SyslogOption.Pid | SyslogOption.NoDelay, SyslogFacility.Local1);
        }

        /// <summary>
        /// Opens logging to the logging deamon
        /// </summary>
        public static void Init(SyslogOption option, SyslogFacility facility)
        {
            if (m_ProcessNamePtr != IntPtr.Zero)
            {
                return;
            }

            string l_ProcessName = Process.GetCurrentProcess().ProcessName;
            m_ProcessNamePtr = Marshal.StringToHGlobalAnsi(l_ProcessName);
            libc.SafeNativeMethods.openlog(m_ProcessNamePtr, new IntPtr((int)option), new IntPtr((int)facility));
        }

        /// <summary>
        /// Logs a message
        /// </summary>
        public static void Write(SyslogSeverity severity, SyslogFacility facility, string msg)
        {
            lock (m_SyncRoot)
            {
                if (m_ProcessNamePtr == IntPtr.Zero)
                {
                    return;
                }

                int l_Priority = (((int)facility) << 3) | ((int)severity);
                libc.SafeNativeMethods.syslog(l_Priority, msg);
            }
        }

        /// <summary>
        /// Closes the connection to the logging deamon
        /// </summary>
        public static void Close()
        {
            lock (m_SyncRoot)
            {
                if (m_ProcessNamePtr != IntPtr.Zero)
                {
                    libc.SafeNativeMethods.closelog();
                    if (m_ProcessNamePtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(m_ProcessNamePtr);
                    }

                    m_ProcessNamePtr = IntPtr.Zero;
                }
            }
        }
    }
}
