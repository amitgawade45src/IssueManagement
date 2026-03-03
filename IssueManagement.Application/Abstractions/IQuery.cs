using IssueManagement.Domain.Abstractions;
using MediatR;

namespace IssueManagement.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
