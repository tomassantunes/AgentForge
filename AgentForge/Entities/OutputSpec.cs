namespace AgentForge.Entities;

public class OutputSpec
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BinaryData Schema { get; set; } = new("");
    public bool Strict { get; set; }
}