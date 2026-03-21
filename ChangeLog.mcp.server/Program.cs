var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
                .WithHttpTransport()
                .WithResourcesFromAssembly()
                .WithPromptsFromAssembly()
                .WithToolsFromAssembly();

builder.Services.AddCors();

var app = builder.Build();

//app.UseHttpsRedirection();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.MapMcp();
app.MapGet("/api/healthz", () => "Healthy");

app.Run();
