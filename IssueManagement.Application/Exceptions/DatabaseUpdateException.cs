namespace IssueManagement.Application.Exceptions;
public class DatabaseUpdateException(string message, Exception exception) : Exception(message, exception);
