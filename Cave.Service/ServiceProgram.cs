#if !NETSTANDARD20

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Cave.Collections;
using Cave.Console;
using Cave.Logging;

#if NET35 || NET20
#elif NET40 || NET45 || NET46 || NET47
using System.Runtime.ExceptionServices;
#else
#error No code defined for the current framework or NETXX version define missing!
#endif

namespace Cave.Service
{
    /// <summary>
    /// Provides a service definiton providing deamon commandline functionality on linux and windows service functionality on windows.
    /// </summary>
    [DesignerCategory("Code")]
    public abstract class ServiceProgram : System.ServiceProcess.ServiceBase, ILogSource
    {
        /// <summary>Gets the commandline arguments if <see cref="ServiceParameters.CommandLineMode"/> == true.</summary>
        /// <value>The commandline arguments or <c>null</c>.</value>
        public Arguments CommandlineArguments { get; private set; }

        /// <summary>Gets a value indicating whether this instance is windows service.</summary>
        /// <value>
        /// <c>true</c> if this instance is a windows service; otherwise, <c>false</c>.
        /// </value>
        public bool IsWindowsService { get; private set; }

        /// <summary>Gets a value indicating whether this instance has admin rights.</summary>
        /// <value>
        /// <c>true</c> if this instance has admin rights; otherwise, <c>false</c>.
        /// </value>
        public bool HasAdminRights { get; private set; }

        /// <summary>Gets or sets the log file used. This may be null.</summary>
        /// <value>The log file or null.</value>
        public LogFile LogFile { get; protected set; }

        /// <summary>Gets the log console used. This may be null.</summary>
        /// <value>The log console or null.</value>
        public LogConsole LogConsole { get; private set; }

        /// <summary>Gets the log system used. This may be null.</summary>
        /// <value>The log system or null.</value>
        public ILogReceiver LogSystem { get; private set; }

        /// <summary>Gets the service parameters.</summary>
        /// <value>The service parameters.</value>
        public ServiceParameters ServiceParameters { get; private set; }

        #region abstract worker definition

        /// <summary>
        /// Worker function to be implemented by the real program.
        /// </summary>
        protected abstract void Worker();

        /// <summary>Called when [key pressed].</summary>
        /// <param name="keyInfo">The key information.</param>
        protected internal abstract void OnKeyPressed(ConsoleKeyInfo keyInfo);
        #endregion

        #region private implementation

        Task task;

        #region application domain unhandled error logging
        void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            string msg = "Unhandled exception!";
            if (e.IsTerminating)
            {
                msg += " Runtime terminating!";
            }

            if (ex != null)
            {
                this.LogEmergency(ex, msg + "\n" + ex.ToXT());
            }
            else
            {
                this.LogEmergency(ex, msg);
            }
        }
        #endregion

        /// <summary>Runs the worker. Used by Service and CommandLine.</summary>
        /// <exception cref="System.InvalidOperationException">Throws if another instance is alreaqdy running.</exception>
        void RunWorker()
        {
            this.LogDebug("Enter Service Mutex");

            try
            {
                Mutex mutex = new Mutex(true, ServiceName, out bool singleInstance);
                try
                {
                    if (!singleInstance)
                    {
                        string msg = string.Format("Another instance of {0} is already running on this machine!", ServiceName);
                        this.LogError(msg);
                        throw new InvalidOperationException(msg);
                    }
                    Worker();
                }
                finally
                {
                    mutex.Close();
                    GC.KeepAlive(mutex);
                }
            }
            catch (Exception ex)
            {
                this.LogEmergency(ex, "<red>Error: <default>A fatal unhandled exception was encountered at the main service worker. See logging for details.");
            }
            this.LogDebug("Exit Service Mutex");
            Logger.Flush();
        }

        #region command line functions

        /// <summary>Shows the help for this instance in commandline mode.</summary>
        protected virtual void Help()
        {
            this.LogInfo("Invalid commandline option used.\n" +
                "\n" +
                "Usage: " + Path.GetFileNameWithoutExtension(FileSystem.ProgramFileName) + " [option]\n" +
                "\n" +
                "Options:\n" +
                "--install  \tinstall service\n" +
                "--uninstall\tuninstall service\n" +
                "--start    \tstart service\n" +
                "--stop     \tstop service\n" +
                "--debug    \tcommandline run with debug logging\n" +
                "--verbose  \tcommandline run with verbose logging\n" +
                "--run      \tcommandline run");
        }

