#if NET5_0_OR_GREATER

#pragma warning disable CS1591

namespace System.Configuration.Install;

public class InstallerLogHandler
{
    #region Internal Methods

    internal void Log(string message)
    {
        var onLog = OnLog;
        if (onLog == null)
            return;
        onLog(this, message);
    }

    #endregion Internal Methods

    #region Public Events

    public event EventHandler<string> OnLog;

    #endregion Public Events

    #region Public Properties

    public static InstallerLogHandler Instance { get; } = new InstallerLogHandler();

    #endregion Public Properties
}

#endif
