namespace NovaBank.Http
{
    /// <summary>The listener that accepts transfers from the other bank.</summary>
    internal interface IBankHttpServer
    {
        Task Start();
    }
}
