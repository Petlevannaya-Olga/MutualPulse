using CSharpFunctionalExtensions;

namespace MutualPulse.Shared.Abstractions;

public interface IQueryHandler<TResponse, in TQuery>
    where TQuery : IQuery
{
    Task<Result<TResponse, Error>> Handle(
        TQuery query,
        CancellationToken cancellationToken);
}