#if NET5_0

using System.Text;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;

namespace System.Configuration.Install
{
    public class InstallContext
    {
        #region Protected Methods

        protected static StringDictionary ParseCommandLine(string[] args)
        {
            var stringDictionary = new StringDictionary();
            if (args == null)
                return stringDictionary;
            for (var index = 0; index < args.Length; ++index)
            {
                if (args[index].StartsWith("-", StringComparison.Ordinal))
                    args[index] = args[index].Substring(1);
                var length = args[index].IndexOf('=');
                if (length < 0)
                    stringDictionary[args[index].ToLower(CultureInfo.InvariantCulture)] = "";
                else
                    stringDictionary[args[index].Substring(0, length).ToLower(CultureInfo.InvariantCulture)] = args[index].Substring(length + 1);
            }
            return stringDictionary;
        }

        #endregion Protected Methods

        #region Internal Methods

        internal void LogMessageHelper(string message)
        {
            var streamWriter = (StreamWriter)null;
            try
            {
                if (string.IsNullOrEmpty(Parameters["logfile"]))
                    return;
                streamWriter = new StreamWriter(Parameters["logfile"], true, Encoding.UTF8);
                streamWriter.WriteLine(message);
            }
            finally
            {
                streamWriter?.Close();
            }
        }

        #endregion Internal Methods

        #region Public Constructors

        public InstallContext()
          : this(null, null)
        {
        }

        public InstallContext(string logFilePath, string[] commandLine)
        {
            Parameters = InstallContext.ParseCommandLine(commandLine);
            if (Parameters["logfile"] != null || logFilePath == null)
                return;
            Parameters["logfile"] = logFilePath;
        }

        #endregion Public Constructors

        #region Public Properties

        public StringDictionary Parameters { get; }

        #endregion Public Properties

        #region Public Methods

        public bool IsParameterTrue(string paramName)
        {
            var parameter = Parameters[paramName.ToLower(CultureInfo.InvariantCulture)];
            if (parameter == null)
                return false;
            return string.Compare(parameter, "true", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(parameter, "yes", StringComparison.OrdinalIgnoreCase) == 0 || (uint)string.Compare(parameter, "1", StringComparison.OrdinalIgnoreCase) <= 0U || "".Equals(parameter);
        }

        public void LogMessage(string message)
        {
            try
            {
                LogMessageHelper(message);
            }
            catch (Exception ex1)
            {
                try
                {
                    Parameters["logfile"] = Path.Combine(Path.GetTempPath(), Path.GetFileName(Parameters["logfile"]));
                    LogMessageHelper(message);
                }
                catch (Exception ex2)
                {
                    Parameters["logfile"] = null;
                }
            }
            if (IsParameterTrue("LogToConsole") || Parameters["logtoconsole"] == null)
                Console.WriteLine(message);
            try
            {
                InstallerLogHandler.Instance.Log(message);
            }
            catch
            {
            }
        }

        #endregion Public Methods
    }
}

#endif
