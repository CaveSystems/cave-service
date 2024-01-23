#if NET5_0_OR_GREATER

#pragma warning disable CS1591

using System.Collections;

namespace System.Configuration.Install;

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

#endif
