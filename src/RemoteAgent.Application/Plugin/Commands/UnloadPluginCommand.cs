using RemoteAgent.Domain.Interface;

namespace RemoteAgent.Application.Plugin.Commands
{
    public class UnloadPluginCommand : ICommand
    {
        public string PluginName { get; }

        public UnloadPluginCommand(string pluginName)
        {
            PluginName = pluginName;
        }
    }
}
