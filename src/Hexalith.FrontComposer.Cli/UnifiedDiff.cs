using System.Text;

namespace Hexalith.FrontComposer.Cli;

internal static class UnifiedDiff {
    private const int ContextLines = 3;

    public static string Create(string relativePath, string original, string updated) {
        string[] oldLines = SplitLines(original);
        string[] newLines = SplitLines(updated);
        StringBuilder builder = new();
        _ = builder.Append("--- a/").Append(relativePath).Append('\n');
        _ = builder.Append("+++ b/").Append(relativePath).Append('\n');

        // Compute LCS-based hunks; each hunk has its own @@ -L1,N1 +L2,N2 @@ header.
        List<(int OldStart, int OldCount, int NewStart, int NewCount, List<(char Prefix, string Line)> Lines)> hunks =
            ComputeHunks(oldLines, newLines, ContextLines);
        if (hunks.Count == 0) {
            return builder.ToString();
        }

        foreach ((int OldStart, int OldCount, int NewStart, int NewCount, List<(char Prefix, string Line)> Lines) in hunks) {
            // Unified-diff convention: emit `0` only when the file is genuinely empty at that side
            // (count==0 AND start==0 — no preceding content). Otherwise emit a 1-based line number.
            int oldHeaderLine = OldCount == 0 && OldStart == 0 ? 0 : OldStart + 1;
            int newHeaderLine = NewCount == 0 && NewStart == 0 ? 0 : NewStart + 1;
            _ = builder.Append("@@ -")
                .Append(oldHeaderLine)
                .Append(',').Append(OldCount)
                .Append(" +")
                .Append(newHeaderLine)
                .Append(',').Append(NewCount)
                .Append(" @@\n");
            foreach ((char prefix, string line) in Lines) {
                // AC27: sanitize each diff line to strip ANSI escapes, control bytes, and DEL while
                // preserving \r\n\t so the hunk body remains line-structured.
                _ = builder.Append(prefix).Append(OutputSanitizer.SanitizeMultiLine(line, 1_000)).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static List<(int OldStart, int OldCount, int NewStart, int NewCount, List<(char, string)> Lines)> ComputeHunks(string[] oldLines, string[] newLines, int context) {
        // Identify diff segments via simple two-pointer walk — emit insert/delete groups with surrounding context.
        List<(char Op, int OldIdx, int NewIdx, string Line)> ops = DiffOps(oldLines, newLines);
        List<(int OldStart, int OldCount, int NewStart, int NewCount, List<(char, string)> Lines)> hunks = [];
        if (ops.Count == 0) {
            return hunks;
        }

        int i = 0;
        while (i < ops.Count) {
            // skip equals
            while (i < ops.Count && ops[i].Op == '=') {
                i++;
            }

            if (i >= ops.Count) {
                break;
            }

            int hunkStart = Math.Max(0, i - context);
            int j = i;
            while (j < ops.Count) {
                while (j < ops.Count && ops[j].Op != '=') {
                    j++;
                }

                int trailingContext = 0;
                while (j < ops.Count && ops[j].Op == '=' && trailingContext < context * 2) {
                    j++;
                    trailingContext++;
                }

                if (j >= ops.Count || ops[j].Op == '=') {
                    break;
                }
            }

            int hunkEnd = Math.Min(ops.Count, j);
            // trim equals beyond context lines
            int trailing = 0;
            while (hunkEnd > 0 && ops[hunkEnd - 1].Op == '=' && trailing < context) {
                hunkEnd--;
                trailing++;
                if (hunkEnd == 0) {
                    break;
                }
            }

            int oldStart = -1;
            int newStart = -1;
            int oldCount = 0;
            int newCount = 0;
            List<(char, string)> lines = [];
            for (int k = hunkStart; k < hunkEnd; k++) {
                (char op, int oldIdx, int newIdx, string line) = ops[k];
                if (oldStart < 0 && oldIdx >= 0) {
                    oldStart = oldIdx;
                }

                if (newStart < 0 && newIdx >= 0) {
                    newStart = newIdx;
                }

                switch (op) {
                    case '=':
                        lines.Add((' ', line));
                        oldCount++;
                        newCount++;
                        break;
                    case '-':
                        lines.Add(('-', line));
                        oldCount++;
                        break;
                    case '+':
                        lines.Add(('+', line));
                        newCount++;
                        break;
                }
            }

            if (oldStart < 0) {
                oldStart = 0;
            }

            if (newStart < 0) {
                newStart = 0;
            }

            hunks.Add((oldStart, oldCount, newStart, newCount, lines));
            i = hunkEnd;
            // consume any equals between hunks
            while (i < ops.Count && ops[i].Op == '=') {
                i++;
            }
        }

        return hunks;
    }

    private static List<(char Op, int OldIdx, int NewIdx, string Line)> DiffOps(string[] oldLines, string[] newLines) {
        // Simple O(N+M) walk anchored on line equality; conservative — emits all old as deletes and all new as inserts
        // when the lines diverge, then re-syncs at the next match. Sufficient for the small migration diffs this CLI
        // produces; not a full Myers diff.
        List<(char, int, int, string)> ops = [];
        int i = 0;
        int j = 0;
        while (i < oldLines.Length && j < newLines.Length) {
            if (string.Equals(oldLines[i], newLines[j], StringComparison.Ordinal)) {
                ops.Add(('=', i, j, oldLines[i]));
                i++;
                j++;
                continue;
            }

            int nextMatchOld = -1;
            int nextMatchNew = -1;
            for (int k = 1; k <= 32 && (i + k < oldLines.Length || j + k < newLines.Length); k++) {
                if (i + k < oldLines.Length && string.Equals(oldLines[i + k], newLines[j], StringComparison.Ordinal)) {
                    nextMatchOld = i + k;
                    break;
                }

                if (j + k < newLines.Length && string.Equals(oldLines[i], newLines[j + k], StringComparison.Ordinal)) {
                    nextMatchNew = j + k;
                    break;
                }
            }

            if (nextMatchOld > 0) {
                while (i < nextMatchOld) {
                    ops.Add(('-', i, -1, oldLines[i]));
                    i++;
                }
            }
            else if (nextMatchNew > 0) {
                while (j < nextMatchNew) {
                    ops.Add(('+', -1, j, newLines[j]));
                    j++;
                }
            }
            else {
                ops.Add(('-', i, -1, oldLines[i]));
                ops.Add(('+', -1, j, newLines[j]));
                i++;
                j++;
            }
        }

        while (i < oldLines.Length) {
            ops.Add(('-', i, -1, oldLines[i]));
            i++;
        }

        while (j < newLines.Length) {
            ops.Add(('+', -1, j, newLines[j]));
            j++;
        }

        return ops;
    }

    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
}
