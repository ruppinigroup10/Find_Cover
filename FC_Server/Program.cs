using FC_Server.Services;
using System.Data.SqlClient; // ���� ����� �� ������ ��� �����

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
/*����� �� �����*/
builder.Services.AddScoped<LocationDbService>(provider =>
    new LocationDbService("Server=media.ruppin.ac.il;Database=igroup10_prod;User Id=igroup10;Password=igroup10_73888;Encrypt=False"));

builder.Services.AddHostedService<AlertBackgroundService>(); // with this we will listen to the tzeva adom api all the time
builder.Services.AddHostedService<LocationCleanupService>(); // with this we will delete old user locations


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
