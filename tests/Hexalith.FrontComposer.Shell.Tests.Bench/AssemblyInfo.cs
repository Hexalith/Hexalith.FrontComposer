// Story 3-7 Pass-1 P16 — configure xUnit collection parallelism for the bench project so a
// sibling fact (e.g., a future AC5 add-on test) cannot starve `BenchmarkRunner.Run<>` mid-
// iteration. The bench is the project's reason to exist; serial execution is correct.

using Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
