#if NET5_0_OR_GREATER

#pragma warning disable CS1591

using System.Linq;
using Cave;

namespace System.Configuration.Install;

internal static class Res
{
    #region Public Methods

    public static string GetString(string text, params object[] args)
    {
        return text + (" " + args?.Join(" ")).Trim();
    }

    #endregion Public Methods
}

#endif
