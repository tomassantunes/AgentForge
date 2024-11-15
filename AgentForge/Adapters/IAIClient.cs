using OpenAI.Chat;

namespace AgentForge.Adapters;

public interface IAIClient
{
    Task<ChatCompletion> CompleteChatAsync(string model, List<ChatMessage> messages, ChatCompletionOptions options);
}