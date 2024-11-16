# Agent Forge
Multi-Agent framework for C# .NET inspired by OpenAI Swarm

## Table of Contents
- [What is it](#what-is-it)
- [Features](#features)
- [Install](#install)
- [Usage](#usage)
- [Documentation](#documentation)
    - [Running Forge](#running-forge)
    - [Agents](#agents)
    - [Functions](#functions)
    - [Agent transfers](#agent-transfers)
    - [Utils](#utils)
- [Contributing](#contributing)

## What is it
Agent Forge is a C# library that facilitates the creation of multi-agent systems for your application utilizing OpenAI models. It efficiently transfers communication between agents until a response to your query is received.

## Features
- Easy integration with OpenAI and Azure OpenAI services.
- Flexible agent creation and orchestration.
- Multiple function types allowed for agent functions.
- Parallel tool calls.

## Install
```bash
dotnet add package AgentForge
```

## Usage
Below is a simple example of how to use AgentForge with both OpenAI and Azure in a C# application.
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

Output:
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

# Documentation

## Running Forge
The first step is to instantiate a Forge client with either an OpenAIClient or AzureOpenAIClient
```csharp
var client = Forge.GetInstance(new OpenAICLient("api_key"));
```

### `client.Run()`
Forge's `Run` method handles chat completions, agent execution, agent transfers, and can take multiple turns before 
returning to the user.

The `Run` method implements the following loop:
1. Get a completion from the active agent.
2. Execute tool calls and append results.
3. Switch agent if necessary.
4. If no new tool calls, exit and return the response.

#### Fields 
| Field             | Type                | Description                                                        |
|-------------------|---------------------|--------------------------------------------------------------------|
| **agent**         | `Agent`             | The initial agent to be executed.                                  |
| **messages**      | `List<ChatMessage>` | A list of messages.                                                |
| **modelOverride** | `string`            | (Optional) An optional string to override the agent defined model. |
| **maxTurns**      | `int`               | (Optional) Maximum number of turns in the conversation.            |
| **executeTools**  | `bool`              | (Optional) If the tool calls should be executed.                   |
| **debug**         | `bool`              | (Optional) Enables debug logging.                                  |

Once `client.Run()` is finished it will return a `Response` containing the completion finished state.

#### `Response` fields
| Field        | Type                | Description                    |
|--------------|---------------------|--------------------------------|
| **agent**    | `Agent`             | The last agent to be executed. |
| **messages** | `List<ChatMessage>` | A list of messages.            |

## Agents
An `Agent` is an encapsulation of a set of `Instructions` with a set of `Functions` (plus some other settings).

Agents can be used to perform specific tasks like get the current weather or generate/test code. They can also be 
used to execute a certain workflow with a set of instructions and functions that define this behavior.

### `Agent` fields
| Field            | Type             | Description                                                                          |
|------------------|------------------|--------------------------------------------------------------------------------------|
| **Name**         | `string`         | Defines the agent's name.                                                            |
| **Instructions** | `string`         | (Optional, "You are a helpful agent.") Defines a set of instructions for the agent.  |
| **Model**        | `string`         | (Optional, "gpt-4o") Defines the llm model to be used with this agent.               |
| **Functions**    | `List<Delegate>` | (Optional, []) List of *static* functions the agent has access to.                   |
| **ToolChoice**   | `string`         | (Optional, "auto") The tool choice for the agent.                                    |

## Functions
- Forge Agents can call C# functions directly, with or without parameters.
- Functions must be `static`.
- Functions can return `Agent`, `string`, `int`, `float`, `bool` and `object`.
- If a Function returns an `Agent`, the execution will be transferred to that `Agent`.

```csharp
[Description("Returns the sum of `a` and `b`.")]
public static string hello(string userName)
{
    return $"Hello, {userName}!";
}

var helloAgent = new Agent();
helloAgent.AddFunction(hello);
```

## `Agent` transfers
The execution can be transferred to another `Agent` by returning it in a `Function`.
```csharp
[Description("Transfers the execution to agent Greeter")]
public static Agent TransferToGreeter()
{
    return new Agent
    {
        Name = "Greeter Agent",
        Instructions = "You are a nice Agent. Your job is to greet users in the most friendly way possible"
    }
}
var orchestrator = new Agent();
orchestrator.AddFunction(TransferToGreeter);
```

## Utils

#### GetToolChoice(string)
GetToolChoice converts a string ("auto", "none", "required" or function name) into a valid *ChatToolChoice* type.

#### FunctionToolConverter
The purpose of this class is to convert functions into a valid *ChatTool*, it gets the name, description (if given) 
and parameters of a function.

# Contributing
We welcome contributions! Please follow these steps to contribute:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Make your changes.
4. Commit your changes (`git commit -am 'Add new feature'`).
5. Push to the branch (`git push origin feature-branch`).
6. Create a new Pull Request.
