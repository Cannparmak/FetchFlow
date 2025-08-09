using Hangfire;
using FetchFlow.Worker.Service.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add ASP.NET Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Hangfire InMemory configuration (temporary for testing)
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

// Hangfire server
builder.Services.AddHangfireServer();

// HTTP Client for service communication
builder.Services.AddHttpClient();

// Register job services
builder.Services.AddTransient<KAPJob>();

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    // Hangfire Dashboard (development only)
    app.UseHangfireDashboard("/hangfire");
}


app.UseAuthorization();


app.UseWelcomePage("/");

app.MapControllers();

// Schedule recurring jobs
RecurringJob.AddOrUpdate<KAPJob>(
    "sync-kap-companies-hourly",
    job => job.SyncKapCompaniesAsync(),
    "0 * * * *"); // Every hour

app.Run();
