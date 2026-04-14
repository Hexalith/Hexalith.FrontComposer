var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Counter_Web>("counter-web");

await builder.Build().RunAsync().ConfigureAwait(false);
