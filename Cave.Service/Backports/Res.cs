#if NET5_0

using System.Linq;
using Cave;

namespace System.Configuration.Install
{
    internal static class Res
    {
        #region Public Methods

        public static string GetString(string text, params object[] args)
        {
            return text + (" " + args?.Join(" ")).Trim();
        }

        #endregion Public Methods
    }
}

#endif
