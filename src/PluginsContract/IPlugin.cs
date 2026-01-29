namespace RemoteAgent.PluginsContract
{
    /// <summary>
    /// This interface will be implemented by plugin services.
    /// </summary>
    public interface IPlugin
    {
        string Name { get; }

        string TargetOS { get; }

        string Version { get; }

        Task<string> ExecuteAsync(string command);
    }

}
