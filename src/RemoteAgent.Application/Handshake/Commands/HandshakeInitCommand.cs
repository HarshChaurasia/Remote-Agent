using RemoteAgent.Domain.Interface;

namespace RemoteAgent.Application.Handshake.Commands
{
    public class HandshakeInitCommand : ICommand
    {
        public string PublicKey { get; }
        public string SessionId { get; }

        public HandshakeInitCommand(string key, string sessionId)
        {
            PublicKey = key;
            SessionId = sessionId;
        }
    }
}
