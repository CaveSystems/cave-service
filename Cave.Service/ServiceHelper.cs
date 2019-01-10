#if !NETSTANDARD20

using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;
using Cave.Console;
using Cave.IO;
using Cave.Logging;

namespace Cave.Service
{
    /// <summary>
    /// Provides common service tasks for the server service
    /// </summary>
    public static class ServiceHelper
    {
        static ServiceController GetController()
        {
            return new ServiceController(AssemblyVersionInfo.Program.Product);
        }

        /// <summary>
        /// Checks whether the server service is installed or not
        /// </summary>
        public static bool IsInstalled
        {
            get
            {
                try
                {
                    ServiceController controller = GetController();
                    bool result = controller != null;
                    controller.Dispose();
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.LogError("ServiceHelper", ex, "Error while checking service state:" + ex.ToXT());
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks whether the server service is running or not
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                try
                {
                    ServiceController controller = GetController();
                    bool result = controller.Status != ServiceControllerStatus.Stopped;
                    controller.Dispose();
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.LogError("ServiceHelper", ex, "Error while checking service state:\n" + ex.ToXT());
                    return false;
                }
            }
        }

        /// <summary>
        /// Stops the server service
        /// </summary>
        /// <returns></returns>
        public static bool StopService()
        {
            Logger.LogInfo("ServiceHelper", string.Format("Stopping service..."));
            try
            {
                ServiceController controller = GetController();
                if (controller.Status == ServiceControllerStatus.Stopped)
                {
                    return true;
                }

                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                bool result = (controller.Status == ServiceControllerStatus.Stopped);
                controller.Dispose();
                if (result)
                {
                    Logger.LogNotice("ServiceHelper", "Service stopped successfully.");
                }
                else
                {
                    Logger.LogWarning("ServiceHelper", "Service could not be stopped!");
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("ServiceHelper", ex, "Error while stopping service:\n" + ex.ToXT());
                return false;
            }
        }

        /// <summary>
        /// Starts the server service
        /// </summary>
        /// <returns></returns>
        public static bool StartService()
        {
            Logger.LogInfo("ServiceHelper", string.Format("Starting service..."));
            try
            {
                ServiceController controller = GetController();
                if (controller.Status == ServiceControllerStatus.Running)
                {
                    return true;
                }

                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                bool result = (controller.Status == ServiceControllerStatus.Running);
                controller.Dispose();
                if (result)
                {
                    Logger.LogNotice("ServiceHelper", "Service started successfully.");
                }
                else
                {
                    Logger.LogWarning("ServiceHelper", "Service could not be started!");
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("ServiceHelper", "Error while starting service:\n" + ex.ToXT());
                return false;
            }
        }

        /// <summary>
        /// Installs the server service
        /// </summary>
        /// <returns></returns>
        public static bool InstallService()
        {
            Logger.LogInfo("ServiceHelper", "Installing service...");
            using (AssemblyInstaller installer = new AssemblyInstaller(FileSystem.ProgramFileName, new string[0]))
            {
                IDictionary l_State = new Hashtable();
                installer.UseNewContext = true;
                try
                {
                    installer.Install(l_State);
                    installer.Commit(l_State);
                    Logger.LogNotice("ServiceHelper", "Service installed successfully.");
                    return true;
                }
                catch (Exception ex)
                {
                    try { installer.Rollback(l_State); }
                    catch { }
                    Logger.LogError("ServiceHelper", ex, "Error while installing service:\n" + ex.ToXT());
                    return false;
                }
            }
        }

        /// <summary>
        /// Uninstalls the server service
        /// </summary>
        /// <returns></returns>
        public static bool UnInstallService()
        {
            if (IsRunning)
            {
                StopService();
            }

            Logger.LogInfo("ServiceHelper", "Uninstalling service...");
            using (AssemblyInstaller installer = new AssemblyInstaller(FileSystem.ProgramFileName, new string[0]))
            {
                IDictionary state = new Hashtable();
                installer.UseNewContext = true;
                try
                {
                    installer.Uninstall(state);
                    Logger.LogNotice("ServiceHelper", "Service uninstalled successfully.");
                    return true;
                }
                catch (Exception ex)
                {
                    try { installer.Rollback(state); }
                    catch { }
                    Logger.LogError("ServiceHelper", ex, "Error while uninstalling service:\n" + ex.ToXT());
                    return false;
                }
            }
        }
    }
}

#endif