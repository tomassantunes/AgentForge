using System.Diagnostics;
using OpenAI.Chat;

namespace AgentForge.Shared;

public static class Utils
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

    public static void DebugPrint(string msg, bool print)
    {
        if (!print)
        {
            return;
        }
        
        Debug.Print($"[DEBUG] {DateTime.Now} - {msg}");
    }
}