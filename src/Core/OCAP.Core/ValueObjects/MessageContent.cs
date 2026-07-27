namespace OCAP.Core.ValueObjects;

public class MessageContent
{
    public string Value { get; private set; }

    public MessageContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Message content cannot be empty.");
        }
        
        int maxLength = 4096;
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Message content exceeds the maximum length of {maxLength}.");
        }
        
        Value = value;
    }

    public override string ToString() => Value;
}
