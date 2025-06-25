using FC_Server.Services;
using FC_Server.DAL;
using FC_Server.Models;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// HttpClient עבור Google Maps
builder.Services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});


// Add services to the container.
builder.Services.AddControllers();

// הוספת שירות ה-LocationDbService ל-DI עם מחרוזת החיבור
string connectionString = builder.Configuration.GetConnectionString("myProjDB");


builder.Services.AddHostedService<AlertBackgroundService>(); // with this we will listen to the tzeva adom api all the time
builder.Services.AddHostedService<LocationCleanupService>(); // with this we will delete old user locations
builder.Services.AddScoped<ShelterAllocationService>();
builder.Services.AddScoped<UserLocationTrackingService>();
builder.Services.AddScoped<EmergencyAlertService>();

builder.Services.AddScoped<DBservices>();
builder.Services.AddScoped<DBservicesAlert>();
builder.Services.AddScoped<DBservicesShelter>();
builder.Services.AddScoped<DBservicesLocation>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("myProjDB");
    return new DBservicesLocation(connectionString);
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
