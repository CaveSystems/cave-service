#if NET5_0

using System.Runtime.Serialization;

namespace System.Configuration.Install
{
    [Serializable]
    public class InstallException : SystemException
    {
        #region Protected Constructors

        protected InstallException(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
        }

        #endregion Protected Constructors

        #region Public Constructors

        public InstallException() => HResult = -2146232057;

        public InstallException(string message)
          : base(message)
        {
        }

        public InstallException(string message, Exception innerException)
          : base(message, innerException)
        {
        }

        #endregion Public Constructors
    }
}

#endif
