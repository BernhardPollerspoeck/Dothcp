using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Configuration.Overrides;
using Orleans.Runtime;
using Orleans.Runtime.Hosting;
using Orleans.Storage;

namespace qt.qsp.dhcp.Server.FileStorage;

//https://learn.microsoft.com/en-us/dotnet/orleans/tutorials-and-samples/custom-grain-storage?pivots=orleans-7-0
internal static class FileGrainStorageFactory
{
	internal static IGrainStorage Create(
		IServiceProvider services, string name)
	{
		var optionsMonitor =
			services.GetRequiredService<IOptionsMonitor<FileGrainStorageOptions>>();

		return ActivatorUtilities.CreateInstance<FileGrainStorage>(
			services,
			name,
			optionsMonitor.Get(name),
			services.GetProviderClusterOptions(name));
	}
}
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
public sealed class FileGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
	private readonly string _storageName;
	private readonly FileGrainStorageOptions _options;
	private readonly ClusterOptions _clusterOptions;

	public FileGrainStorage(
		string storageName,
		FileGrainStorageOptions options,
		IOptions<ClusterOptions> clusterOptions)
	{
		_storageName = storageName;
		_options = options;
		_clusterOptions = clusterOptions.Value;
	}


	public Task ClearStateAsync<T>(
		string stateName,
		GrainId grainId,
		IGrainState<T> grainState)
	{
		var fName = GetKeyString(stateName, grainId);
		var path = Path.Combine(_options.RootDirectory, fName!);
		var fileInfo = new FileInfo(path);
		if (fileInfo.Exists)
		{
			if (fileInfo.LastWriteTimeUtc.ToString() != grainState.ETag)
			{
				throw new InconsistentStateException($"""
                    Version conflict (ClearState): ServiceId={_clusterOptions.ServiceId}
                    ProviderName={_storageName} GrainType={typeof(T)}
                    GrainReference={grainId}.
                    """);
			}

			grainState.ETag = null;
			grainState.State = (T)Activator.CreateInstance(typeof(T))!;

			fileInfo.Delete();
		}

		return Task.CompletedTask;
	}


	public async Task ReadStateAsync<T>(
		string stateName,
		GrainId grainId,
		IGrainState<T> grainState)
	{
		var fName = GetKeyString(stateName, grainId);
		var path = Path.Combine(_options.RootDirectory, fName!);
		var fileInfo = new FileInfo(path);
		if (fileInfo is { Exists: false })
		{
			grainState.State = (T)Activator.CreateInstance(typeof(T))!;
			return;
		}

		using var stream = fileInfo.OpenText();
		var storedData = await stream.ReadToEndAsync();

		grainState.State = _options.GrainStorageSerializer.Deserialize<T>(new BinaryData(storedData));
		grainState.ETag = fileInfo.LastWriteTimeUtc.ToString();
	}


	public async Task WriteStateAsync<T>(
		string stateName,
		GrainId grainId,
		IGrainState<T> grainState)
	{
		var storedData = _options.GrainStorageSerializer.Serialize(grainState.State);
		var fName = GetKeyString(stateName, grainId);
		var path = Path.Combine(_options.RootDirectory, fName!);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		}
		var fileInfo = new FileInfo(path);
		if (fileInfo.Exists && fileInfo.LastWriteTimeUtc.ToString() != grainState.ETag)
		{
			throw new InconsistentStateException($"""
                Version conflict (WriteState): ServiceId={_clusterOptions.ServiceId}
                ProviderName={_storageName} GrainType={typeof(T)}
                GrainReference={grainId}.
                """);
		}

		await File.WriteAllBytesAsync(path, storedData.ToArray());

		fileInfo.Refresh();
		grainState.ETag = fileInfo.LastWriteTimeUtc.ToString();
	}


	public void Participate(ISiloLifecycle lifecycle) =>
		lifecycle.Subscribe(
			observerName: OptionFormattingUtilities.Name<FileGrainStorage>(_storageName),
			stage: ServiceLifecycleStage.ApplicationServices,
			onStart: (ct) =>
			{
				Directory.CreateDirectory(_options.RootDirectory);
				return Task.CompletedTask;
			});


	private string GetKeyString(string grainType, GrainId grainId) =>
		$"{_clusterOptions.ServiceId}.{grainId.Key}.{grainType}.STATE";


}

public sealed class FileGrainStorageOptions : IStorageProviderSerializerOptions
{
	public required string RootDirectory { get; set; }

	public required IGrainStorageSerializer GrainStorageSerializer { get; set; }
}