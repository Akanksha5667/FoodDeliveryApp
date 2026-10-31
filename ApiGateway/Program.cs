
var builder = WebApplication.CreateBuilder(args);

// Register YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(
        builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// API Gateway health check
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Service = "ApiGateway"
});

// Forward requests to microservices
app.MapReverseProxy();

app.Run();
