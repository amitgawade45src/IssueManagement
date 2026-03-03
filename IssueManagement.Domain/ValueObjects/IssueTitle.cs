namespace IssueManagement.Domain.ValueObjects; 
public sealed record IssueTitle
{
    public const int MaxLength = 200;

    public string Value { get; }

    private IssueTitle(string value) => Value = value;

    public static IssueTitle Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value); 
        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new ArgumentException($"Issue title must not exceed {MaxLength} characters.");

        return new IssueTitle(trimmed);
    } 
    public override string ToString() => Value;
}
