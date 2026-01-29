namespace RemoteAgent.Domain.Interface
{
    public interface IPluginService
    {
        Task<string> LoadPluginAsync(byte[] dllBytes, string pluginName);

        IEnumerable<PluginInfo> GetLoadedPlugins();

        Task UnloadPlugin(string pluginName);

        Task<string> ExecutePluginAsync(string targetOS, string version, string command);
    }
    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string TargetOS { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; }
    }
}
