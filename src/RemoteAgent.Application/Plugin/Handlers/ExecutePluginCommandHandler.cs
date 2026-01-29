using RemoteAgent.Domain.Common;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Application.Plugin.Commands;

namespace RemoteAgent.Application.Plugin.Handlers
{
    public class ExecutePluginCommandHandler : ICommandHandler<ExecutePluginCommand>
    {
        private readonly IPluginService _pluginService;

        public ExecutePluginCommandHandler(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        public async Task<HandlerResponse> Handle(ExecutePluginCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _pluginService.ExecutePluginAsync(
                    command.TargetOS,
                    command.Version,
                    command.Command);

                return new HandlerResponse(result, true);
            }
            catch (Exception ex)
            {
                return new HandlerResponse($"Failed to execute plugin: {ex.Message}", false);
            }
        }
    }
}
