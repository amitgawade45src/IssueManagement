namespace IssueManagement.Domain.ValueObjects;
 
public sealed record IssueDescription
{
    public const int MaxLength = 4000;
    public string Value { get; }
    private IssueDescription(string value) => Value = value;
    public static IssueDescription Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new ArgumentException($"Issue description must not exceed {MaxLength} characters.");

        return new IssueDescription(trimmed);
    }
    public override string ToString() => Value;
}
