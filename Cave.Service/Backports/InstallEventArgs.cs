#if NET5_0

using System.Collections;

namespace System.Configuration.Install
{
    public class InstallEventArgs : EventArgs
    {
        #region Public Constructors

        public InstallEventArgs()
        {
        }

        public InstallEventArgs(IDictionary savedState) => SavedState = savedState;

        #endregion Public Constructors

        #region Public Properties

        public IDictionary SavedState { get; }

        #endregion Public Properties
    }
}

#endif
