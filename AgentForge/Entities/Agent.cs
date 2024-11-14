using AgentForge.Shared;
using OpenAI.Chat;

namespace AgentForge.Entities;

public class Agent
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = "You are a helpful agent.";
    public string Model { get; set; } = "gpt-4o";
    public List<Delegate> Functions { get; set; } = [];
    public string ToolChoice { get; set; } = "auto";
    public bool ParallelToolCalls { get; set; } = false;

    public void AddFunction(Delegate function)
    {
        Functions.Add(function);
    }

    public void AddFunction<TResult>(Func<TResult> function)
        => AddFunction((Delegate)function);

    public void AddFunction<T, TResult>(Func<T, TResult> function)
        => AddFunction((Delegate)function);

    public void AddFunction<T1, T2, TResult>(Func<T1, T2, TResult> function)
        => AddFunction((Delegate)function);

    public void AddFunction<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function)
        => AddFunction((Delegate)function);

    public List<ChatTool> GetTools() => Functions
        .Select(FunctionToolConverter.FunctionToTool).ToList();
}