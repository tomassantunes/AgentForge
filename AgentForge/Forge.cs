using AgentForge.Entities;
using AgentForge.Shared;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;

namespace AgentForge;

public class Forge
{
    private static Forge? _instance;
    private OpenAIClient Client { get; }

    private Forge(string apiKey)
    {
        Client = new OpenAIClient(apiKey);
    }

    public static Forge GetInstance(string apiKey)
    {
        if (_instance != null) return _instance;
        
        _instance ??= new Forge(apiKey);

        return _instance;
    }

    private async Task<ChatCompletion> GetChatCompletion(
        Agent agent,
        List<ChatMessage> messages,
        string modelOverride = "")
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
        }
        
        return await Client
            .GetChatClient(modelOverride.Length > 0 ? modelOverride : agent.Model)
            .CompleteChatAsync(messages, completionOptions);
    }

    public async Task<Response> Run(
        Agent? agent,
        List<ChatMessage> messages,
        string modelOverride = "",
        int maxTurns = int.MaxValue,
        bool executeTools = true)
    {
        var activeAgent = agent;
        var history = new List<ChatMessage>(messages);
        var initLen = messages.Count;

        while (history.Count - initLen < maxTurns && activeAgent != null)
        {
            var completion = await GetChatCompletion(activeAgent, history, modelOverride);

            if (completion.ToolCalls is null || completion.ToolCalls.Count == 0 || !executeTools)
            {
                history.Add(new AssistantChatMessage(completion.Content.FirstOrDefault()?.Text ?? ""));
                break;
            }

            history.Add(new AssistantChatMessage(completion.ToolCalls));
            
            var partialResponse = HandleToolCalls(
                completion.ToolCalls.ToList(),
                activeAgent.Functions);
            history.AddRange(partialResponse.Messages.Cast<ToolChatMessage>());

            if (partialResponse.Agent is not null)
            {
                activeAgent = partialResponse.Agent;
            }
        }

        return new Response
        {
            Messages = history.Skip(initLen).ToList(),
            Agent = activeAgent,
        };
    }

    private Response HandleToolCalls(
        List<ChatToolCall> toolCalls,
        List<Delegate> functions)
    {
        var functionMap = functions.ToDictionary(f => f.Method.Name, f => f);
        var response = new Response
        {
            Messages = [],
        };

        foreach (var toolCall in toolCalls)
        {
            var name = toolCall.FunctionName;
            if (!functionMap.ContainsKey(name))
            {
                response.Messages.Add(ChatMessage
                    .CreateToolMessage(toolCall.Id, $"Error: Tool {name} not found."));
                continue;
            }

            var args = JsonConvert
                .DeserializeObject<Dictionary<string, object>>(toolCall.FunctionArguments.ToString());
            var function = functionMap[name];

            var result = HandleFunctionCall(function.DynamicInvoke(args!.Values.ToArray()));
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