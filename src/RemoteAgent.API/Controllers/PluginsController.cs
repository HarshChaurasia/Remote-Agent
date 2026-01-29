using Microsoft.AspNetCore.Mvc;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Application.Plugin.Commands;
using RemoteAgent.Application.Plugin.Queries;
using RemoteAgent.WebAPI.Model;

namespace RemoteAgent.WebAPI.Controllers
{
    /// <summary>
    /// Controller for handling assembly uploading, unloading and retrieval.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PluginsController : Controller
    {
        private readonly ICommandHandler<UploadPluginCommand> _uploadPluginCommandHandler;
        private readonly IQueryHandler<GetPluginsQuery, IEnumerable<PluginInfo>> _getPluginsQueryHandler;
        private readonly ICommandHandler<UnloadPluginCommand> _unloadPluginCommandHandler;
        private readonly ILogger<PluginsController> _logger;

        public PluginsController(
            ICommandHandler<UploadPluginCommand> uploadPluginCommandHandler,
            IQueryHandler<GetPluginsQuery, IEnumerable<PluginInfo>> getPluginsQueryHandler,
            ICommandHandler<UnloadPluginCommand> unloadPluginCommandHandler,
            ILogger<PluginsController> logger)
        {
            _uploadPluginCommandHandler = uploadPluginCommandHandler;
            _getPluginsQueryHandler = getPluginsQueryHandler;
            _unloadPluginCommandHandler = unloadPluginCommandHandler;
            _logger = logger;
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> UploadPlugin([FromBody] UploadPluginRequest request, CancellationToken cancellationToken)
        {
            
            var result = await _uploadPluginCommandHandler.Handle(
                new UploadPluginCommand(request.DllFile, request.Name), 
                cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Response?.ToString() });
            }

            _logger.LogInformation("{PluginName} uploaded successfully.", request.Name);

            return Ok(new { message = result.Response?.ToString() });
            
        }


        [HttpGet]
        public async Task<IActionResult> GetPlugins()
        {
            var query = new GetPluginsQuery();
            var plugins = await _getPluginsQueryHandler.Handle(query);
            return Ok(plugins);
        }

        [HttpDelete("{pluginName}")]
        public async Task<IActionResult> UnloadPlugin(string pluginName, CancellationToken cancellationToken)
        {
            var command = new UnloadPluginCommand(pluginName);
            var result = await _unloadPluginCommandHandler.Handle(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(new { error = result.Response?.ToString() });
            }

            _logger.LogInformation($"{pluginName} plugin unloaded.");

            return Ok(new { message = result.Response?.ToString() });
            
        }
    }
}
