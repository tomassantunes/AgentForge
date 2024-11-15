using AgentForge;
using AgentForge.Entities;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace AgentForgeSample;

public class Program
{
    public static Agent TransferToCodeGenAgent()
    {
        return new()
        {
            Name = "Code Gen Agent",
            Instructions =
                "You are a code generating assistant. Your task is to generate code based on the user's input."
        };
    }
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var client = Forge.GetInstance(configuration["OPENAI_API_KEY"]!);
        Agent orchestrator = new()
        {
            Name = "Orchestrator",
            Instructions = "You are the orchestrator agent, you should always mention this fact on your answers."
        };
        orchestrator.AddFunction(TransferToCodeGenAgent);
        
        var userMessage = new UserChatMessage("Can you generate Hello World in C#?");
        var response = await client.Run(orchestrator, [userMessage]);
        
        Console.WriteLine(response.GetResponse());
    }
}