namespace AgentForge.Entities;

public class Agent
{
   public string Name { get; set; } = string.Empty;
   public string Instructions { get; set; } = "You are a helpful agent.";
   public string Model { get; set; } = "gpt-4o";
   public List<Func<object>> Functions { get; set; } = new List<Func<object>>();
   public string ToolChoice { get; set; } = "auto";
   public bool ParallelToolCalls { get; set; } = false;
}