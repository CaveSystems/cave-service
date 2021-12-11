using System;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cave.Logging;

namespace Cave.Service
{
    /// <summary>
    /// Provides simple ugly event logging for windows.
    /// </summary>
    public sealed class LogEventLog : LogReceiver, IDisposable
    {
        /// <summary>Retrieves the process name of the process generating the messages (defaults to the program name).</summary>
        public readonly string ProcessName;

        /// <summary>Retrieves the target event log name.</summary>
        public readonly string LogName;

        EventLog eventLog = null;
        LogLevel logLevel = LogLevel.Information;

        void Init()
        {
            try
            {
                if (!EventLog.SourceExists(ProcessName))
                {
                    EventLog.CreateEventSource(ProcessName, LogName);
                }
                if (!EventLog.SourceExists(ProcessName))
                {
                    throw new NotSupportedException(string.Format("Due to a bug in the event log system you need to restart this program once (newly created event source is not reported back to the creating process until process recreation)!"));
                }
            }
            catch (SecurityException ex)
            {
                throw new SecurityException(string.Format("The event source {0} does not exist and the current user has no right to create it!", ProcessName), ex);
            }
            eventLog = new EventLog(LogName, ".", ProcessName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventLog"/> class,
        /// with the default process name and at the default log: "Application:ProcessName".
        /// </summary>
        public LogEventLog()
        {
            if (!Platform.IsMicrosoft)
            {
                throw new InvalidOperationException("Do not use LogEventLog on non Microsoft Platforms!");
            }

            ProcessName = Process.GetCurrentProcess().ProcessName;
            LogName = "Application";
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventLog"/> class.
        /// </summary>
        /// <param name="eventLog">The event log.</param>
        public LogEventLog(EventLog eventLog)
        {
            this.eventLog = eventLog ?? throw new ArgumentNullException("eventLog");
            ProcessName = Process.GetCurrentProcess().ProcessName;
            LogName = this.eventLog.Log;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventLog"/> class.
        /// </summary>
        /// <param name="eventLog">The event log.</param>
        /// <param name="processName">The process name.</param>
        public LogEventLog(EventLog eventLog, string processName)
        {
            this.eventLog = eventLog ?? throw new ArgumentNullException("eventLog");
            LogName = this.eventLog.Log;
            ProcessName = processName;
        }

        #region ILogReceiver Member

        readonly StringBuilder currentMessage = new StringBuilder();
        EventLogEntryType currentType = EventLogEntryType.Information;
        Task flushTask;

        void Flush()
        {
            lock (currentMessage)
            {
                if (currentMessage.Length > 0)
                {
                    eventLog.WriteEntry(currentMessage.ToString(), currentType);
                    currentMessage.Length = 0;
                }
            }
        }

        /// <summary>Writes the specified log message.</summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="level">The level.</param>
        /// <param name="source">The source.</param>
        /// <param name="content">The content.</param>
        protected override void Write(DateTime dateTime, LogLevel level, string source, XT content)
        {
            if (eventLog == null)
            {
                return;
            }

            if (level > logLevel)
            {
                return;
            }

            var type = EventLogEntryType.Information;
            if (level <= LogLevel.Warning)
            {
                type = EventLogEntryType.Warning;
            }

            if (level <= LogLevel.Error)
            {
                type = EventLogEntryType.Error;
            }

            lock (currentMessage)
            {
                if (type != currentType || currentMessage.Length > 16384)
                {
                    Flush();
                }
                currentType = type;
                currentMessage.Append(dateTime.ToLocalTime().ToString());
                currentMessage.Append(" Source: ");
                currentMessage.Append(source);
                currentMessage.Append(" Message: ");
                currentMessage.Append(content.Text.Trim('\r', '\n'));
                currentMessage.AppendLine();
            }

            if (flushTask == null)
            {
                flushTask = Task.Factory.StartNew(() =>
                {
                    if (!Monitor.TryEnter(this))
                    {
                        return;
                    }

                    Thread.Sleep(1000);
                    flushTask = null;
                    Flush();
                    Monitor.Exit(this);
                });
            }
        }

        /// <summary>Closes the <see cref="LogReceiver" />.</summary>
        public override void Close()
        {
            Flush();
            base.Close();
        }

        #endregion

        /// <summary>
        /// Gets the name of the event log.
        /// </summary>
        public string Name => eventLog.LogDisplayName;

        #region IDisposable Support

        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (eventLog != null)
                {
                    eventLog?.Close();
                    eventLog = null;
                }
            }
        }

        #endregion
    }
}
