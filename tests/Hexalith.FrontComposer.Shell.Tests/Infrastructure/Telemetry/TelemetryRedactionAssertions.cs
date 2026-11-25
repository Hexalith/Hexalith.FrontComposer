using System.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

internal static class TelemetryRedactionAssertions
{
    public static CapturedLogEntry AssertLog(IEnumerable<CapturedLogEntry> entries, int eventId, params string[] sensitive)
    {
        CapturedLogEntry entry = entries.Where(entry => entry.EventId.Id == eventId).ShouldHaveSingleItem();
        entry.Exception.ShouldBeNull();
        AssertLogs(entries, sensitive);
        return entry;
    }

    public static void AssertLogs(IEnumerable<CapturedLogEntry> entries, params string[] sensitive)
    {
        foreach (CapturedLogEntry entry in entries)
        {
            foreach (string value in sensitive)
            {
                entry.Message.ShouldNotContain(value);
                entry.Exception?.ToString().ShouldNotContain(value);
                foreach (KeyValuePair<string, object?> field in entry.State)
                {
                    field.Key.ShouldNotContain(value);
                    field.Value?.ToString()?.ShouldNotContain(value);
                }
            }
        }
    }

    public static Activity AssertActivity(ActivityCapture capture, ActivityTraceId traceId, string operationName, params string[] sensitive)
    {
        Activity activity = capture.Single(operationName, activity => activity.TraceId == traceId);
        foreach (string value in sensitive)
        {
            activity.DisplayName.ShouldNotContain(value);
            activity.StatusDescription?.ShouldNotContain(value);
            foreach (KeyValuePair<string, object?> tag in activity.TagObjects)
            {
                tag.Key.ShouldNotContain(value);
                tag.Value?.ToString()?.ShouldNotContain(value);
            }
        }

        return activity;
    }
}
