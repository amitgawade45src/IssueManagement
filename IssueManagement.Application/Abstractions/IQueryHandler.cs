using IssueManagement.Domain.Abstractions;
using MediatR;

namespace IssueManagement.Application.Abstractions;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> 
        where TQuery : IQuery<TResponse>
{
}
