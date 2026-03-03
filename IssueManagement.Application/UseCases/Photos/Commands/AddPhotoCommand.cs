using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Domain.Enums;

namespace IssueManagement.Application.UseCases.Photos.Commands;

public sealed record AddPhotoCommand(Guid IssueId, string FileName, string ContentType, Stream FileStream, CorrectionStage CorrectionStage, string createdBy) : ICommand<IssuePhotoDto>;
