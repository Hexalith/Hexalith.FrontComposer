namespace Hexalith.FrontComposer.Testing;

internal sealed record TestCommandConfiguration(TestCommandOutcome Outcome, string RejectionReason, string RejectionResolution);
