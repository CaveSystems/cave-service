#if !NETSTANDARD20

using System;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cave.Console;
using Cave.Logging;

namespace Cave.Service
{
    /// <summary>
    /// Provides simple ugly event logging for windows.
    /// </summary>
    public sealed class LogEventLog : LogReceiver, IDisposable
    {
        EventLog m_EventLog = null;
        LogLevel m_LogLevel = LogLevel.Information;

        /// <summary>Retrieves the process name of the process generating the messages (defaults to the program name).</summary>
        public readonly string ProcessName;

        /// <summary>Retrieves the target event log name.</summary>
        public readonly string LogName;

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
            m_EventLog = new EventLog(LogName, ".", ProcessName);
        }

        /// <summary>
        /// Creates a new instance of <see cref="LogEventLog"/> with the default process name and at the default log: "Application:ProcessName".
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
        /// Creates a new instance using a specified EventLog object.
        /// </summary>
        /// <param name="eventLog"></param>
        public LogEventLog(EventLog eventLog)
        {
            if (eventLog == null)
            {
                throw new ArgumentNullException("eventLog");
            }

            m_EventLog = eventLog;
            ProcessName = Process.GetCurrentProcess().ProcessName;
            LogName = m_EventLog.Log;
        }

        /// <summary>
        /// Creates a new instance of <see cref="LogEventLog"/>.
        /// </summary>
        public LogEventLog(EventLog eventLog, string processName)
        {
            if (eventLog == null)
            {
                throw new ArgumentNullException("eventLog");
            }

            m_EventLog = eventLog;
            LogName = m_EventLog.Log;
            ProcessName = processName;
        }

        #region ILogReceiver Member

        EventLogEntryType m_CurrentType = EventLogEntryType.Information;
        readonly StringBuilder m_CurrentMessage = new StringBuilder();
        DateTime m_LastMessage;
        Task m_FlushTask;

        void Flush()
        {
            lock (m_CurrentMessage)
            {
                if (m_CurrentMessage.Length > 0)
                {
                    m_EventLog.WriteEntry(m_CurrentMessage.ToString(), m_CurrentType);
                    m_CurrentMessage.Length = 0;
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
            if (m_EventLog == null)
            {
                return;
            }

            if (level > m_LogLevel)
            {
                return;
            }

            EventLogEntryType type = EventLogEntryType.Information;
            if (level <= LogLevel.Warning)
            {
                type = EventLogEntryType.Warning;
            }

            if (level <= LogLevel.Error)
            {
                type = EventLogEntryType.Error;
            }

            lock (m_CurrentMessage)
            {
                if (type != m_CurrentType || m_CurrentMessage.Length > 16384)
                {
                    Flush();
                }
                m_LastMessage = dateTime;
                m_CurrentType = type;
                m_CurrentMessage.Append(dateTime.ToLocalTime().ToString());
                m_CurrentMessage.Append(" Source: ");
                m_CurrentMessage.Append(source);
                m_CurrentMessage.Append(" Message: ");
                m_CurrentMessage.Append(content.Text.Trim('\r', '\n'));
                m_CurrentMessage.AppendLine();
            }

            if (m_FlushTask == null)
            {
                m_FlushTask = Task.Factory.StartNew(delegate
                {
                    if (!Monitor.TryEnter(this))
                    {
                        return;
                    }

                    Thread.Sleep(1000);
                    m_FlushTask = null;
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
        /// Returns the name of the event log.
        /// </summary>
        public string Name => m_EventLog.LogDisplayName;

        /// <summary>
        /// LogEventLog.
        /// </summary>
        public override string LogSourceName => "LogEventLog";

        #region IDisposable Support
        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "m_EventLog")]
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (m_EventLog != null)
                {
                    m_EventLog?.Close();
                    m_EventLog = null;
                }
            }
        }

        #endregion
    }
}

#endif