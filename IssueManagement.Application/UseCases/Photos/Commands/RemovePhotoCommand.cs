using IssueManagement.Application.Abstractions;

namespace IssueManagement.Application.UseCases.Photos.Commands;

public sealed record RemovePhotoCommand(Guid IssueId, Guid PhotoId) : ICommand;
