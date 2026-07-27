namespace OCAP.Core.ValueObjects;

public class UserIdentifier
{
    public string Value { get; private set; }
    public string Provider { get; private set; }

    public UserIdentifier(string value, string provider)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("User identifier value cannot be empty.");
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("User identifier provider cannot be empty.");

        Value = value;
        Provider = provider;
    }

    public override string ToString() => $"{Provider}:{Value}";
}
