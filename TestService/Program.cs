using System.Threading;
using Cave.Logging;
using Cave.Security;
using Cave.Service;

class Program : ServiceProgram
{
    #region Private Methods

    static void Main()
    {
        new Program().Run();
    }

    #endregion Private Methods

    #region Protected Methods

    protected override void Worker()
    {
        Logger log = new();
        while (!ServiceParameters.Shutdown)
        {
            Thread.Sleep(1000);
            if (RNG.UInt32 % 5 == 0)
            {
                var level = (LogLevel)(RNG.UInt32 % ((int)LogLevel.Verbose + 1));
                log.Log(level, $"Message of level {level}.");
            }
        }
    }

    #endregion Protected Methods
}
