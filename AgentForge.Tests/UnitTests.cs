using FluentAssertions;
using AgentForge;
using AgentForge.Adapters;
using AgentForge.Shared;
using OpenAI.Chat;

namespace AgentForge.Tests;

public class UnitTests
{
    [Fact]
    public void Get_instance_should_return_valid_openai_forge()
    {
        var agentForge = Forge.GetInstance(new OpenAIClient("any_apikey"));
        agentForge.Should().BeOfType<Forge>();
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("none")]
    [InlineData("required")]
    [InlineData("any_function")]
    public void GetToolChoice_should_return_valid_tool_choice_if_given_valid_input(string toolCall)
    {
        var toolChoice = Utils.GetToolChoice(toolCall);
        toolChoice.Should().BeOfType<ChatToolChoice>();
    }

    public string Test(string message)
    {
        return message + "hello";
    }
    
    [Fact]
    public void FunctionToTool_should_return_valid_chat_tool_if_given_valid_function()
    {
        var func = FunctionToolConverter.FunctionToTool<string, string>(Test);
        func.Should().BeOfType<ChatTool>();
    }
}
