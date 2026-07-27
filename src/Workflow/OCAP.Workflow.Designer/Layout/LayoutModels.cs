namespace OCAP.Workflow.Designer.Layout;

public record ViewportState(
    double X,
    double Y,
    double Zoom
);

public record LayoutState(
    ViewportState Viewport,
    List<string> SelectedNodeIds,
    List<string> SelectedEdgeIds
);
