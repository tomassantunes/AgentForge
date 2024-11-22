using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using AgentForge.Entities;
using Newtonsoft.Json.Schema.Generation;
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

    public static OutputSpec TypeToOutputSpec(Type type, string name, bool strict = false)
    {
        JSchemaGenerator generator = new();
        var schema = generator.Generate(type);
        schema.AllowAdditionalProperties = false;
            
        return new OutputSpec
        {
            Name = name,
            Description = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
            Schema = BinaryData.FromString(schema.ToString()),
            Strict = strict
        };
    }
}