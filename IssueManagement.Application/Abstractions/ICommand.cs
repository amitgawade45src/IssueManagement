using IssueManagement.Domain.Abstractions;
using MediatR;

namespace IssueManagement.Application.Abstractions;
public interface ICommand<TResult> : IRequest<Result<TResult>>
{
}
public interface ICommand : IRequest<Result> 
{
}
