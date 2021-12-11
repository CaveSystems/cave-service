#if NET5_0

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace System.Configuration.Install
{
    [DefaultEvent("AfterInstall")]
    public class Installer : Component
    {
        #region Private Fields

        private const string WrappedExceptionSource = "WrappedExceptionSource";
        private InstallEventHandler _afterCommitHandler;
        private InstallEventHandler _afterInstallHandler;
        private InstallEventHandler _afterRollbackHandler;
        private InstallEventHandler _afterUninstallHandler;
        private InstallEventHandler _beforeCommitHandler;
        private InstallEventHandler _beforeInstallHandler;
        private InstallEventHandler _beforeRollbackHandler;
        private InstallEventHandler _beforeUninstallHandler;
        private InstallerCollection _installers;

        #endregion Private Fields

        #region Private Methods

        private static IDictionary[] ToDictionaries(object savedState) => savedState is JArray jarray ? ((IEnumerable<JToken>)jarray).Select<JToken, IDictionary>(_ => _.ToObject<IDictionary>()).ToArray<IDictionary>() : (IDictionary[])savedState;

        private bool IsWrappedException(Exception e) => e is InstallException && e.Source == "WrappedExceptionSource" && e.TargetSite.ReflectedType == typeof(Installer);

        private void WriteEventHandlerError(string severity, string eventName, Exception e)
        {
            Context.LogMessage(Res.GetString("InstallLogError", severity, eventName, GetType().FullName));
            Installer.LogException(e, Context);
        }

        #endregion Private Methods

        #region Protected Methods

        protected virtual void OnAfterInstall(IDictionary savedState)
        {
            var afterInstallHandler = _afterInstallHandler;
            if (afterInstallHandler == null)
                return;
            afterInstallHandler(this, new InstallEventArgs(savedState));
        }

        protected virtual void OnAfterRollback(IDictionary savedState)
        {
            var afterRollbackHandler = _afterRollbackHandler;
            if (afterRollbackHandler == null)
                return;
            afterRollbackHandler(this, new InstallEventArgs(savedState));
        }

        protected virtual void OnAfterUninstall(IDictionary savedState)
        {
            var uninstallHandler = _afterUninstallHandler;
            if (uninstallHandler == null)
                return;
            uninstallHandler(this, new InstallEventArgs(savedState));
        }

        protected virtual void OnBeforeInstall(IDictionary savedState)
        {
            var beforeInstallHandler = _beforeInstallHandler;
            if (beforeInstallHandler == null)
                return;
            beforeInstallHandler(this, new InstallEventArgs(savedState));
        }

        protected virtual void OnBeforeRollback(IDictionary savedState)
        {
            var beforeRollbackHandler = _beforeRollbackHandler;
            if (beforeRollbackHandler == null)
                return;
            beforeRollbackHandler(this, new InstallEventArgs(savedState));
        }

        protected virtual void OnBeforeUninstall(IDictionary savedState)
        {
            var uninstallHandler = _beforeUninstallHandler;
            if (uninstallHandler == null)
                return;
            uninstallHandler(this, new InstallEventArgs(savedState));
        }

        protected virtual void OnCommitted(IDictionary savedState)
        {
            var afterCommitHandler = _afterCommitHandler;
            if (afterCommitHandler == null)
                return;
            afterCommitHandler(this, new InstallEventArgs(savedState));
        }

        protected virtual void OnCommitting(IDictionary savedState)
        {
            var beforeCommitHandler = _beforeCommitHandler;
            if (beforeCommitHandler == null)
                return;
            beforeCommitHandler(this, new InstallEventArgs(savedState));
        }

        #endregion Protected Methods

        #region Internal Fields

        internal Installer _parent;

        #endregion Internal Fields

        #region Internal Methods

        internal static void LogException(Exception e, InstallContext context)
        {
            var flag = true;
            for (; e != null; e = e.InnerException)
            {
                if (flag)
                {
                    context.LogMessage(e.GetType().FullName + ": " + e.Message);
                    flag = false;
                }
                else
                    context.LogMessage(Res.GetString("InstallLogInner", e.GetType().FullName, e.Message));
                if (context.IsParameterTrue("showcallstack"))
                    context.LogMessage(e.StackTrace);
            }
        }

        internal bool InstallerTreeContains(Installer target)
        {
            if (Installers.Contains(target))
                return true;
            foreach (Installer installer in Installers)
            {
                if (installer.InstallerTreeContains(target))
                    return true;
            }
            return false;
        }

        #endregion Internal Methods

        #region Public Events

        public event InstallEventHandler AfterInstall
        {
            add => _afterInstallHandler += value;
            remove => _afterInstallHandler -= value;
        }

        public event InstallEventHandler AfterRollback
        {
            add => _afterRollbackHandler += value;
            remove => _afterRollbackHandler -= value;
        }

        public event InstallEventHandler AfterUninstall
        {
            add => _afterUninstallHandler += value;
            remove => _afterUninstallHandler -= value;
        }

        public event InstallEventHandler BeforeInstall
        {
            add => _beforeInstallHandler += value;
            remove => _beforeInstallHandler -= value;
        }

        public event InstallEventHandler BeforeRollback
        {
            add => _beforeRollbackHandler += value;
            remove => _beforeRollbackHandler -= value;
        }

        public event InstallEventHandler BeforeUninstall
        {
            add => _beforeUninstallHandler += value;
            remove => _beforeUninstallHandler -= value;
        }

        public event InstallEventHandler Committed
        {
            add => _afterCommitHandler += value;
            remove => _afterCommitHandler -= value;
        }

        public event InstallEventHandler Committing
        {
            add => _beforeCommitHandler += value;
            remove => _beforeCommitHandler -= value;
        }

        #endregion Public Events

        #region Public Properties

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public InstallContext Context { get; set; }

        public virtual string HelpText
        {
            get
            {
                var stringBuilder = new StringBuilder();
                for (var index = 0; index < Installers.Count; ++index)
                {
                    var helpText = Installers[index].HelpText;
                    if (helpText.Length > 0)
                    {
                        stringBuilder.Append("\r\n");
                        stringBuilder.Append(helpText);
                    }
                }
                return stringBuilder.ToString();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public InstallerCollection Installers => _installers ?? (_installers = new InstallerCollection(this));

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [TypeConverter(typeof(InstallerParentConverter))]
        public Installer Parent
        {
            get => _parent;
            set
            {
                if (value == this)
                    throw new InvalidOperationException(Res.GetString("InstallBadParent"));
                if (value == _parent)
                    return;
                if (value != null && InstallerTreeContains(value))
                    throw new InvalidOperationException(Res.GetString("InstallRecursiveParent"));
                if (_parent != null)
                {
                    var index = _parent.Installers.IndexOf(this);
                    if (index != -1)
                        _parent.Installers.RemoveAt(index);
                }
                _parent = value;
                if (_parent != null && !_parent.Installers.Contains(this))
                    _parent.Installers.Add(this);
            }
        }

        #endregion Public Properties

        #region Public Methods

        public virtual void Commit(IDictionary savedState)
        {
            if (savedState == null)
                throw new ArgumentException(Res.GetString("InstallNullParameter", (object)nameof(savedState)));
            if (savedState["_reserved_lastInstallerAttempted"] == null || savedState["_reserved_nestedSavedStates"] == null)
                throw new ArgumentException(Res.GetString("InstallDictionaryMissingValues", (object)nameof(savedState)));
            var exception1 = (Exception)null;
            try
            {
                OnCommitting(savedState);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnCommitting", ex);
                Context.LogMessage(Res.GetString("InstallCommitException"));
                exception1 = ex;
            }
            var int32 = Convert.ToInt32(savedState["_reserved_lastInstallerAttempted"]);
            var dictionaries = Installer.ToDictionaries(savedState["_reserved_nestedSavedStates"]);
            if (int32 + 1 != dictionaries.Length || int32 >= Installers.Count)
                throw new ArgumentException(Res.GetString("InstallDictionaryCorrupted", (object)nameof(savedState)));
            for (var index = 0; index < Installers.Count; ++index)
                Installers[index].Context = Context;
            for (var index = 0; index <= int32; ++index)
            {
                try
                {
                    Installers[index].Commit(dictionaries[index]);
                }
                catch (Exception ex)
                {
                    if (!IsWrappedException(ex))
                    {
                        Context.LogMessage(Res.GetString("InstallLogCommitException", (object)Installers[index].ToString()));
                        Installer.LogException(ex, Context);
                        Context.LogMessage(Res.GetString("InstallCommitException"));
                    }
                    exception1 = ex;
                }
            }
            savedState["_reserved_nestedSavedStates"] = dictionaries;
            savedState.Remove("_reserved_lastInstallerAttempted");
            try
            {
                OnCommitted(savedState);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnCommitted", ex);
                Context.LogMessage(Res.GetString("InstallCommitException"));
                exception1 = ex;
            }
            if (exception1 != null)
            {
                var exception2 = exception1;
                exception2 = !IsWrappedException(exception1) ? new InstallException(Res.GetString("InstallCommitException"), exception1) : throw exception2;
                exception2.Source = "WrappedExceptionSource";
            }
        }

        public virtual void Install(IDictionary stateSaver)
        {
            if (stateSaver == null)
                throw new ArgumentException(Res.GetString("InstallNullParameter", (object)nameof(stateSaver)));
            try
            {
                OnBeforeInstall(stateSaver);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityError"), "OnBeforeInstall", ex);
                throw new InvalidOperationException(Res.GetString("InstallEventException", "OnBeforeInstall", GetType().FullName), ex);
            }
            var num = -1;
            var arrayList = new ArrayList();
            try
            {
                for (var index = 0; index < Installers.Count; ++index)
                    Installers[index].Context = Context;
                for (var index = 0; index < Installers.Count; ++index)
                {
                    var installer = Installers[index];
                    var stateSaver1 = (IDictionary)new Hashtable();
                    try
                    {
                        num = index;
                        installer.Install(stateSaver1);
                    }
                    finally
                    {
                        arrayList.Add(stateSaver1);
                    }
                }
            }
            finally
            {
                stateSaver.Add("_reserved_lastInstallerAttempted", num);
                stateSaver.Add("_reserved_nestedSavedStates", arrayList.ToArray(typeof(IDictionary)));
            }
            try
            {
                OnAfterInstall(stateSaver);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityError"), "OnAfterInstall", ex);
                throw new InvalidOperationException(Res.GetString("InstallEventException", "OnAfterInstall", GetType().FullName), ex);
            }
        }

        public virtual void Rollback(IDictionary savedState)
        {
            if (savedState == null)
                throw new ArgumentException(Res.GetString("InstallNullParameter", (object)nameof(savedState)));
            if (savedState["_reserved_lastInstallerAttempted"] == null || savedState["_reserved_nestedSavedStates"] == null)
                throw new ArgumentException(Res.GetString("InstallDictionaryMissingValues", (object)nameof(savedState)));
            var exception1 = (Exception)null;
            try
            {
                OnBeforeRollback(savedState);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnBeforeRollback", ex);
                Context.LogMessage(Res.GetString("InstallRollbackException"));
                exception1 = ex;
            }
            var int32 = Convert.ToInt32(savedState["_reserved_lastInstallerAttempted"]);
            var dictionaries = Installer.ToDictionaries(savedState["_reserved_nestedSavedStates"]);
            if (int32 + 1 != dictionaries.Length || int32 >= Installers.Count)
                throw new ArgumentException(Res.GetString("InstallDictionaryCorrupted", (object)nameof(savedState)));
            for (var index = Installers.Count - 1; index >= 0; --index)
                Installers[index].Context = Context;
            for (var index = int32; index >= 0; --index)
            {
                try
                {
                    Installers[index].Rollback(dictionaries[index]);
                }
                catch (Exception ex)
                {
                    if (!IsWrappedException(ex))
                    {
                        Context.LogMessage(Res.GetString("InstallLogRollbackException", (object)Installers[index].ToString()));
                        Installer.LogException(ex, Context);
                        Context.LogMessage(Res.GetString("InstallRollbackException"));
                    }
                    exception1 = ex;
                }
            }
            try
            {
                OnAfterRollback(savedState);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnAfterRollback", ex);
                Context.LogMessage(Res.GetString("InstallRollbackException"));
                exception1 = ex;
            }
            if (exception1 != null)
            {
                var exception2 = exception1;
                exception2 = !IsWrappedException(exception1) ? new InstallException(Res.GetString("InstallRollbackException"), exception1) : throw exception2;
                exception2.Source = "WrappedExceptionSource";
            }
        }

        public virtual void Uninstall(IDictionary savedState)
        {
            var exception1 = (Exception)null;
            try
            {
                OnBeforeUninstall(savedState);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnBeforeUninstall", ex);
                Context.LogMessage(Res.GetString("InstallUninstallException"));
                exception1 = ex;
            }
            IDictionary[] dictionaryArray;
            if (savedState != null)
            {
                dictionaryArray = Installer.ToDictionaries(savedState["_reserved_nestedSavedStates"]);
                if (dictionaryArray == null || dictionaryArray.Length != Installers.Count)
                    throw new ArgumentException(Res.GetString("InstallDictionaryCorrupted", (object)nameof(savedState)));
            }
            else
                dictionaryArray = new IDictionary[Installers.Count];
            for (var index = Installers.Count - 1; index >= 0; --index)
                Installers[index].Context = Context;
            for (var index = Installers.Count - 1; index >= 0; --index)
            {
                try
                {
                    Installers[index].Uninstall(dictionaryArray[index]);
                }
                catch (Exception ex)
                {
                    if (!IsWrappedException(ex))
                    {
                        Context.LogMessage(Res.GetString("InstallLogUninstallException", (object)Installers[index].ToString()));
                        Installer.LogException(ex, Context);
                        Context.LogMessage(Res.GetString("InstallUninstallException"));
                    }
                    exception1 = ex;
                }
            }
            try
            {
                OnAfterUninstall(savedState);
            }
            catch (Exception ex)
            {
                WriteEventHandlerError(Res.GetString("InstallSeverityWarning"), "OnAfterUninstall", ex);
                Context.LogMessage(Res.GetString("InstallUninstallException"));
                exception1 = ex;
            }
            if (exception1 != null)
            {
                var exception2 = exception1;
                exception2 = !IsWrappedException(exception1) ? new InstallException(Res.GetString("InstallUninstallException"), exception1) : throw exception2;
                exception2.Source = "WrappedExceptionSource";
            }
        }

        #endregion Public Methods
    }
}

#endif
