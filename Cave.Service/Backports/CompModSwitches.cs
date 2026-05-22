#if NET5_0_OR_GREATER

#nullable disable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Configuration.Install;

[SuppressMessage("Naming", "CC0002")]
[SuppressMessage("Naming", "CC0004")]
internal static class CompModSwitches
{
    #region Private Fields

    static TraceSwitch _installerDesign;

    #endregion Private Fields

    #region Public Properties

    public static TraceSwitch InstallerDesign => _installerDesign ??= new TraceSwitch(nameof(InstallerDesign), "Enable tracing for design-time code for installers");

    #endregion Public Properties
}

#endif
