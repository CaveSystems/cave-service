using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cave.Logging;

namespace Cave.Service;

/// <summary>Provides simple ugly event logging for windows.</summary>
public sealed class LogEventLog : LogReceiver, IDisposable
{
    #region Private Classes

    class LogEventLogWriter : ILogWriter
    {
        #region Private Fields

        readonly object writeLock = new();
        bool closed = false;
        LogEventLog eventLog;

        #endregion Private Fields

        #region Public Constructors

        public LogEventLogWriter(LogEventLog eventLog) => this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));

        #endregion Public Constructors

        #region Public Properties

        public bool IsClosed => closed;

        #endregion Public Properties

        #region Public Methods

        public void Close() => closed = true;

        public void Write(LogMessage message, IEnumerable<ILogText> items)
        {
            lock (eventLog.currentMessage)
            {
                if (eventLog == null)
                {
                    return;
                }

                if (message.Level > eventLog.logLevel)
                {
                    return;
                }

                var type = EventLogEntryType.Information;
                if (message.Level <= LogLevel.Warning)
                {
                    type = EventLogEntryType.Warning;
                }
                if (message.Level <= LogLevel.Error)
                {
                    type = EventLogEntryType.Error;
                }

                if (type != eventLog.currentType || eventLog.currentMessage.Length > 16384)
                {
                    eventLog.Flush();
                }
                eventLog.currentType = type;
                var lf = false;
                foreach (var item in items)
                {
                    if (item.Equals(LogText.NewLine))
                    {
                        eventLog.currentMessage.AppendLine(item.Text);
                        lf = true;
                    }
                    else
                    {
                        eventLog.currentMessage.Append(item.Text);
                    }
                }
                if (!lf) eventLog.currentMessage?.AppendLine();

                if (Monitor.TryEnter(writeLock))
                {
                    Task.Factory.StartNew(() =>
                    {
                        //void FlushTask()
                        lock (writeLock)
                        {
                            Thread.Sleep(1000);
                            Monitor.Exit(writeLock);
                            eventLog.Flush();
                        }
                    });
                }
            }
        }

        #endregion Public Methods
    }

    #endregion Private Classes

    #region Private Fields

    EventLog eventLog = null;

    LogLevel logLevel = LogLevel.Information;

    #endregion Private Fields

    #region Private Methods

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
        Writer = new LogEventLogWriter(this);
    }

    #endregion Private Methods

    #region Public Fields

    /// <summary>Retrieves the target event log name.</summary>
    public readonly string LogName;

    /// <summary>Retrieves the process name of the process generating the messages (defaults to the program name).</summary>
    public readonly string ProcessName;

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="LogEventLog"/> class, with the default process name and at the default log: "Application:ProcessName".</summary>
    public LogEventLog()
    {
        if (!Platform.IsMicrosoft)
        {
            throw new InvalidOperationException("Do not use LogEventLog on non Microsoft Platforms!");
        }

        ProcessName = Process.GetCurrentProcess().ProcessName;
        LogName = "Application";
        Init();
        Name = eventLog.LogDisplayName;
    }

    /// <summary>Initializes a new instance of the <see cref="LogEventLog"/> class.</summary>
    /// <param name="eventLog">The event log.</param>
    public LogEventLog(EventLog eventLog)
    {
        this.eventLog = eventLog ?? throw new ArgumentNullException("eventLog");
        ProcessName = Process.GetCurrentProcess().ProcessName;
        LogName = this.eventLog.Log;
        Init();
        Name = eventLog.LogDisplayName;
    }

    /// <summary>Initializes a new instance of the <see cref="LogEventLog"/> class.</summary>
    /// <param name="eventLog">The event log.</param>
    /// <param name="processName">The process name.</param>
    public LogEventLog(EventLog eventLog, string processName)
    {
        this.eventLog = eventLog ?? throw new ArgumentNullException("eventLog");
        LogName = this.eventLog.Log;
        ProcessName = processName;
        Init();
        Name = eventLog.LogDisplayName;
    }

    #endregion Public Constructors

    #region ILogReceiver Member

    readonly StringBuilder currentMessage = new StringBuilder();
    EventLogEntryType currentType = EventLogEntryType.Information;

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

    /// <summary>Closes the <see cref="LogReceiver"/>.</summary>
    public override void Close()
    {
        Flush();
        base.Close();
    }

    #endregion ILogReceiver Member

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

    #endregion IDisposable Support
}
