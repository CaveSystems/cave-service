using System.Threading;
using Cave.Logging;

namespace Cave.Service;

/// <summary>Provides service runtime variables.</summary>
public sealed class ServiceParameters
{
    #region Private Fields

    Logger log = new Logger("Service");

    #endregion Private Fields

    #region Internal Fields

    internal bool IsStopping = false;

    #endregion Internal Fields

    #region Internal Constructors

    /// <summary>Initializes a new instance of the <see cref="ServiceParameters"/> class.</summary>
    /// <param name="hasAdminRights">User has admin rights.</param>
    /// <param name="cmdLineMode">Service runs in command line mode.</param>
    /// <param name="userInteractive">Service runs in interactive shell.</param>
    internal ServiceParameters(bool hasAdminRights, bool cmdLineMode, bool userInteractive)
    {
        HasAdminRights = hasAdminRights;
        CommandLineMode = cmdLineMode;
        UserInteractive = userInteractive;
    }

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>Gets a value indicating whether running in commandline mode.</summary>
    public bool CommandLineMode { get; private set; }

    /// <summary>Gets a value indicating whether the user hat admin rights or not.</summary>
    public bool HasAdminRights { get; private set; }

    /// <summary>Gets a value indicating whether the service shall shutdown (leave the worker function).</summary>
    public bool Shutdown { get; private set; }

    /// <summary>Gets a value indicating whether the session is user interactive or a service/daemon.</summary>
    /// <value><c>true</c> if [user interactive]; otherwise, <c>false</c>.</value>
    public bool UserInteractive { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Initiates the shutdown.</summary>
    public void CommitShutdown()
    {
        log.Info("Shutdown initiated.");
        Shutdown = true;
        lock (this)
        {
            Monitor.PulseAll(this);
        }
    }

    /// <summary>Waits for shutdown.</summary>
    /// <param name="millisecondsTimeout">the timeout in millicesonds.</param>
    public void WaitForShutdown(int millisecondsTimeout = -1)
    {
        while (!Shutdown)
        {
            lock (this)
            {
                Monitor.Wait(this, millisecondsTimeout);
                if (millisecondsTimeout != -1)
                {
                    break;
                }
            }
        }
    }

    #endregion Public Methods
}
