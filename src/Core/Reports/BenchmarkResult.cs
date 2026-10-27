namespace Core.Reports;

public sealed record BenchmarkResult(int Records, int Iterations, int ResultCount,
    double WithoutIntermediateListMs, double WithIntermediateListMs, bool ResultsEqual);
