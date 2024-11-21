using LazyAPI.Commands;
using MonoMod.Utils;
using Rests;
using System.Reflection;
using TShockAPI;

namespace LazyAPI;

public static class RestHelper
{
    private static RestCommandD ParseCommand(MethodInfo method)
    {
        var @params = method.GetParameters();
        var n = @params.Length;
        var parsers = new CommandParser.Parser[n - 1];
        var names = new string[n - 1];
        var errors = new RestObject[n - 1];
        for (var i = 1; i < n; ++i)
        {
            var parser = CommandParser.GetParser(@params[i].ParameterType);
            parsers[i - 1] = parser;
            names[i - 1] = @params[i].Name!;
            errors[i - 1] = new RestObject("400") { Error = $"Bad input for parameter: {CommandParser.GetFriendlyName(@params[i].ParameterType)} {names[i - 1]}" };
        }

        var @delegate = method.CreateFastDelegate();

        return arg =>
        {
            var args = new object?[n];
            args[0] = arg;
            for (var i = 1; i < n; ++i)
            {
                if (!parsers[i - 1](arg.Parameters[names[i - 1]], out args[i]))
                {
                    return errors[i - 1];
                }
            }

            return @delegate(null, args);
        };
    }

    internal static void Register(Type type, string name, LazyPlugin plugin)
    {
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            var parser = ParseCommand(method);
            if (parser == null)
            {
                continue;
            }
            TShock.RestApi.Register(new SecureRestCommand($"/{name}/{method.Name}", parser,
                method.GetCustomAttributes<Permission>().Select(p => p.Name)
                    .Concat(method.GetCustomAttributes<PermissionsAttribute>().Select(p => p.perm)).ToArray()));
            Console.WriteLine(GetString($"[{plugin.Name}] rest endpoint registered: /{name}/{method.Name}"));
        }
    }
}