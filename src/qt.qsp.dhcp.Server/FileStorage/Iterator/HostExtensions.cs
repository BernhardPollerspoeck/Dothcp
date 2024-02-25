using Microsoft.Extensions.Options;
using Orleans.Iterator.Abstraction.Server;

namespace qt.qsp.dhcp.Server.FileStorage.Iterator;

public static class HostExtensions
{
	public static ISiloBuilder UseFileGrainIterator(this ISiloBuilder builder, Action<FileGrainIteratorOptions> configureOptions)
	{
		builder.UseFileGrainIterator(c => c.Configure(configureOptions));
		return builder;
	}

	private static ISiloBuilder UseFileGrainIterator(this ISiloBuilder builder, Action<OptionsBuilder<FileGrainIteratorOptions>> configureOptions)
	{
		configureOptions?.Invoke(builder.Services.AddOptions<FileGrainIteratorOptions>());
		builder.Services.AddSingleton<IServerGrainIterator, FileGrainIterator>();
		return builder;
	}
}
