# Agent Forge
Multi-Agent framework for C# .NET inspired by OpenAI Swarm

## What is it
Agent Forge is a C# library that facilitates the creation of multi-agent systems for your application utilizing OpenAI models. It efficiently transfers communication between agents until a response to your query is received.

## Install

## Usage
```cs
using System.ClientModel;
using System.ComponentModel;
using OpenAI.Chat;

using AgentForge;
using AgentForge.Adapters;
using AgentForge.Entities;

namespace Readme;

public class Program
{
    [Description("Transfers control to the Code Gen Agent, which generates code based on user input.")]
    public static Agent TransferToCodeGenAgent()
    {
        return new()
        {
            Name = "Code Gen Agent",
            Instructions = "You are a code generating assistant. Your task is to generate code based on the user's input"
        };
    }

    public static async Task Main(string[] args)
    {
        // Using OpenAI
        var openAIClient = Forge.GetInstance(new OpenAIClient("api_key"]!));

        // Using Azure OpenAI
        var azureClient = Forge.GetInstance(new AzureAIClient(new Uri("azure_endpoint"), new 
                ApiKeyCredential("azure_api_key")));

        Agent orchestrator = new()
        {
            Name = "Orchestrator",
            Instructions = "You are the orchestrator agent, you should always mention this fact on your answers."
        };
        orchestrator.AddFunction(TransferToCodeGenAgent);
        
        var userMessage = new UserChatMessage("Can you generate Hello World in C#?");
        
        // Using OpenAI
        var responseOAI = await openAIClient.Run(orchestrator, [userMessage]);
        Console.WriteLine("OpenAI: \n" + responseOAI.GetResponse());
        
        // Using Azure OpenAI
        var responseA = await azureClient.Run(orchestrator, [userMessage]);
        Console.WriteLine("Azure: \n" + responseA.GetResponse());
    }
}
```
```
OpenAI: 
\```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello, World!");
    }
}
\```

Azure: 
\```csharp
using System;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}
\```
```
