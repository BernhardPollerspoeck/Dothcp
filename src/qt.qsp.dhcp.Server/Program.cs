using qt.qsp.dhcp.Server.Components;
using qt.qsp.dhcp.Server.Workers;
using NLog.Web;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using qt.qsp.dhcp.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using qt.qsp.dhcp.Server.Data;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Services.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Configure SQLite Database
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "dhcp.db");
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!Directory.Exists(dbDirectory))
{
	Directory.CreateDirectory(dbDirectory!);
}

builder.Services.AddDbContext<DhcpDbContext>(options =>
	options.UseSqlite($"Data Source={dbPath}"));

// Register Repositories
builder.Services.AddScoped<ILeaseRepository, LeaseRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IIpAddressRepository, IpAddressRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();

// Register Core Services (replacements for Grains)
builder.Services.AddScoped<ILeaseService, LeaseService>();
builder.Services.AddScoped<IReservationService, ReservationServiceCore>();
builder.Services.AddScoped<IIpAddressService, IpAddressService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Register existing services
builder.Services.AddTransient<ISettingsLoaderService, SettingsLoaderService>();
builder.Services.AddTransient<ISettingsService, SettingsService>();
builder.Services.AddTransient<IFirstRunService, FirstRunService>();
builder.Services.AddTransient<IOfferGeneratorService, OfferGeneratorService>();
builder.Services.AddTransient<ILeaseGrainSearchService, LeaseGrainSearchService>();
builder.Services.AddTransient<INetworkUtilityService, NetworkUtilityService>();
builder.Services.AddTransient<IDashboardService, DashboardService>();
builder.Services.AddSingleton<IDhcpServerService, DhcpServerService>();

builder.Services.AddHostedService<NetworkListener>();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<DhcpDbContext>();
	dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
