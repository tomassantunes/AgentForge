using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using OpenAI.Chat;

namespace AgentForge.Shared;

public class Utils
{
    public static ChatToolChoice GetToolChoice(string toolChoice)
    {
        switch (toolChoice)
        {
            case "auto":
                return ChatToolChoice.CreateAutoChoice();
            case "none":
                return ChatToolChoice.CreateNoneChoice();
            case "required":
                return ChatToolChoice.CreateRequiredChoice();
            default:
                return ChatToolChoice.CreateFunctionChoice(toolChoice);
        }
    }
}