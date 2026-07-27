namespace OCAP.Knowledge.Domain.ValueObjects;

public record DocumentVersion(
    int Major = 1,
    int Minor = 0,
    int Revision = 0
)
{
    public override string ToString() => $"{Major}.{Minor}.{Revision}";
}
