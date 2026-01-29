using Microsoft.AspNetCore.Mvc;
using RemoteAgent.Application.Handshake.Commands;
using RemoteAgent.Domain.Interface;
using RemoteAgent.WebAPI.Model;

namespace RemoteAgent.WebAPI.Controllers
{
    /// <summary>
    /// Controller for handling Diffie-Hellman (or Elliptic Curve Diffie-Hellman) key exchange endpoint.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HandshakeController : Controller
    {
        private readonly ICommandHandler<HandshakeInitCommand> _commandHandler;

        public HandshakeController(
            ICommandHandler<HandshakeInitCommand> commandHandler)
        {
            _commandHandler = commandHandler;
        }

        [HttpPost("init")]
        public async Task<IActionResult> Init(HandshakeRequest handshakeRequest, CancellationToken cancellationToken)
        {
            var sessionId = Guid.NewGuid().ToString();

            var res = await _commandHandler.Handle(
                new HandshakeInitCommand(handshakeRequest.PublicKey, sessionId),
                cancellationToken);

            if (!res.IsSuccess)
            {
                return BadRequest(res.Response?.ToString());
            }

            return Ok(new HandshakeResponse(res.Response!.ToString()!, sessionId));
        }
    }
}

