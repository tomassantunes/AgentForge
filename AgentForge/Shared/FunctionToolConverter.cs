using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Schema;
using OpenAI.Chat;

namespace AgentForge.Shared;

public static class FunctionToolConverter
{
    public static ChatTool FunctionToTool(Delegate function)
    {
        var methodInfo = function.Method;
        var parameters = methodInfo.GetParameters();

        var properties = new Dictionary<string, object>();
        foreach (var param in parameters)
        {
            var paramType = param.ParameterType;
            var typeInfo = JsonSerializerOptions.Default.GetJsonSchemaAsNode(paramType);
            properties.Add(param.Name!, typeInfo);
        }

        var schema = new
        {
            type = "object",
            properties,
            required = parameters
                .Where(p => !p.IsOptional)
                .Select(p => p.Name)
                .ToList()
        };
        
        return ChatTool.CreateFunctionTool(
            methodInfo.Name,
            methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
            BinaryData.FromString(JsonSerializer.Serialize(schema)));
    }

    public static ChatTool FunctionToTool<TResult>(Func<TResult> function)
        => FunctionToTool((Delegate)function);

    public static ChatTool FunctionToTool<T, TResult>(Func<T, TResult> function)
        => FunctionToTool((Delegate)function);

    public static ChatTool FunctionToTool<T1, T2, TResult>(Func<T1, T2, TResult> function)
        => FunctionToTool((Delegate)function);

    public static ChatTool FunctionToTool<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function)
        => FunctionToTool((Delegate)function);
}