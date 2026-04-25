// Story 3-7 T2.2 — synthetic 1 000-candidate registry generator. Names are 4–38 char ASCII,
// mixed case, ~10 % dotted (e.g., "Domain.Counter.CounterView"). Seeded so the bench is
// reproducible run-to-run. Pass-1 P19 fix: removed the 24-char hard truncation that was
// producing partial-segment names like "Notification.Workflow.Pe"; the realistic upper
// bound for adopter projection FQNs comfortably exceeds 24 chars.

namespace Hexalith.FrontComposer.Shell.Tests.Bench;

internal static class SyntheticRegistry {
    private static readonly string[] _segmentPool = [
        "Counter", "Order", "Submit", "Increment", "Decrement", "Customer", "Invoice",
        "Line", "Item", "Total", "Status", "View", "Command", "Query", "Projection",
        "Domain", "Service", "Aggregate", "Event", "Notification", "Detail", "Summary",
        "Report", "Tenant", "User", "Role", "Permission", "Rule", "Policy", "Workflow",
    ];

    public static string[] Build(int count, int seed) {
        Random rng = new(seed);
        string[] result = new string[count];
        for (int i = 0; i < count; i++) {
            result[i] = NextName(rng);
        }

        return result;
    }

    private static string NextName(Random rng) {
        bool dotted = rng.Next(10) == 0;
        int segments = dotted ? rng.Next(2, 4) : 1;
        string[] parts = new string[segments];
        for (int s = 0; s < segments; s++) {
            string seg = _segmentPool[rng.Next(_segmentPool.Length)];
            if (rng.Next(2) == 0) {
                seg = seg.ToLowerInvariant();
            }

            parts[s] = seg;
        }

        string name = string.Join('.', parts);
        if (name.Length < 3) {
            name += "Vw";
        }

        return name;
    }
}
