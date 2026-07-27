namespace OCAP.Workflow.Designer.Models;

public class NodePosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public NodePosition() { }
    public NodePosition(double x, double y) { X = x; Y = y; }
}

public class VisualNode
{
    public string Id { get; set; } = string.Empty;
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public NodePosition Position { get; set; } = new NodePosition();
    public string ConfigurationJson { get; set; } = "{}";
    public List<string> InputPorts { get; set; } = new();
    public List<string> OutputPorts { get; set; } = new();
}

public class VisualEdge
{
    public string Id { get; set; } = string.Empty;
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string FromPort { get; set; } = string.Empty;
    public string ToPort { get; set; } = string.Empty;
    public string ConditionExpression { get; set; } = string.Empty;
}

public class VisualWorkflowGraph
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<VisualNode> Nodes { get; set; } = new();
    public List<VisualEdge> Edges { get; set; } = new();
}
