using AgentForge.Adapters;
using AgentForge.Entities;
using AgentForge.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;

namespace AgentForge;

public class Forge
{
    private static Forge? _instance;
    private IAIClient Client { get; }

    private Forge(IAIClient client)
    {
        Client = client;
    }

    public static Forge GetInstance(IAIClient client)
    {
        if (_instance is not null) return _instance;

        _instance ??= new Forge(client);

        return _instance;
    }

    public async Task<Response> Run(
        Agent? agent,
        List<ChatMessage> messages,
        string modelOverride = "",
        int maxTurns = int.MaxValue,
        bool executeTools = true,
        bool debug = false)
    {
        var activeAgent = agent;
        var history = new List<ChatMessage>(messages);
        var initLen = messages.Count;

        Utils.DebugPrint("Starting agent interaction loop...", debug);
        
        while (history.Count - initLen < maxTurns && activeAgent != null)
        {
            var completion = await GetChatCompletion(activeAgent, history, modelOverride, debug);

            if (completion.ToolCalls is null || completion.ToolCalls.Count == 0 || !executeTools)
            {
                Utils.DebugPrint("Interaction loop finished due to no more tool calls.", debug);
                history.Add(new AssistantChatMessage(completion.Content.FirstOrDefault()?.Text ?? ""));
                break;
            }

            history.Add(new AssistantChatMessage(completion.ToolCalls));
            
            var partialResponse = HandleToolCalls(
                completion.ToolCalls.ToList(),
                activeAgent.Functions,
                debug);
            history.AddRange(partialResponse.Messages.Cast<ToolChatMessage>());

            if (partialResponse.Agent is not null)
            {
                Utils.DebugPrint($"Switching to agent '{partialResponse.Agent.Name}'.", debug);
                activeAgent = partialResponse.Agent;
            }
        }

        return new Response
        {
            Messages = history.Skip(initLen).ToList(),
            Agent = activeAgent,
        };
    }
    
    private async Task<ChatCompletion> GetChatCompletion(
        Agent agent,
        List<ChatMessage> messages,
        string modelOverride = "",
        bool debug = false)
    {
        var completionOptions = new ChatCompletionOptions();

        foreach (var tool in agent.Functions.AsParallel())
        {
            var func = FunctionToolConverter.FunctionToTool(tool);
            completionOptions.Tools.Add(func);
        }

        if (completionOptions.Tools.Count > 0)
        {
            completionOptions.ToolChoice = Utils.GetToolChoice(agent.ToolChoice);
            completionOptions.AllowParallelToolCalls = agent.ParallelToolCalls;
        }
        
        Utils.DebugPrint($"Getting chat completion for '{string.Join(", ", messages
            .Where(m => m.Content.Count > 0)
            .Select(m => m.Content.FirstOrDefault()!.Text).ToList())}'.", debug);

        return await Client.CompleteChatAsync(
            modelOverride.Length > 0 ? modelOverride : agent.Model,
            messages,
            completionOptions);
    }

    private Response HandleToolCalls(
        List<ChatToolCall> toolCalls,
        List<Delegate> functions,
        bool debug = false)
    {
        var functionMap = functions.ToDictionary(f => f.Method.Name, f => f);
        var response = new Response
        {
            Messages = [],
        };

        foreach (var toolCall in toolCalls)
        {
            var name = toolCall.FunctionName;
            
            Utils.DebugPrint($"Executing function {name}.", debug);
            
            if (!functionMap.TryGetValue(name, out var function))
            {
                response.Messages.Add(ChatMessage
                    .CreateToolMessage(toolCall.Id, $"Error: Tool {name} not found."));
                continue;
            }

            var methodParams = function.Method.GetParameters();
            var functionArgs = new object[methodParams.Length];

            var jObject = JObject.Parse(toolCall.FunctionArguments.ToString());
            for (var i = 0; i < methodParams.Length; i++)
            {
                var paramType = methodParams[i].ParameterType;
                var paramName = methodParams[i].Name!;

                if (!jObject.ContainsKey(paramName))
                {
                    throw new ArgumentException(
                        $"Required argument '{paramName}' not found in tool call function arguments."); 
                }
                
                functionArgs[i] = jObject[paramName]!.ToObject(paramType)!;
            }

            var result = HandleFunctionCall(function.DynamicInvoke(functionArgs));
            response.Messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, result.Value));

            if (result.Agent is not null)
            {
                response.Agent = result.Agent;
            }
        }

        return response;
    }

    private Result HandleFunctionCall(object? result)
    {
        switch (result)
        {
            case Result resultObj:
                return resultObj;
            case Agent agent:
                return new Result
                {
                    Agent = agent,
                    Value = JsonConvert.SerializeObject(new { assistant = agent.Name })
                };
            default:
                try
                {
                    return new Result
                    {
                        Value = JsonConvert.SerializeObject(result)
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
        }
    }
}