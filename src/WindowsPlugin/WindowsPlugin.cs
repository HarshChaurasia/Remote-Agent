using RemoteAgent.PluginsContract;

namespace WindowsPlugin
{
    /// <summary>
    /// Windows plugin implementation of IPlugin
    /// </summary>
    public class WindowsPlugin : IPlugin
    {
        public string Name => "WindowsPlugin";
        public string TargetOS => "Windows";
        public string Version => "1.0";

        public Task<string> ExecuteAsync(string command)
        {
            var result = $"Command: '{command}', Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            if (command.StartsWith("info", StringComparison.OrdinalIgnoreCase))
            {
                result += $"\ninfo - Name: {Name}, TargetOS: {TargetOS}, Version: {Version}, \n";
            }
            else if (command.StartsWith("system", StringComparison.OrdinalIgnoreCase))
            {
                result += $"\nsystem - Name: {Name}, TargetOS: {TargetOS}, Version: {Version}, \n";
            }
            else
            {
                result += "Unknown Command";
            }
            return Task.FromResult(result);
        }
    }
}
