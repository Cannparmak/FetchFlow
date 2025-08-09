using Microsoft.EntityFrameworkCore;
using FetchFlow.Database.Service.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// MySQL DbContext konfigürasyonu
builder.Services.AddDbContext<ApiContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Internal API - Swagger sadece development'ta
}



app.UseAuthorization();


app.UseWelcomePage("/");

app.MapControllers();

app.Run();
