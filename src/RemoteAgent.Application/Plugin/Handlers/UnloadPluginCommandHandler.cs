using RemoteAgent.Domain.Common;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Application.Plugin.Commands;

namespace RemoteAgent.Application.Plugin.Handlers
{
    public class UnloadPluginCommandHandler : ICommandHandler<UnloadPluginCommand>
    {
        private readonly IPluginService _pluginService;

        public UnloadPluginCommandHandler(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        public async Task<HandlerResponse> Handle(UnloadPluginCommand command, CancellationToken cancellationToken)
        {
            await _pluginService.UnloadPlugin(command.PluginName);

            return new HandlerResponse($"Plugin '{command.PluginName}' unloaded successfully", true);
            
        }
    }
}
