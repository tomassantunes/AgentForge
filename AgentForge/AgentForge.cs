using System.Runtime.InteropServices;
using AgentForge.Entities;
using AgentForge.Shared;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;

namespace AgentForge;

public class AgentForge
{
    private static AgentForge? _instance;
    private static readonly Lock _lock = new Lock();
    public OpenAIClient Client { get; private set; }

    private AgentForge(string apiKey)
    {
        Client = new OpenAIClient(apiKey);
    }

    public static AgentForge GetInstance(string apiKey)
    {
        if (_instance != null) return _instance;
        lock (_lock)
        {
            _instance ??= new AgentForge(apiKey);
        }

        return _instance;
    }

    public async Task<ChatCompletion> GetChatCompletion(
        Agent agent,
        List<ChatMessage> messages,
        Dictionary<string, string> contextVariables,
        string modelOverride = "")
    {
        var completionOptions = new ChatCompletionOptions { ToolChoice = Utils.GetToolChoice(agent.ToolChoice) };

        foreach (var tool in agent.Functions.Select(Utils.FunctionToTool).AsParallel())
        {
            completionOptions.Tools.Add(tool);
        }

        return await Client
            .GetChatClient(modelOverride ?? agent.Model)
            .CompleteChatAsync(messages, completionOptions);
    }

    public async Task<Response> Run(
        Agent? agent,
        List<ChatMessage> messages,
        Dictionary<string, string> contextVariables = null,
        string modelOverride = "",
        int maxTurns = int.MaxValue,
        bool executeTools = true)
    {
        var activeAgent = agent;
        contextVariables ??= new Dictionary<string, string>();
        var history = new List<ChatMessage>(messages);
        var initLen = messages.Count;

        while (history.Count - initLen < maxTurns && activeAgent != null)
        {
            var completion = await GetChatCompletion(activeAgent, history, contextVariables, modelOverride);
            var message = new AssistantChatMessage(completion.Content.FirstOrDefault()?.Text ?? "");
            history.Add(message);

            if (message.ToolCalls is null || !executeTools)
            {
                break;
            }

            var partialResponse = HandleToolCalls(
                message.ToolCalls.ToList(),
                activeAgent.Functions,
                contextVariables);
            history.AddRange(partialResponse.Messages.Cast<ToolChatMessage>());
            contextVariables = contextVariables.Concat(partialResponse.ContextVariables)
                .ToDictionary(k => k.Key, v => v.Value);

            if (partialResponse.Agent is not null)
            {
                activeAgent = partialResponse.Agent;
            }
        }

        return new Response
        {
            Messages = history.Skip(initLen).ToList(),
            Agent = activeAgent,
            ContextVariables = contextVariables
        };
    }

    private Response HandleToolCalls(
        List<ChatToolCall> toolCalls,
        List<Func<object>> functions,
        Dictionary<string, string> contextVariables)
    {
        var functionMap = functions.ToDictionary(f => f.Method.Name, f => f);
        var response = new Response
        {
            Messages = new List<ChatMessage>(),
            ContextVariables = new Dictionary<string, string>()
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
            if (function.Method.GetParameters().Select(p => p.Name).Contains("context_variables"))
            {
                args!["context_variables"] = contextVariables;
            }

            var result = HandleFunctionCall(function.DynamicInvoke(args!.Values.ToArray()));
            response.Messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, result.Value));
            response.ContextVariables = contextVariables.Concat(result.ContextVariables)
                .ToDictionary(k => k.Key, v => v.Value);

            if (result.Agent is not null)
            {
                response.Agent = result.Agent;
            }
        }

        return response;
    }

    private Result HandleFunctionCall(object? result)
    {
        if (result is Result resultObj)
        {
            return resultObj;
        }
        else if (result is Agent agent)
        {
            return new Result
            {
                Agent = agent,
                Value = JsonConvert.SerializeObject(new { assistant = agent.Name })
            };
        }
        else
        {
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