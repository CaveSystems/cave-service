using Cave.Service;

class Program : ServiceProgram
{
    #region Private Methods

    static void Main()
    {
        new Program().Run();
    }

    protected override void Worker()
    {
        ServiceParameters.WaitForShutdown();
    }

    #endregion Private Methods
}
