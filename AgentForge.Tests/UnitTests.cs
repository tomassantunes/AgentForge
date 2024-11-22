using System.ClientModel;
using FluentAssertions;
using AgentForge;
using AgentForge.Adapters;
using AgentForge.Entities;
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
    
    [Fact]
    public void Get_instance_should_return_valid_azure_forge()
    {
        var agentForge = Forge.GetInstance(
            new AzureAIClient(new Uri("https://any.endpoint"), new ApiKeyCredential("any_apikey")));
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
    
    public class TestClass
    {
        private string testString { get; set; } = string.Empty;
        private string testString2 { get; set; } = string.Empty;
        private string testString3 { get; set; } = string.Empty;
    }

    [Fact]
    public void TypeToOutputSpec_should_return_valid_output_spec_if_given_valid_type()
    {
        var outputSpec = Utils.TypeToOutputSpec(typeof(TestClass), "test", true);
        outputSpec.Should().BeOfType<OutputSpec>();
        outputSpec.Name.Should().Be("test");
        outputSpec.Schema.Should().NotBeNull();
        outputSpec.Strict.Should().Be(true);
    }
}
