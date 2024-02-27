using qt.qsp.dhcp.Server.Components;
using qt.qsp.dhcp.Server.FileStorage;
using qt.qsp.dhcp.Server.StartupTasks;
using qt.qsp.dhcp.Server.Workers;
using qt.qsp.dhcp.Server.FileStorage.Iterator;
using NLog.Web;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Grains.DhcpManager;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Host.UseOrleans(static siloBuilder =>
{
	siloBuilder.UseLocalhostClustering();
	siloBuilder.AddFileGrainStorage(
		"File",
		options => options.RootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
		"Orleans/GrainState/v1"));
	siloBuilder.UseFileGrainIterator(
		o => o.RootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
		"Orleans/GrainState/v1"));

	siloBuilder.AddStartupTask<SettingsStartupTask>();
});

builder.Services.AddTransient<ISettingsLoaderService, SettingsLoaderService>();
builder.Services.AddTransient<IOfferGeneratorService, OfferGeneratorService>();

builder.Services.AddHostedService<NetworkListener>();

var app = builder.Build();

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
