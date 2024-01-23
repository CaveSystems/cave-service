using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;
using Cave.Logging;

namespace Cave.Service;

/// <summary>Provides common service tasks for the server service.</summary>
public static class ServiceHelper
{
    #region Private Fields

    static readonly Logger log = new Logger(typeof(ServiceHelper));

    #endregion Private Fields

    #region Private Properties

    static ServiceController Controller => new ServiceController(ServiceName);

    #endregion Private Properties

    #region Public Properties

    /// <summary>Gets a value indicating whether the server service is installed or not.</summary>
    public static bool IsInstalled
    {
        get
        {
            try
            {
                var controller = Controller;
                var result = controller != null;
                controller.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                log.Error("Error while checking service state:", ex);
                return false;
            }
        }
    }

    /// <summary>Gets a value indicating whether the server service is running or not.</summary>
    public static bool IsRunning
    {
        get
        {
            try
            {
                var controller = Controller;
                var result = controller.Status != ServiceControllerStatus.Stopped;
                controller.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                log.Error("Error while checking service state:\n", ex);
                return false;
            }
        }
    }

    /// <summary>Gets or sets the name of the service the helper is working on. This defaults to the product name.</summary>
    public static string ServiceName { get; set; } = AssemblyVersionInfo.Program.Product;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Installs the server service.</summary>
    /// <returns>True if the service could be installed without errors.</returns>
    public static bool InstallService()
    {
        log.Info("Installing service...");
        using var installer = new AssemblyInstaller(FileSystem.ProgramFileName, new string[0]);
        IDictionary state = new Hashtable();
        installer.UseNewContext = true;
        try
        {
            installer.Install(state);
            installer.Commit(state);
            log.Notice("Service installed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                installer.Rollback(state);
            }
            catch
            {
            }
            log.Error("Error while installing service:", ex);
            return false;
        }
    }

    /// <summary>Starts the server service.</summary>
    /// <returns>True if the service could be started without errors.</returns>
    public static bool StartService()
    {
        log.Info("Starting service...");
        try
        {
            var controller = Controller;
            if (controller.Status == ServiceControllerStatus.Running)
            {
                return true;
            }

            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            var result = controller.Status == ServiceControllerStatus.Running;
            controller.Dispose();
            if (result)
            {
                log.Notice("Service started successfully.");
            }
            else
            {
                log.Warning("Service could not be started!");
            }
            return result;
        }
        catch (Exception ex)
        {
            log.Error("Error while starting service:", ex);
            return false;
        }
    }

    /// <summary>Stops the server service.</summary>
    /// <returns>True if the service could be stopped without errors.</returns>
    public static bool StopService()
    {
        log.Info("Stopping service...");
        try
        {
            var controller = Controller;
            if (controller.Status == ServiceControllerStatus.Stopped)
            {
                return true;
            }

            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            var result = controller.Status == ServiceControllerStatus.Stopped;
            controller.Dispose();
            if (result)
            {
                log.Notice("Service stopped successfully.");
            }
            else
            {
                log.Error("Service could not be stopped!");
            }
            return result;
        }
        catch (Exception ex)
        {
            log.Error("Error while stopping service:", ex);
            return false;
        }
    }

    /// <summary>Uninstalls the server service.</summary>
    /// <returns>True if the service could be uninstalled without errors.</returns>
    public static bool UnInstallService()
    {
        if (IsRunning)
        {
            StopService();
        }

        log.Info("Uninstalling service...");
        using var installer = new AssemblyInstaller(FileSystem.ProgramFileName, new string[0]);
        IDictionary state = new Hashtable();
        installer.UseNewContext = true;
        try
        {
            installer.Uninstall(state);
            log.Notice("Service uninstalled successfully.");
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                installer.Rollback(state);
            }
            catch
            {
            }
            log.Error("Error while uninstalling service:", ex);
            return false;
        }
    }

    #endregion Public Methods
}