        /// <summary>
        /// Does a commandline run.
        /// </summary>
        void CommandLineRun()
        {
            this.LogInfo("Initializing service <cyan>{0}<default> commandline instance...\nRelease: <magenta>{1}<default>, FileVersion: <magenta>{2}<default>", ServiceName, VersionInfo.AssemblyVersion, VersionInfo.FileVersion);
            bool needAdminRights = false;
            if (Platform.IsMicrosoft)
            {
                foreach (Option option in CommandlineArguments.Options)
                {
                    switch (option.Name)
                    {
                        case "start":
                        case "stop":
                        case "install":
                        case "uninstall": needAdminRights = true; break;
                    }
                }

                if (needAdminRights && !HasAdminRights)
                {
                    if (Debugger.IsAttached)
                    {
                        throw new InvalidOperationException("Please debug this program in administration mode!");
                    }

                    this.LogNotice("Restarting service with administration rights!");
                    Logger.CloseAll();
                    ProcessStartInfo processStartInfo = new ProcessStartInfo(CommandlineArguments.Command, CommandlineArguments.ToString(false) + " --wait")
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                    };
                    Process.Start(processStartInfo);
                    return;
                }
                if (HasAdminRights)
                {
                    this.LogInfo("Current user has <green>admin<default> rights.");
                }
                else
                {
                    this.LogInfo("Running in <red>debug<default> mode <red>without admin<default> rights.");
                }
            }

            bool runCommandLine;
            bool wait = CommandlineArguments.IsOptionPresent("wait");

