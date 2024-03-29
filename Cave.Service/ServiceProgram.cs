using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Cave.Console;
using Cave.IO;
using Cave.Logging;

namespace Cave.Service;

/// <summary>Provides a service definiton providing deamon commandline functionality on linux and windows service functionality on windows.</summary>
[DesignerCategory("Code")]
public class ServiceProgram : System.ServiceProcess.ServiceBase
{
    #region Private Fields

    Logger log = new Logger("Service");

    #endregion Private Fields

    #region Private Methods

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
                LogConsole = LogConsole.StartNew();
                LogConsole.Title = ServiceName + " v" + VersionInfo.InformalVersion;
                if (CommandlineArguments.IsOptionPresent("debug"))
                {
                    LogConsole.Level = LogLevel.Debug;
                }

                if (CommandlineArguments.IsOptionPresent("verbose"))
                {
                    LogConsole.Level = LogLevel.Verbose;
                }
            }

            // on unix do syslog
            LogSystem = LogConsole;
            if (Platform.Type == PlatformType.Linux)
            {
                LogSystem = LogSyslog.Create();
            }
        }

        log.Info($"Service <cyan>{ServiceName}<default> initialized!");
    }

    #endregion Private Methods

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceProgram"/> class. Use this constructor in inherited classes when overriding the Worker() proc.
    /// </summary>
    public ServiceProgram()
        : base()
    {
        log.Info("Initializing Service instance.");
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;

        var type = GetType();
        VersionInfo = AssemblyVersionInfo.FromAssembly(type.Assembly);

        ServiceName = StringExtensions.ReplaceInvalidChars(VersionInfo.Product, ASCII.Strings.Letters + ASCII.Strings.Digits + "_", "_");
        ServiceWorker = (p) => p.WaitForShutdown();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceProgram"/> class. Use this constructor if you do not want to inherit this class but use it with a
    /// specified worker action.
    /// </summary>
    public ServiceProgram(Action<ServiceParameters> worker) : this() => ServiceWorker = worker;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the commandline arguments if <see cref="ServiceParameters.CommandLineMode"/> == true.</summary>
    /// <value>The commandline arguments or <c>null</c>.</value>
    public Arguments CommandlineArguments { get; private set; }

    /// <summary>Gets a value indicating whether this instance has admin rights.</summary>
    /// <value><c>true</c> if this instance has admin rights; otherwise, <c>false</c>.</value>
    public bool HasAdminRights { get; private set; }

    /// <summary>Gets a value indicating whether this instance is windows service.</summary>
    /// <value><c>true</c> if this instance is a windows service; otherwise, <c>false</c>.</value>
    public bool IsWindowsService { get; private set; }

    /// <summary>Gets the log console used. This may be null.</summary>
    /// <value>The log console or null.</value>
    public LogConsole LogConsole { get; private set; }

    /// <summary>Gets or sets the log file used. This may be null.</summary>
    /// <value>The log file or null.</value>
    public LogFile LogFile { get; protected set; }

    /// <summary>Gets the log system used. This may be null.</summary>
    /// <value>The log system or null.</value>
    public LogReceiver LogSystem { get; private set; }

    /// <summary>Gets the service parameters.</summary>
    /// <value>The service parameters.</value>
    public ServiceParameters ServiceParameters { get; private set; }

    /// <summary>Gets or sets the action to be called when the service is started</summary>
    public Action<ServiceParameters> ServiceWorker { get; set; }

    #endregion Public Properties

    #region abstract worker definition

    /// <summary>Worker function to be implemented by the real program.</summary>
    protected virtual void Worker() => ServiceWorker(ServiceParameters);

    #endregion abstract worker definition

    #region protected implementation

    /// <summary>Gets or sets the timespan within the user has to press 'escape' twice to shutdown the commandline version of the program.</summary>
    protected TimeSpan OnKeyPressedEscapeShutdownTimeSpan { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Called when [key pressed].</summary>
    /// <param name="keyInfo">The key information.</param>
    protected internal virtual void OnKeyPressed(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key == ConsoleKey.Escape)
        {
            var now = DateTime.Now;
            var time = now - onKeyPressedEscape;
            onKeyPressedEscape = now;
            if (time > TimeSpan.Zero && time < OnKeyPressedEscapeShutdownTimeSpan)
            {
                ServiceParameters.CommitShutdown();
            }
            else
            {
                log.Info($"Press escape within <cyan>{OnKeyPressedEscapeShutdownTimeSpan.FormatTime()}<default> to perform shutdown.");
            }
        }
    }

    #endregion protected implementation

    #region private implementation

    DateTime onKeyPressedEscape;
    Task task;

    #region application domain unhandled error logging

    void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var msg = "Unhandled exception!";
        if (e.IsTerminating)
        {
            msg += " Runtime terminating!";
        }
        log.Emergency(msg, ex);
    }

    #endregion application domain unhandled error logging

    /// <summary>Runs the worker. Used by Service and CommandLine.</summary>
    /// <exception cref="System.InvalidOperationException">Throws if another instance is alreaqdy running.</exception>
    void RunWorker()
    {
        log.Debug("Enter Service Mutex");

        try
        {
            var mutex = new Mutex(true, ServiceName, out var singleInstance);
            try
            {
                if (!singleInstance)
                {
                    var msg = string.Format("Another instance of {0} is already running on this machine!", ServiceName);
                    log.Error(msg);
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
            log.Emergency("<red>Error: <default>A fatal unhandled exception was encountered at the main service worker. See logging for details.", ex);
        }
        log.Debug("Exit Service Mutex");
        Logger.Flush();
    }

    #region command line functions

    /// <summary>Does a commandline run.</summary>
    void CommandLineRun()
    {
        log.Info($"Initializing service <cyan>{ServiceName}<default> commandline instance...\nRelease: <magenta>{VersionInfo.AssemblyVersion}<default>, FileVersion: <magenta>{VersionInfo.FileVersion}<default>");
        var needAdminRights = false;
        if (Platform.IsMicrosoft)
        {
            foreach (var option in CommandlineArguments.Options)
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

                log.Notice("Restarting service with administration rights!");
                Logger.Close();
                var processStartInfo = new ProcessStartInfo(CommandlineArguments.Command, CommandlineArguments.ToString(false) + " --wait")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                };
                Process.Start(processStartInfo);
                return;
            }
            if (HasAdminRights)
            {
                log.Info("Current user has <green>admin<default> rights.");
            }
            else
            {
                log.Info("Running in <red>debug<default> mode <red>without admin<default> rights.");
            }
        }

        bool runCommandLine;
        var wait = CommandlineArguments.IsOptionPresent("wait");

        if (Debugger.IsAttached)
        {
            runCommandLine = true;
            log.Info("<red>Debugger<default> attached.");
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
            foreach (var option in CommandlineArguments.Options)
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
                            log.Error("Ignore <red>start<default> service command, doing commandline run!");
                        }

                        break;

                    case "stop":
                        if (Debugger.IsAttached || !runCommandLine)
                        {
                            ServiceHelper.StopService();
                        }
                        else
                        {
                            log.Error("Ignore <red>stop<default> service command, doing commandline run!");
                        }

                        break;

                    case "install":
                        if (Debugger.IsAttached || !runCommandLine)
                        {
                            ServiceHelper.InstallService();
                        }
                        else
                        {
                            log.Error("Ignore <red>install<default> service command, doing commandline run!");
                        }

                        break;

                    case "uninstall":
                        if (Debugger.IsAttached || !runCommandLine)
                        {
                            ServiceHelper.UnInstallService();
                        }
                        else
                        {
                            log.Error("Ignore <red>uninstall<default> service command, doing commandline run!");
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
                log.Error("Error while running service executable in commandline mode.", ex);
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

    /// <summary>Shows the help for this instance in commandline mode.</summary>
    protected virtual void Help()
    {
        log.Info("Invalid commandline option used.\n" +
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

    #endregion command line functions

    #endregion private implementation

    #region protected overrides

    /// <summary>Handles the service start event creating a background thread calling the <see cref="RunWorker"/> function.</summary>
    /// <param name="args">The arguments.</param>
    protected override void OnStart(string[] args)
    {
        if (ServiceParameters != null)
        {
            return;
        }

        log.Info($"Starting service <cyan>{ServiceName}<default>...");
        base.OnStart(args);
        ServiceParameters = new ServiceParameters(HasAdminRights, false, false);
        task = Task.Factory.StartNew(() =>
        {
            RunWorker();
            base.OnStop();
        }, TaskCreationOptions.LongRunning);
        log.Info($"Service <cyan>{ServiceName}<default> started.");
    }

    /// <summary>Tells the worker to shutdown by setting the <see cref="ServiceParameters"/>.</summary>
    protected override void OnStop()
    {
        if (ServiceParameters == null)
        {
            return;
        }

        log.Info($"Stopping service <cyan>{ServiceName}<default>...");
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
        log.Info($"Service <cyan>{ServiceName}<default> stopped.");
    }

    #endregion protected overrides

    #region public Run() function

    /// <summary>
    /// Starts the service as service process or user interactive commandline program. In user interactive mode a logconsole is created to receive messages. In
    /// service mode an eventlog is used.
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
            log.Emergency("Unhandled exception:", ex);
        }
        finally
        {
            // force stop if not already stopped / set stopped flag at service
            if (IsWindowsService)
            {
                Stop();
            }

            Logger.Flush();
            Logger.Close();
            SystemConsole.RemoveKeyPressedEvent();
            if (Debugger.IsAttached)
            {
                Thread.Sleep(1000);
                SystemConsole.WriteLine("--- Press <yellow>enter<default> to exit ---");
                SystemConsole.ReadLine();
            }
        }
    }

    #endregion public Run() function

    #region public properties

    /// <summary>Gets the <see cref="AssemblyVersionInfo"/> of the service.</summary>
    public AssemblyVersionInfo VersionInfo { get; private set; }

    #endregion public properties
}
