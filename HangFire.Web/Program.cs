using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddHangfire(config =>
{
    var connectionsString = builder.Configuration.GetConnectionString("HangFireConnection");

    config.UseSqlServerStorage(connectionsString);
    config.UseColouredConsoleLogProvider();
});

builder.Services.AddHangfireServer();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
