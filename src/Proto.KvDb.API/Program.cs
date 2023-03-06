using Proto.KvDb.API;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureAppSettings();
builder.ConfigureKestrel();
builder.AddCustomSerilog();
builder.AddCustomSwagger();
builder.AddCustomHealthChecks();
builder.AddActorSystem();
builder.AddApiConfiguration();

var app = builder.Build();
app.UseCustomSwagger();
app.UseRouting();
app.UseCustomMapHealthChecks();
app.MapEndpoints();
app.RunApplication();