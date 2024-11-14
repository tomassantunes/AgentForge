using OpenAI.Chat;

namespace AgentForge.Entities;

public class Response 
{
    public List<ChatMessage> Messages { get; set; } = [];
    public Agent? Agent { get; set; } = new();
}