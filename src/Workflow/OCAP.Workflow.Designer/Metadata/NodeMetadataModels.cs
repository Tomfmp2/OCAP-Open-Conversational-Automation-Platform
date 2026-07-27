namespace OCAP.Workflow.Designer.Metadata;

public record PropertyMetadata(
    string Name,
    string DisplayName,
    string Type,
    bool IsRequired,
    string DefaultValue,
    string Description,
    List<string>? Options = null
);

public record PortMetadata(
    string Name,
    string Type,
    bool IsRequired,
    string Description
);

public record NodeMetadata(
    string Type,
    string Category,
    string Name,
    string Description,
    string Icon,
    string Color,
    List<PropertyMetadata> Properties,
    List<PortMetadata> InputPorts,
    List<PortMetadata> OutputPorts,
    List<string> RequiredPermissions
);

public record CategoryMetadata(
    string Name,
    string DisplayName,
    string Icon,
    string Color,
    int Order
);
