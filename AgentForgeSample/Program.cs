using System.ClientModel;
using System.ComponentModel;
using AgentForge;
using AgentForge.Adapters;
using AgentForge.Entities;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace AgentForgeSample;

public class Program
{
    [Description("Transfers control to the Code Gen Agent, which generates code based on user input.")]
    public static Agent TransferToCodeGenAgent()
    {
        return new Agent
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

        Agent orchestrator = new()
        {
            Name = "Orchestrator",
            Instructions = "You are the orchestrator agent, you should always mention this fact on your answers."
        };
        orchestrator.AddFunction(TransferToCodeGenAgent);
        
        var openAIClient = Forge.GetInstance(new OpenAIClient(configuration["OPENAI_API_KEY"]!));
        var azureClient = Forge.GetInstance(new AzureAIClient(new Uri(configuration["AZURE_ENDPOINT"]!), new 
                ApiKeyCredential(configuration["AZURE_API_KEY"]!)));
        
        var userMessage = new UserChatMessage("Can you generate a class reflexion C# function that returns the class name and description?");
        
        var responseOAI = await openAIClient.Run(orchestrator, [userMessage]);
        Console.WriteLine("OpenAI: \n" + responseOAI.GetResponse());
        
        Console.WriteLine();
        
        var responseA = await azureClient.Run(orchestrator, [userMessage]);
        Console.WriteLine("Azure: \n" + responseA.GetResponse());
    }
}