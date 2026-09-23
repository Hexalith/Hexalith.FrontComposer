using Hexalith.EventStore.DomainService;
using Hexalith.FrontComposer.CounterFixture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<FixtureProjectionRows>();
builder.AddEventStoreDomainService();

WebApplication app = builder.Build();
app.UseEventStoreDomainService();
app.Run();
