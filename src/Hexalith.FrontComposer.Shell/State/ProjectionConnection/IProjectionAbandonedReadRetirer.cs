namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Retires exact read admissions observed by reconciliation after its recovery deadline.</summary>
internal interface IProjectionAbandonedReadRetirer
{
    /// <summary>Releases only admissions observed by the expired epoch.</summary>
    void RetireAbandonedReads(long epoch);
}
