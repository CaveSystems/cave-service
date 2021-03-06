﻿using System;
using Cave.Console;
using Cave.Logging;

namespace Cave.Service
{
    /// <summary>
    /// Provides a log receiver using the posix syslog (libc) implementation.
    /// </summary>
    public sealed class LogSyslog : LogReceiver
    {
        static object syncRoot = new object();
        static LogSyslog instance;

        /// <summary>Creates a new instance.</summary>
        /// <returns>The new syslog instance.</returns>
        /// <exception cref="Exception">Only one instance allowed!.</exception>
        public static LogSyslog Create()
        {
            lock (syncRoot)
            {
                if (instance == null)
                {
                    Syslog.Init();
                    instance = new LogSyslog();
                }
                else
                {
                    throw new Exception("Only one instance allowed!");
                }
                return instance;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="LogSyslog"/> class.</summary>
        LogSyslog()
        {
        }

        /// <summary>Closes the <see cref="T:Cave.Logging.LogReceiver" />.</summary>
        public override void Close()
        {
            base.Close();
            lock (syncRoot)
            {
                Syslog.Close();
                instance = null;
            }
        }

        /// <summary>
        /// Gets or sets facility to use.
        /// </summary>
        public SyslogFacility Facility { get; set; }

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            var severity = (SyslogSeverity)Math.Min((int)level, (int)SyslogSeverity.Debug);
            Syslog.Write(severity, Facility, content.Text);
        }
    }
}
