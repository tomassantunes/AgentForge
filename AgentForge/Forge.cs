using AgentForge.Adapters;
using AgentForge.Entities;
using AgentForge.Shared;
using Azure.AI.OpenAI;
using Newtonsoft.Json;
using OpenAI;
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
            completionOptions.AllowParallelToolCalls = agent.ParallelToolCalls;
        }

        return await Client.CompleteChatAsync(
            modelOverride.Length > 0 ? modelOverride : agent.Model,
            messages,
            completionOptions);
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
            var methodParams = function.Method.GetParameters();
            var functionArgs = args!.Values.ToArray();
            for (var i = 0; i < methodParams.Length; i++)
            {
                var paramType = methodParams[i].ParameterType;
                functionArgs[i] = Convert.ChangeType(args![methodParams[i].Name!], paramType);
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