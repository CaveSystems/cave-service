#if NET5_0_OR_GREATER

#nullable disable

using System.Diagnostics;

namespace System.Configuration.Install;

internal static class CompModSwitches
{
    #region Private Fields

    private static TraceSwitch _installerDesign;

    #endregion Private Fields

    #region Public Properties

    public static TraceSwitch InstallerDesign => CompModSwitches._installerDesign ?? (CompModSwitches._installerDesign = new TraceSwitch(nameof(InstallerDesign), "Enable tracing for design-time code for installers"));

    #endregion Public Properties
}

#endif
