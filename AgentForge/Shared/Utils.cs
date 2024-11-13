using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using OpenAI.Chat;

namespace AgentForge.Shared;

public class Utils
{
    public static ChatTool FunctionToTool(Func<object> function)
    {
        var typeMap = new Dictionary<Type, string>
        {
            { typeof(string), "string" },
            { typeof(int), "int" },
            { typeof(float), "float" },
            { typeof(bool), "bool" },
            { typeof(List<>), "array" },
            { typeof(Dictionary<,>), "object" },
            { typeof(object), "null" },
        };

        var parameters = BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = function.Method.GetParameters()
                .ToDictionary(p => p.Name, p => new
                {
                    type = typeMap.GetValueOrDefault(p.ParameterType, "string")
                }),
            required = function.Method.GetParameters()
                                   .Where(p => !p.IsOptional)
                                   .Select(p => p.Name)
                                   .ToList()
        }));
        
        return ChatTool.CreateFunctionTool(
            function.Method.Name,
            function.Method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "", 
            parameters);
    }

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