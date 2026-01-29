using Microsoft.AspNetCore.Mvc;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Application.Plugin.Commands;
using RemoteAgent.WebAPI.Model;

namespace RemoteAgent.WebAPI.Controllers
{
    [ApiController]
    [Route("execute")]
    public class ExecutePluginController : ControllerBase
    {
        private readonly ICommandHandler<ExecutePluginCommand> _commandHandler;
        private readonly ILogger<ExecutePluginController> _logger;

        public ExecutePluginController(
            ICommandHandler<ExecutePluginCommand> commandHandler,
            ILogger<ExecutePluginController> logger)
        {
            _commandHandler = commandHandler;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Execute([FromBody] ExecuteRequest request, CancellationToken cancellationToken)
        {
            try
            {

                var result = await _commandHandler.Handle(
                    new ExecutePluginCommand(request.TargetOS, request.Version, request.Command),
                    cancellationToken);

                _logger.LogInformation("Plugin executed. TargetOS: {TargetOS}, Version: {Version}, Command: {Command}",
                    request.TargetOS, request.Version, request.Command);

                return Ok(new ExecuteResponse
                {
                    Success = true,
                    Result = result.IsSuccess ? (result.Response?.ToString() ?? string.Empty) : string.Empty,
                    ErrorMessage = !result.IsSuccess ? (result.Response?.ToString() ?? string.Empty) : string.Empty,
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing plugin");
                return StatusCode(500, new ExecuteResponse
                {
                    Success = false,
                    ErrorMessage = $"Execution failed: {ex.Message}"
                });
            }
        }
    }
}