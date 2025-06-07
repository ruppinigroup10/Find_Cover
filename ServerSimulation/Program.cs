using ServerSimulation.DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register Google Maps service with HttpClient
builder.Services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Increase to 5 minutes for testing
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 10, // Allow more concurrent connections
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true // For dev only
});

// Add logging
builder.Services.AddLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (true)
{
    app.UseSwagger();
    app.UseSwaggerUI(); //for windows

    // for mac
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty; // Access Swagger at the root URL
    });
}

app.UseHttpsRedirection();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthorization();

app.MapControllers();

app.Run();
