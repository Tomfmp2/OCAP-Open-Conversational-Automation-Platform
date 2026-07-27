namespace OCAP.Agents.Domain.Entities;

// Modelo que representa una intencionalidad identificada en el mensaje del usuario.
public class Intent
{
    // Nombres estándar de intenciones predefinidas en el motor.
    public const string Unknown = "Unknown";
    public const string Greeting = "Greeting";
    public const string CreateReminder = "CreateReminder";
    public const string GetInformation = "GetInformation";
    public const string HumanSupport = "HumanSupport";

    // Nombre de la intención resuelta.
    public string Name { get; }

    // Grado de certeza o nivel de confianza (entre 0.0 y 1.0).
    public double Confidence { get; }

    // Parámetros o entidades extraídas del mensaje (ej. fecha, hora, tema).
    public IReadOnlyDictionary<string, string> Parameters { get; }

    public Intent(string name, double confidence = 1.0, Dictionary<string, string>? parameters = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Unknown : name.Trim();
        Confidence = Math.Clamp(confidence, 0.0, 1.0);
        Parameters = parameters ?? new Dictionary<string, string>();
    }

    public static Intent CreateUnknown() => new(Unknown, 0.0);
}
