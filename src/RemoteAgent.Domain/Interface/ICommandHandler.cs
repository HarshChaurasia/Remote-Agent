
using RemoteAgent.Domain.Common;

namespace RemoteAgent.Domain.Interface
{
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task<HandlerResponse> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
