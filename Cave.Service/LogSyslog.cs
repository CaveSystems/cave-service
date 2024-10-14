using System;
using System.Globalization;
using Cave.Logging;

namespace Cave.Service;

/// <summary>Provides a log receiver using the posix syslog (libc) implementation.</summary>
public sealed class LogSyslog : LogReceiver
{
    #region Private Fields

    static LogSyslog? instance;
    static object syncRoot = new object();

    #endregion Private Fields

    #region Private Constructors

    /// <summary>Initializes a new instance of the <see cref="LogSyslog"/> class.</summary>
    LogSyslog()
    {
    }

    #endregion Private Constructors

    #region Public Properties

    /// <summary>Gets or sets the message encoding culture.</summary>
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>Gets or sets facility to use.</summary>
    public SyslogFacility Facility { get; set; }

    #endregion Public Properties

    #region Public Methods

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

    /// <summary>Closes the <see cref="T:Cave.Logging.LogReceiver"/>.</summary>
    public override void Close()
    {
        base.Close();
        lock (syncRoot)
        {
            Syslog.Close();
            instance = null;
        }
    }

    /// <inheritdoc/>
    public override void Write(LogMessage message)
    {
        var severity = (SyslogSeverity)Math.Min((int)message.Level, (int)SyslogSeverity.Debug);
        var content = message.Content?.ToString(null, CultureInfo);
        if (content is not null) Syslog.Write(severity, Facility, content);
    }

    #endregion Public Methods
}
