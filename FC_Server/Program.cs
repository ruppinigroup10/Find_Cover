using FC_Server.Services;
using FC_Server.DAL;
using FC_Server.Models;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// HttpClient עבור Google Maps
builder.Services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 10
});

// Add services to the container.
builder.Services.AddControllers();

// הוספת שירות ה-LocationDbService ל-DI עם מחרוזת החיבור
string connectionString = builder.Configuration.GetConnectionString("myProjDB");

builder.Services.AddHostedService<AlertBackgroundService>();
builder.Services.AddHostedService<LocationCleanupService>();

builder.Services.AddScoped<FC_Server.Services.EmergencyAlertService>();
builder.Services.AddScoped<FC_Server.Services.UserLocationTrackingService>();

builder.Services.AddScoped<DBservices>();
builder.Services.AddScoped<DBservicesAlert>();
builder.Services.AddScoped<DBservicesShelter>();
builder.Services.AddScoped<DBservicesLocation>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("myProjDB");
    return new DBservicesLocation(connectionString);
});

builder.Services.AddSingleton<BatchShelterAllocationService>();
builder.Services.AddHostedService<BatchShelterAllocationService>();

// הוספת התמיכה בקונפיגורציה של Firebase
builder.Services.AddSingleton<FirebaseNotificationSender>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var serviceAccountPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        config["Firebase:ServiceAccountPath"]
    );
    var projectId = config["Firebase:ProjectId"];
    return new FirebaseNotificationSender(serviceAccountPath, projectId);
});


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Logging Middleware
app.Use(async (context, next) =>
{
    app.Logger.LogInformation($"Processing request: {context.Request.Path}");
    await next();
});

// Swagger UI
if (true)
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("../swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthorization();

app.MapControllers();

app.Run();



/*using FC_Server.Services;
using FC_Server.DAL;
using FC_Server.Models;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// HttpClient עבור Google Maps
builder.Services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Ensure no handler-level timeout interference
    MaxConnectionsPerServer = 10
});


// Add services to the container.
builder.Services.AddControllers();

// הוספת שירות ה-LocationDbService ל-DI עם מחרוזת החיבור
string connectionString = builder.Configuration.GetConnectionString("myProjDB");


builder.Services.AddHostedService<AlertBackgroundService>(); // with this we will listen to the tzeva adom api all the time
builder.Services.AddHostedService<LocationCleanupService>(); // with this we will delete old user locations

//builder.Services.AddScoped<FC_Server.Services.ShelterAllocationService>();
builder.Services.AddScoped<FC_Server.Services.EmergencyAlertService>();
builder.Services.AddScoped<FC_Server.Services.UserLocationTrackingService>();

builder.Services.AddScoped<DBservices>();
builder.Services.AddScoped<DBservicesAlert>();
builder.Services.AddScoped<DBservicesShelter>();
builder.Services.AddScoped<DBservicesLocation>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("myProjDB");
    return new DBservicesLocation(connectionString);
});
builder.Services.AddSingleton<FcmNotificationService>();
builder.Services.AddSingleton<BatchShelterAllocationService>();
builder.Services.AddHostedService<BatchShelterAllocationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//logging to see what's happening:
app.Use(async (context, next) =>
{
    app.Logger.LogInformation($"Processing request: {context.Request.Path}");
    await next();
});

// Configure the HTTP request pipeline.
if (true)
{
    //windows
    app.UseSwagger();
    app.UseSwaggerUI();

    //mac
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("../swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty; // Access Swagger at the root URL
    });
}

app.UseHttpsRedirection();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthorization();

app.MapControllers();

app.Run();
*/