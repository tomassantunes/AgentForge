using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace AgentForge.Adapters;

public class AzureAIClient(Uri endpoint, ApiKeyCredential apiKey) : IAIClient
{
    private readonly AzureOpenAIClient _client = new(endpoint, apiKey);
    
    public async Task<ChatCompletion> CompleteChatAsync(string model, List<ChatMessage> messages, ChatCompletionOptions 
            options)
    {
        return await _client.GetChatClient(model).CompleteChatAsync(messages, options);
    }
}