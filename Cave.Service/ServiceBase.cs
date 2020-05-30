#if NETSTANDARD20

using System;
using System.Collections.Generic;
using System.Text;

namespace System.ServiceProcess
{
    public abstract class ServiceBase
    {
        protected abstract void OnStart(string[] args);

        protected abstract void OnStop();
    }
}

#endif
