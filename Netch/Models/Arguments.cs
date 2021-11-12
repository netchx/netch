using Netch.Utils;

namespace Netch.Models;

public static class Arguments
{
    public static string Format(IEnumerable<object?> a)
    {
        var arguments = a.ToList();
        if (arguments.Count % 2 != 0)
            throw new FormatException("missing last argument value");

        var tokens = new List<string>();

        for (var i = 0; i < arguments.Count; i += 2)
        {
            var keyObj = arguments[i];
            var valueObj = arguments[i + 1];

            if (keyObj is not string key)
                throw new FormatException($"argument key at array index {i} is not string");

            switch (valueObj)
            {
                case SpecialArgument.Flag:
                    tokens.Add(key);
                    break;
                case null:
                case string value when value.IsNullOrWhiteSpace():
                    continue;
                default:
                    tokens.Add(key);
                    tokens.Add(valueObj.ToString()!);
                    break;
            }
        }

        return string.Join(' ', tokens);
    }
}

public enum SpecialArgument
{
    Flag
}