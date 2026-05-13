using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Infrastructure.Data;
using VehicleBookingSystem.Infrastructure.Repositories;
using VehicleBookingSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var dataDir    = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var passphrase = builder.Configuration["DataEncryption:Passphrase"] ?? "HealthwiseFleet-DefaultKey-2024!";
var encryptor  = new DataEncryptor(passphrase);
var dataCtx    = new JsonDataContext(dataDir, encryptor);

builder.Services.AddSingleton(dataCtx);
builder.Services.AddScoped<IVehicleRepository,  JsonVehicleRepository>();
builder.Services.AddScoped<IBookingRepository,  JsonBookingRepository>();
builder.Services.AddScoped<IAuditLogRepository, JsonAuditLogRepository>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

