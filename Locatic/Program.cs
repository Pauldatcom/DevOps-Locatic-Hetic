using Locatic.Data;
using Locatic.Services.Implementations;
using Locatic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHealthChecks();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IModeleService, ModeleService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(context);
}

// Pas de redirection HTTPS en conteneur (HTTP only sur :8080).
if (!app.Environment.IsEnvironment("Production"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Middleware /metrics (plus fiable que MapMetrics avec le catch-all MVC).
app.UseMetricServer();
app.UseHttpMetrics();

app.UseRouting();

app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();