using Microsoft.Extensions.Options;
using Orleans.Runtime.Hosting;
using Orleans.Storage;

namespace qt.qsp.dhcp.Server.FileStorage;

public static class FileSiloBuilderExtensions
{
	public static ISiloBuilder AddFileGrainStorage(
		this ISiloBuilder builder,
		string providerName,
		Action<FileGrainStorageOptions> options) =>
		builder.ConfigureServices(
			services => services.AddFileGrainStorage(
				providerName, options));

	public static IServiceCollection AddFileGrainStorage(
		this IServiceCollection services,
		string providerName,
		Action<FileGrainStorageOptions> options)
	{
		services.AddOptions<FileGrainStorageOptions>(providerName)
			.Configure(options);

		services.AddTransient<
			IPostConfigureOptions<FileGrainStorageOptions>,
			DefaultStorageProviderSerializerOptionsConfigurator<FileGrainStorageOptions>>();

		return services.AddGrainStorage(providerName, FileGrainStorageFactory.Create);
	}
}
