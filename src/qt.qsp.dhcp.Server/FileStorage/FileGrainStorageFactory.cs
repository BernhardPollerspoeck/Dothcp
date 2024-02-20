using Microsoft.Extensions.Options;
using Orleans.Configuration.Overrides;
using Orleans.Storage;

namespace qt.qsp.dhcp.Server.FileStorage;

//https://learn.microsoft.com/en-us/dotnet/orleans/tutorials-and-samples/custom-grain-storage?pivots=orleans-7-0
internal static class FileGrainStorageFactory
{
	internal static IGrainStorage Create(IServiceProvider services, string name)
	{
		var optionsMonitor = services.GetRequiredService<IOptionsMonitor<FileGrainStorageOptions>>();

		return ActivatorUtilities.CreateInstance<FileGrainStorage>(
			services,
			name,
			optionsMonitor.Get(name),
			services.GetProviderClusterOptions(name));
	}
}
