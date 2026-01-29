
using RemoteAgent.Domain.Interface;

namespace RemoteAgent.Application.Plugin.Commands
{
    public class ExecutePluginCommand : ICommand
    {
        public string TargetOS { get; }
        public string Version { get; }
        public string Command { get; }

        public ExecutePluginCommand(string targetOS, string version, string command)
        {
            TargetOS = targetOS;
            Version = version;
            Command = command;
        }
    }
}
