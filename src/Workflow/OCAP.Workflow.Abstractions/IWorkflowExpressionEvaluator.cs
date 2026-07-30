namespace OCAP.Workflow.Abstractions;

public interface IWorkflowExpressionEvaluator
{
    bool EvaluateBool(string expression, IDictionary<string, object> variables);
    object? Evaluate(string expression, IDictionary<string, object> variables);
    string Interpolate(string template, IDictionary<string, object> variables);
}
