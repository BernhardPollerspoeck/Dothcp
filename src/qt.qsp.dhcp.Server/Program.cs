using qt.qsp.dhcp.Server.Components;
using qt.qsp.dhcp.Server.FileStorage;
using qt.qsp.dhcp.Server.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Host.UseOrleans(static siloBuilder =>
{
	siloBuilder.UseLocalhostClustering();
	siloBuilder.AddFileGrainStorage("File", options =>
	 {
		 //string path = Environment.GetFolderPath(
		 //Environment.SpecialFolder.ApplicationData);

		 options.RootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orleans/GrainState/v1");
	 });
	siloBuilder.AddMemoryGrainStorage("leases");//TODO: file storage
});

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
