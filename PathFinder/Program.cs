using PathFinder.Interfaces;
using PathFinder.Strategies;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IGridFactory, GridFactory>();
builder.Services.AddScoped<IPathfindingEngine, AStarEngine>();
builder.Services.AddScoped<ILineOfSightChecker, BresenhamLineOfSightChecker>();
builder.Services.AddScoped<IHeadingCalculator, StopAndTurnHeadingCalculator>();
builder.Services.AddScoped<IPathOptimizer, RaycastingPathOptimizer>();
builder.Services.AddScoped<PathFinder.Services.PathfindingService>();
builder.Services.AddSingleton<ISpeedEvaluator, CostmapSpeedEvaluator>();
builder.Services.AddTransient<ILineOfSightChecker, BresenhamLineOfSightChecker>();
builder.Services.AddTransient<IHeadingCalculator, StopAndTurnHeadingCalculator>();

// ⭐ FIX: Forza la cultura invariante per il parsing dei numeri.
// Senza questo, su macchine con cultura it-IT il model binding di ASP.NET
// interpreta "0.5" come 5 (il punto viene letto come separatore migliaia).
// HTML <input type="number"> invia SEMPRE il punto come decimale (standard W3C),
// quindi il server deve usare InvariantCulture per interpretarlo correttamente.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
