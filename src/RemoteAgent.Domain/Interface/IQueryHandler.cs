namespace RemoteAgent.Domain.Interface
{
    public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult> 
    {
        Task<TResult> Handle(TQuery query);
    }
}
