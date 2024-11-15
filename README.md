# Agent Forge
Multi-Agent framework for C# .NET inspired by OpenAI Swarm

## What is it
Agent Forge is a C# library that facilitates the creation of multi-agent systems for your application utilizing OpenAI models. It efficiently transfers communication between agents until a response to your query is received.

## Install

## Usage
```cs
using AgentForge;
using AgentForgeEntites;

namespace Readme;

public class Program
{
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
        var client = Forge.GetInstance("openai_api_key");
        Agent orchestrator = new()
        {
            Name = "Orchestrator",
            Instructions  = "You are the orchestrator agent, your task is to transfer the user to the correct agent."
        }
        orchestrator.AddFunction(TransferToCodeGenAgent);

        var userMessage = new UserChatMessage("Can you generate Hello World in C#?");
        var response = await client.Run(orchestrator, [userMessage]);

        Console.WriteLine(response.GetResponse());
    }
}
```
```
Here is a simple C# program that outputs "Hello, World!" to the console:

\```csharp
using System;

class HelloWorld
{
    static void Main()
    {
        Console.WriteLine(\"Hello, World!\");
    }
}
\```

You can compile and run this program using a C# compiler, such as the one provided in the .NET SDK.
```
