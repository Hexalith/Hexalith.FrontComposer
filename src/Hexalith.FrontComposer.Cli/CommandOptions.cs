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
        bool positionalOnly = false;
        foreach (string token in args) {
            if (!positionalOnly && token == "--") {
                if (pending is not null) {
                    options.Add(pending, "true");
                    pending = null;
                }

                positionalOnly = true;
                continue;
            }

            if (!positionalOnly && token.StartsWith("--", StringComparison.Ordinal)) {
                if (pending is not null) {
                    options.Add(pending, "true");
                }

                string name = token[2..];
                int equals = name.IndexOf('=', StringComparison.Ordinal);
                if (equals >= 0) {
                    options.Add(name[..equals], name[(equals + 1)..]);
                    pending = null;
                }
                else {
                    pending = name;
                }
                continue;
            }

            if (!positionalOnly && token.StartsWith("-", StringComparison.Ordinal) && token.Length > 1) {
                if (pending is not null) {
                    options.Add(pending, "true");
                }

                pending = token[1..] switch {
                    "h" => "help",
                    _ => token[1..],
                };
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
        => _values.TryGetValue(name, out List<string>? values) && values.Count == 1 ? values[0] : null;

    public string Get(string name, string defaultValue) => Get(name) ?? defaultValue;

    public bool HasDuplicate(string name)
        => _values.TryGetValue(name, out List<string>? values) && values.Count > 1;

    private void Add(string name, string value)
    {
        if (!_values.TryGetValue(name, out List<string>? values)) {
            values = [];
            _values.Add(name, values);
        }

        values.Add(value);
    }
}
