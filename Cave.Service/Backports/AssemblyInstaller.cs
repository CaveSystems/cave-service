#if NET5_0

using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace System.Configuration.Install
{
    public class AssemblyInstaller : Installer
    {
        #region Private Fields

        private static bool _helpPrinted;
        private bool _initialized;

        #endregion Private Fields

        #region Private Methods

        private static Type[] GetInstallerTypes(Assembly assem)
        {
            var arrayList = new ArrayList();
            foreach (var module in assem.GetModules())
            {
                var types = module.GetTypes();
                for (var index = 0; index < types.Length; ++index)
                {
                    if (typeof(Installer).IsAssignableFrom(types[index]) && !types[index].IsAbstract && types[index].IsPublic && ((RunInstallerAttribute)TypeDescriptor.GetAttributes(types[index])[typeof(RunInstallerAttribute)]).RunInstaller)
                        arrayList.Add(types[index]);
                }
            }
            return (Type[])arrayList.ToArray(typeof(Type));
        }

        private InstallContext CreateAssemblyContext()
        {
            var installContext = new InstallContext(System.IO.Path.ChangeExtension(Path, ".InstallLog"), CommandLine);
            if (Context != null)
                installContext.Parameters["logtoconsole"] = Context.Parameters["logtoconsole"];
            installContext.Parameters["assemblypath"] = Path;
            return installContext;
        }

        private string GetInstallStatePath(string assemblyPath)
        {
            var parameter = Context.Parameters["InstallStateDir"];
            assemblyPath = System.IO.Path.ChangeExtension(assemblyPath, ".InstallState");
            return string.IsNullOrEmpty(parameter) ? assemblyPath : System.IO.Path.Combine(parameter, System.IO.Path.GetFileName(assemblyPath));
        }

        private void InitializeFromAssembly()
        {
            Type[] installerTypes;
            try
            {
                installerTypes = AssemblyInstaller.GetInstallerTypes(Assembly);
            }
            catch (Exception ex)
            {
                Context.LogMessage(Res.GetString("InstallException", (object)Path));
                Installer.LogException(ex, Context);
                Context.LogMessage(Res.GetString("InstallAbort", (object)Path));
                throw new InvalidOperationException(Res.GetString("InstallNoInstallerTypes", (object)Path), ex);
            }
            if (installerTypes == null || installerTypes.Length == 0)
            {
                Context.LogMessage(Res.GetString("InstallNoPublicInstallers", (object)Path));
            }
            else
            {
                for (var index = 0; index < installerTypes.Length; ++index)
                {
                    try
                    {
                        Installers.Add((Installer)Activator.CreateInstance(installerTypes[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, new object[0], null));
                    }
                    catch (Exception ex)
                    {
                        Context.LogMessage(Res.GetString("InstallCannotCreateInstance", (object)installerTypes[index].FullName));
                        Installer.LogException(ex, Context);
                        throw new InvalidOperationException(Res.GetString("InstallCannotCreateInstance", (object)installerTypes[index].FullName), ex);
                    }
                }
                _initialized = true;
            }
        }

        private void PrintStartText(string activity)
        {
            if (UseNewContext)
            {
                var assemblyContext = CreateAssemblyContext();
                if (Context != null)
                {
                    Context.LogMessage(Res.GetString("InstallLogContent", (object)Path));
                    Context.LogMessage(Res.GetString("InstallFileLocation", (object)assemblyContext.Parameters["logfile"]));
                }
                Context = assemblyContext;
            }
            Context.LogMessage(string.Format(CultureInfo.InvariantCulture, activity, new object[1]
            {
         Path
            }));
            Context.LogMessage(Res.GetString("InstallLogParameters"));
            if (Context.Parameters.Count == 0)
                Context.LogMessage("   " + Res.GetString("InstallLogNone"));
            var enumerator = (IDictionaryEnumerator)Context.Parameters.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var key = (string)enumerator.Key;
                var str = (string)enumerator.Value;
                if (key.Equals("password", StringComparison.InvariantCultureIgnoreCase))
                    str = "********";
                Context.LogMessage("   " + key + " = " + str);
            }
        }

        #endregion Private Methods

        #region Public Constructors

        public AssemblyInstaller()
        {
        }

        public AssemblyInstaller(string fileName, string[] commandLine)
        {
            Path = System.IO.Path.GetFullPath(fileName);
            CommandLine = commandLine;
            UseNewContext = true;
        }

        public AssemblyInstaller(Assembly assembly, string[] commandLine)
        {
            Assembly = assembly;
            CommandLine = commandLine;
            UseNewContext = true;
        }

        #endregion Public Constructors

        #region Public Properties

        public Assembly Assembly { get; set; }

        public string[] CommandLine { get; set; }

        public override string HelpText
        {
            get
            {
                if (!string.IsNullOrEmpty(Path))
                {
                    Context = new InstallContext(null, new string[0]);
                    if (!_initialized)
                        InitializeFromAssembly();
                }
                if (AssemblyInstaller._helpPrinted)
                    return base.HelpText;
                AssemblyInstaller._helpPrinted = true;
                return Res.GetString("InstallAssemblyHelp") + "\r\n" + base.HelpText;
            }
        }

        public string Path
        {
            get => Assembly == null ? null : Assembly.Location;
            set
            {
                if (value == null)
                    Assembly = null;
                Assembly = Assembly.LoadFrom(value);
            }
        }

        public bool UseNewContext { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static void CheckIfInstallable(string assemblyName)
        {
            var assemblyInstaller = new AssemblyInstaller();
            assemblyInstaller.UseNewContext = false;
            assemblyInstaller.Path = assemblyName;
            assemblyInstaller.CommandLine = new string[0];
            assemblyInstaller.Context = new InstallContext(null, new string[0]);
            assemblyInstaller.InitializeFromAssembly();
            if (assemblyInstaller.Installers.Count == 0)
                throw new InvalidOperationException(Res.GetString("InstallNoPublicInstallers", (object)assemblyName));
        }

        public override void Commit(IDictionary savedState)
        {
            PrintStartText(Res.GetString("InstallActivityCommitting"));
            if (!_initialized)
                InitializeFromAssembly();
            var installStatePath = GetInstallStatePath(Path);
            try
            {
                if (File.Exists(installStatePath))
                    savedState = JsonConvert.DeserializeObject<IDictionary>(File.ReadAllText(installStatePath));
            }
            finally
            {
                if (Installers.Count == 0)
                {
                    Context.LogMessage(Res.GetString("RemovingInstallState"));
                    File.Delete(installStatePath);
                }
            }
            base.Commit(savedState);
        }

        public override void Install(IDictionary savedState)
        {
            PrintStartText(Res.GetString("InstallActivityInstalling"));
            if (!_initialized)
                InitializeFromAssembly();
            savedState = new Hashtable();
            try
            {
                base.Install(savedState);
            }
            finally
            {
                var str = JsonConvert.SerializeObject(savedState);
                using (var text = File.CreateText(GetInstallStatePath(Path)))
                    text.Write(str);
            }
        }

        public override void Rollback(IDictionary savedState)
        {
            PrintStartText(Res.GetString("InstallActivityRollingBack"));
            if (!_initialized)
                InitializeFromAssembly();
            var installStatePath = GetInstallStatePath(Path);
            if (File.Exists(installStatePath))
                savedState = JsonConvert.DeserializeObject<IDictionary>(File.ReadAllText(installStatePath));
            try
            {
                base.Rollback(savedState);
            }
            finally
            {
                File.Delete(installStatePath);
            }
        }

        public override void Uninstall(IDictionary savedState)
        {
            PrintStartText("InstallActivityUninstalling");
            if (!_initialized)
                InitializeFromAssembly();
            var installStatePath = GetInstallStatePath(Path);
            if (installStatePath != null && File.Exists(installStatePath))
            {
                try
                {
                    savedState = JsonConvert.DeserializeObject<IDictionary>(File.ReadAllText(installStatePath));
                }
                catch
                {
                    Context.LogMessage($"InstallSavedStateFileCorruptedWarning {Path} {installStatePath}");
                    savedState = null;
                }
            }
            else
                savedState = null;
            base.Uninstall(savedState);
            if (string.IsNullOrEmpty(installStatePath))
                return;
            try
            {
                File.Delete(installStatePath);
            }
            catch
            {
                throw new InvalidOperationException($"InstallUnableDeleteFile {installStatePath}");
            }
        }

        #endregion Public Methods
    }
}

#endif
