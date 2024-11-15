using OpenAI.Chat;

namespace AgentForge.Adapters;

public class OpenAIClient(string apiKey) : IAIClient
{
    private readonly OpenAI.OpenAIClient _client = new(apiKey);

    public async Task<ChatCompletion> CompleteChatAsync(string model, List<ChatMessage> messages, ChatCompletionOptions 
            options)
    {
        return await _client.GetChatClient(model).CompleteChatAsync(messages, options);
    }
}