using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using OpenAI.Chat;

namespace AgentForge.Shared;

public static class FunctionToolConverter
{
    private static readonly Dictionary<Type, string> TypeMap = new()
    {
        { typeof(string), "string" },
        { typeof(int), "int" },
        { typeof(float), "float" },
        { typeof(bool), "bool" },
        { typeof(List<>), "array" },
        { typeof(Dictionary<,>), "object" },
        { typeof(object), "null" },
    };

    private static string ResolveType(Type type)
    {
        if (TypeMap.TryGetValue(type, out var resolveType))
        {
            return resolveType;
        }

        if (type.IsEnum)
        {
            return "string";
        }

        return "object";
    }

    public static ChatTool FunctionToTool(Delegate function)
    {
        var methodInfo = function.Method;
        var parameters = methodInfo.GetParameters();

        var properties = new Dictionary<string, object>();
        foreach (var param in parameters)
        {
            var paramType = param.ParameterType;
            var typeInfo = new
            {
                type = ResolveType(paramType),
                description = param.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                @enum = paramType.IsEnum ? Enum.GetNames(paramType) : null
            };

            var cleanTypeInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(typeInfo))!
                .Where(kvp => kvp.Value is not null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            properties.Add(param.Name!, cleanTypeInfo);
        }

        var schema = new
        {
            type = "object",
            properties = properties,
            required = parameters
                .Where(p => !p.IsOptional)
                .Select(p => p.Name)
                .ToList()
        };

        var parametersJson = BinaryData.FromString(JsonSerializer.Serialize(schema));

        return ChatTool.CreateFunctionTool(
            methodInfo.Name,
            methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
            parametersJson);
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