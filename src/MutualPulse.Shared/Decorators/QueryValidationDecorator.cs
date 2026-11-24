using CSharpFunctionalExtensions;
using MutualPulse.Shared;
using MutualPulse.Shared.Abstractions;
using MutualPulse.Shared.Validation;

namespace MutualPulse.Shared.Decorators;

public sealed class QueryValidationDecorator<TResponse, TQuery>(
    IQueryHandler<TResponse, TQuery> inner,
    ValidationExecutor<TQuery> validator)
    : IQueryHandler<TResponse, TQuery>
    where TQuery : IQuery
{
    public Task<Result<TResponse, Error>> Handle(
        TQuery query,
        CancellationToken cancellationToken)
    {
        return validator.ExecuteAsync(
            query,
            token => inner.Handle(query, token),
            cancellationToken);
    }
}