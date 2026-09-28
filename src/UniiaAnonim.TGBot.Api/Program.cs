using UniiaAnonim.TGBot.Api.Extensions;
using UniiaAnonim.TGBot.Application.Extensions;
using UniiaAnonim.TGBot.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();
builder.Services
    .AddPresentation()
    .AddInfrastructureServices(builder.Configuration)
    .AddApplicationServices();

var app = builder.Build();

await app.Services.UseInfrastructureDatabaseAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCustomHealthChecks();
app.MapControllers();

await app.RunAsync();