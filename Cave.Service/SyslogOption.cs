using System;

namespace Cave.Service
{
    /// <summary>
    /// Options needed for the *nix logging deamons
    /// </summary>

    [Flags]
    public enum SyslogOption
    {
        /// <summary>
        /// Logs the pid with each message
        /// </summary>
        Pid = 0x01,

        /// <summary>
        /// Logs on the console if errors in sending
        /// </summary>
        Console = 0x02,

        /// <summary>
        /// Delays open until first log() (default)
        /// </summary>
        Delay = 0x04,

        /// <summary>
        /// Don't delay open
        /// </summary>
        NoDelay = 0x08,

        /// <summary>
        /// Don't wait for console forks: DEPRECATED
        /// </summary>
        [Obsolete]
        NoWait = 0x10,

        /// <summary>
        /// Logs to stderr as well
        /// </summary>
        PrintError = 0x20
    }
}
