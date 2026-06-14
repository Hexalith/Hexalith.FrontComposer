namespace Hexalith.FrontComposer.Cli;

internal sealed class CommandLineException(string message) : Exception(message);

internal sealed class CommandOptions {
    private static readonly HashSet<string> AllowedShortOptions = new(StringComparer.Ordinal) { "h" };

    private readonly Dictionary<string, List<string>> _values = new(StringComparer.Ordinal);

    private CommandOptions() {
    }

    public static CommandOptions Parse(IEnumerable<string> args) {
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
                if (equals == 0) {
                    throw new CommandLineException("Empty option name in '" + OutputSanitizer.Sanitize(token, 64) + "'.");
                }

                if (equals > 0) {
                    string key = name[..equals];
                    if (!IsValidLongName(key)) {
                        throw new CommandLineException("Invalid option name in '" + OutputSanitizer.Sanitize(token, 64) + "'.");
                    }

                    options.Add(key, name[(equals + 1)..]);
                    pending = null;
                }
                else {
                    if (!IsValidLongName(name)) {
                        throw new CommandLineException("Invalid option name '" + OutputSanitizer.Sanitize(token, 64) + "'.");
                    }

                    pending = name;
                }
                continue;
            }

            if (!positionalOnly && token.StartsWith('-') && token.Length > 1) {
                if (pending is not null) {
                    options.Add(pending, "true");
                }

                string shortName = token[1..];
                if (!AllowedShortOptions.Contains(shortName)) {
                    throw new CommandLineException("Unknown short option '" + OutputSanitizer.Sanitize(token, 32) + "'. Allowed: " + string.Join(", ", AllowedShortOptions.Select(x => "-" + x)) + ".");
                }

                pending = shortName switch {
                    "h" => "help",
                    _ => shortName,
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

        foreach ((string name, List<string> values) in options._values) {
            if (name == "_") {
                continue;
            }

            if (values.Count > 1) {
                throw new CommandLineException("Option '--" + name + "' was specified " + values.Count + " times. Specify each option at most once.");
            }
        }

        return options;
    }

    public bool Has(string name) => _values.ContainsKey(name);

    public string? Get(string name)
        => _values.TryGetValue(name, out List<string>? values) && values.Count > 0 ? values[0] : null;

    public string Get(string name, string defaultValue) => Get(name) ?? defaultValue;

    private static bool IsValidLongName(string name)
        => !string.IsNullOrEmpty(name) && name.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');

    private void Add(string name, string value) {
        if (!_values.TryGetValue(name, out List<string>? values)) {
            values = [];
            _values.Add(name, values);
        }

        values.Add(value);
    }
}
