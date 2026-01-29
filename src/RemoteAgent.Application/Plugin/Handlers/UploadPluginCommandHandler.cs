using RemoteAgent.Application.Plugin.Commands;
using RemoteAgent.Domain.Common;
using RemoteAgent.Domain.Interface;

namespace RemoteAgent.Application.Plugin.Handlers
{
    public class UploadPluginCommandHandler : ICommandHandler<UploadPluginCommand>
    {
        private readonly IPluginService _pluginService;
        
        public UploadPluginCommandHandler(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }
        
        public async Task<HandlerResponse> Handle(UploadPluginCommand command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.DllFile))
            {
                return new HandlerResponse("DllFile cannot be empty.", false);
            }

            byte[] dllBytes;
            try
            {
                dllBytes = Convert.FromBase64String(command.DllFile);
            }
            catch (FormatException)
            {
                return new HandlerResponse("DllFile encoding is invalid.", false);
            }

            if (dllBytes.Length == 0)
            {
                return new HandlerResponse("File is empty.", false);
            }

            try
            {
                var result = await _pluginService.LoadPluginAsync(dllBytes, command.Name);
                return new HandlerResponse(result, true);
            }
            catch (Exception ex)
            {
                return new HandlerResponse($"Failed to load plugin: {ex.Message}", false);
            }
        }
    }
}
