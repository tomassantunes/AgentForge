namespace AgentForge.Entities;

public class Result
{
    public string Value { get; set; } = string.Empty;
    public Agent? Agent { get; set; } = null;
    public Dictionary<string, string> ContextVariables { get; set; } = new Dictionary<string, string>();
}