namespace Core.Reports;

public sealed record DeferredExecutionResult(int Before, int After, int SnapshotCount, int AfterClear);