            if (Debugger.IsAttached)
            {
                runCommandLine = true;
                this.LogInfo("<red>Debugger<default> attached.");
            }
            else
            {
                if (CommandlineArguments.Options.Count == 0)
                {
                    Help();
                    return;
                }
                runCommandLine = CommandlineArguments.IsOptionPresent("run");
            }
            bool isInteractive;
            if (Platform.IsMicrosoft)
            {
                ServiceHelper.ServiceName = ServiceName;
                isInteractive = Environment.UserInteractive;
                if (CommandlineArguments.IsHelpOptionFound())
                {
                    Help();
                    return;
                }
                foreach (Option option in CommandlineArguments.Options)
                {
                    switch (option.Name)
                    {
                        case "start":
                            if (Debugger.IsAttached || !runCommandLine)
                            {
                                ServiceHelper.StartService();
                            }
                            else
                            {
                                this.LogError("Ignore <red>start<default> service command, doing commandline run!");
                            }

                            break;
                        case "stop":
                            if (Debugger.IsAttached || !runCommandLine)
                            {
                                ServiceHelper.StopService();
                            }
                            else
                            {
                                this.LogError("Ignore <red>stop<default> service command, doing commandline run!");
                            }

                            break;
                        case "install":
                            if (Debugger.IsAttached || !runCommandLine)
                            {
                                ServiceHelper.InstallService();
                            }
                            else
                            {
                                this.LogError("Ignore <red>install<default> service command, doing commandline run!");
                            }

                            break;
                        case "uninstall":
                            if (Debugger.IsAttached || !runCommandLine)
                            {
                                ServiceHelper.UnInstallService();
                            }
                            else
                            {
                                this.LogError("Ignore <red>uninstall<default> service command, doing commandline run!");
                            }

                            break;
                    }
                }
            }
            else
            {
                isInteractive = !CommandlineArguments.IsOptionPresent("daemon");
                runCommandLine = true;
            }
            if (runCommandLine)
            {
                // --- start service as program
                ServiceParameters = new ServiceParameters(HasAdminRights, true, isInteractive);
                try
                {
                    RunWorker();
                }
                catch (Exception ex)
                {
                    this.LogError("Error while running service executable in commandline mode.\n" + ex.ToXT());
                }

                // --- exit
            }
            Logger.Flush();
            if (isInteractive && wait)
            {
                SystemConsole.RemoveKeyPressedEvent();
                while (SystemConsole.KeyAvailable)
                {
                    SystemConsole.ReadKey();
                }

                SystemConsole.WriteLine("--- Press <yellow>enter<default> to exit... ---");
                while (SystemConsole.ReadKey().Key != ConsoleKey.Enter)
                {
                }
            }
        }
        #endregion

        #endregion

        #region protected overrides

        /// <summary>
        /// Handles the service start event creating a background thread calling the <see cref="RunWorker"/> function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        protected override void OnStart(string[] args)
        {
            if (ServiceParameters != null)
            {
                return;
            }

            this.LogInfo("Starting service <cyan>{0}<default>...", ServiceName);
            base.OnStart(args);
            ServiceParameters = new ServiceParameters(HasAdminRights, false, false);
            task = Task.Factory.StartNew(() =>
            {
                RunWorker();
                base.OnStop();
            }, TaskCreationOptions.LongRunning);
            this.LogInfo("Service <cyan>{0}<default> started.", ServiceName);
        }

        /// <summary>
        /// Tells the worker to shutdown by setting the <see cref="ServiceParameters"/>.
        /// </summary>
        protected override void OnStop()
        {
            if (ServiceParameters == null)
            {
                return;
            }

            this.LogInfo("Stopping service <cyan>{0}<default>...", ServiceName);
            ServiceParameters.IsStopping = true;
            if (!ServiceParameters.CommandLineMode)
            {
                base.OnStop();
            }

            ServiceParameters.CommitShutdown();
            if (task != null)
            {
                task.Wait();
                task = null;
            }
            ServiceParameters = null;
            this.LogInfo("Service <cyan>{0}<default> stopped.", ServiceName);
        }
        #endregion

        /// <summary>Initializes a new instance of the <see cref="ServiceProgram"/> class.</summary>
        public ServiceProgram()
            : base()
        {
            this.LogInfo("Initializing Service instance.");
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            Type type = GetType();
            VersionInfo = AssemblyVersionInfo.FromAssembly(type.Assembly);
            if (VersionInfo == null)
            {
                throw new InvalidDataException("Service VersionInfo unset!");
            }

            ServiceName = StringExtensions.ReplaceInvalidChars(VersionInfo.Product, ASCII.Strings.Letters + ASCII.Strings.Digits + "_", "_");
        }

        void Init()
        {
            if (Platform.IsMicrosoft)
            {
                HasAdminRights = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }

            IsWindowsService = Platform.IsMicrosoft && !Environment.UserInteractive;
            if (IsWindowsService)
            {
                // no log console + service run
                if (!HasAdminRights)
                {
                    throw new NotSupportedException("Service requires administration rights!");
                }

                LogSystem = new LogEventLog(EventLog, ServiceName);
            }
            else
            {
                CommandlineArguments = Arguments.FromEnvironment();

                // commandline run or linux daemon ?
                if (!CommandlineArguments.IsOptionPresent("daemon"))
                {
                    // no daemon -> log console
                    LogConsole = LogConsole.Create();
                    LogConsole.Title = ServiceName + " v" + VersionInfo.InformalVersion;
                    if (CommandlineArguments.IsOptionPresent("debug"))
                    {
                        LogConsole.Level = LogLevel.Debug;
                    }

                    if (CommandlineArguments.IsOptionPresent("verbose"))
                    {
                        LogConsole.Level = LogLevel.Verbose;
                    }

                    if (LogConsole.Level < LogLevel.Information)
                    {
                        LogConsole.ExceptionMode = LogExceptionMode.Full;
                    }
                }

                // on unix do syslog
                LogSystem = LogConsole;
                if (Platform.Type == PlatformType.Linux)
                {
                    LogSystem = LogSyslog.Create();
                }
            }

            if (LogSystem != null)
            {
                LogSystem.ExceptionMode = LogExceptionMode.Full;
            }
            this.LogInfo("Service <cyan>{0}<default> initialized!", ServiceName);
        }

        #region public Run() function

        /// <summary>
        /// Starts the service as service process or user interactive commandline program.
        /// In user interactive mode a logconsole is created to receive messages.
        /// In service mode an eventlog is used.
        /// </summary>
        public void Run()
        {
            try
            {
                Init();
                if (IsWindowsService)
                {
                    Run(this);
                    return;
                }

                if (SystemConsole.IsConsoleAvailable && SystemConsole.CanReadKey)
                {
                    SystemConsole.SetKeyPressedEvent(OnKeyPressed);
                }

                // run commandline
                CommandLineRun();
            }
            catch (Exception ex)
            {
                this.LogEmergency(ex, "Unhandled exception:\n" + ex.ToXT());
            }
            finally
            {
                // force stop if not already stopped / set stopped flag at service
                if (IsWindowsService)
                {
                    Stop();
                }

                Logger.Flush();
                Logger.CloseAll();
                SystemConsole.RemoveKeyPressedEvent();
                if (Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                    SystemConsole.WriteLine("--- Press <yellow>enter<default> to exit ---");
                    SystemConsole.ReadLine();
                }
            }
        }
        #endregion

        #region public properties

        /// <summary>
        /// Gets the <see cref="AssemblyVersionInfo"/> of the service.
        /// </summary>
        public AssemblyVersionInfo VersionInfo { get; private set; }

        /// <summary>
        /// Gets the string "Service" + product version info.
        /// </summary>
        public virtual string LogSourceName => "Service " + VersionInfo.Product;
        #endregion
    }
}
#endif
