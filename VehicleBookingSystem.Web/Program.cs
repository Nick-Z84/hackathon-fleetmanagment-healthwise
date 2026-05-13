using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Infrastructure.Data;
using VehicleBookingSystem.Infrastructure.Repositories;
using VehicleBookingSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ?? Encrypted JSON data store ?????????????????????????????????????????????
var dataDir    = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var passphrase = builder.Configuration["DataEncryption:Passphrase"] ?? "HealthwiseFleet-DefaultKey-2024!";
var encryptor  = new DataEncryptor(passphrase);
var dataCtx    = new JsonDataContext(dataDir, encryptor);

builder.Services.AddSingleton(dataCtx);
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.AddScoped<IVehicleRepository,     JsonVehicleRepository>();
builder.Services.AddScoped<IBookingRepository,     JsonBookingRepository>();
builder.Services.AddScoped<IStaffRepository,       JsonStaffRepository>();
builder.Services.AddScoped<IFuelReceiptRepository, JsonFuelReceiptRepository>();
builder.Services.AddScoped<IAuditLogRepository,    JsonAuditLogRepository>();

// ?? Cookie authentication ?????????????????????????????????????????????????
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Account/Login";
        options.LogoutPath       = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

// ?? Seed data ?????????????????????????????????????????????????????????????
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await DataSeeder.SeedAsync(
        sp.GetRequiredService<IVehicleRepository>(),
        sp.GetRequiredService<IBookingRepository>(),
        sp.GetRequiredService<IStaffRepository>(),
        sp.GetRequiredService<IFuelReceiptRepository>(),
        sp.GetRequiredService<IAuditLogRepository>(),
        sp.GetRequiredService<IPasswordHasher>());
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
