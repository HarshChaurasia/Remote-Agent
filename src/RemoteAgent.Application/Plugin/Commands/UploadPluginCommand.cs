using RemoteAgent.Domain.Interface;

namespace RemoteAgent.Application.Plugin.Commands
{
    public class UploadPluginCommand : ICommand
    {

        public string DllFile { get; set; } 
        public string Name { get; set; }

        public UploadPluginCommand(string dllFile, string name)
        {
            DllFile = dllFile;
            Name = name;
        }
    }
}
