using System.Threading;
using Cave.Logging;

namespace Cave.Service
{
    /// <summary>
    /// Provides service runtime variables
    /// </summary>
    public sealed class ServiceParameters : ILogSource
    {
        /// <summary>
        /// Creates new service parameters
        /// </summary>
        /// <param name="hasAdminRights"></param>
        /// <param name="cmdLineMode"></param>
        /// <param name="userInteractive"></param>
        internal ServiceParameters(bool hasAdminRights, bool cmdLineMode, bool userInteractive)
        {
            HasAdminRights = hasAdminRights;
            CommandLineMode = cmdLineMode;
            UserInteractive = userInteractive;
        }

        internal bool IsStopping = false;

        /// <summary>Gets a value indicating whether the session is user interactive or a service/daemon.</summary>
        /// <value><c>true</c> if [user interactive]; otherwise, <c>false</c>.</value>
        public bool UserInteractive { get; private set; }

        /// <summary>
        /// Obtains whether the user hat admin rights or not
        /// </summary>
        public bool HasAdminRights { get; private set; }

        /// <summary>
        /// Checks whether the service shall shutdown (leave the worker function)
        /// </summary>
        public bool Shutdown { get; private set; }

        /// <summary>
        /// Running in commandline mode
        /// </summary>
        public bool CommandLineMode { get; private set; }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName => "ServiceParameters";

        /// <summary>
        /// Initiates the shutdown
        /// </summary>
        public void CommitShutdown()
        {
            this.LogInfo("Shutdown initiated.");
            Shutdown = true;
            lock (this)
            {
                Monitor.PulseAll(this);
            }
        }

        /// <summary>Waits for shutdown.</summary>
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
    }
}
