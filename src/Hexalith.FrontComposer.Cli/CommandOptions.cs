namespace Hexalith.FrontComposer.Cli;

internal sealed class CommandOptions
{
    private readonly Dictionary<string, List<string>> _values = new(StringComparer.Ordinal);

    private CommandOptions()
    {
    }

    public static CommandOptions Parse(IEnumerable<string> args)
    {
        CommandOptions options = new();
        string? pending = null;
        foreach (string token in args) {
            if (token.StartsWith("--", StringComparison.Ordinal)) {
                if (pending is not null) {
                    options.Add(pending, "true");
                }

                pending = token[2..];
                continue;
            }

            if (pending is null) {
                options.Add("_", token);
            }
            else {
                options.Add(pending, token);
                pending = null;
            }
        }

        if (pending is not null) {
            options.Add(pending, "true");
        }

        return options;
    }

    public bool Has(string name) => _values.ContainsKey(name);

    public string? Get(string name)
        => _values.TryGetValue(name, out List<string>? values) ? values.LastOrDefault() : null;

    public string Get(string name, string defaultValue) => Get(name) ?? defaultValue;

    private void Add(string name, string value)
    {
        if (!_values.TryGetValue(name, out List<string>? values)) {
            values = [];
            _values.Add(name, values);
        }

        values.Add(value);
    }
}
