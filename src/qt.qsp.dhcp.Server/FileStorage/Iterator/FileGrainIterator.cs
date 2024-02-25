using Microsoft.Extensions.Options;
using Orleans.Iterator.Abstraction;
using Orleans.Iterator.Abstraction.Server;

namespace qt.qsp.dhcp.Server.FileStorage.Iterator;

public class FileGrainIterator(
	IServiceProvider serviceProvider)
	: IServerGrainIterator
{
	#region IServerGrainIterator
	public Task<IIterativeServerGrainReader> GetReader<TGrainInterface>(params GrainDescriptor[] grainDescriptions) where TGrainInterface : IGrain
	{
		var options = serviceProvider.GetRequiredService<IOptions<FileGrainIteratorOptions>>();
		return Task.FromResult(
			(IIterativeServerGrainReader)new FileIterativeGrainReader<TGrainInterface>(
				options,
				grainDescriptions));
	}
	#endregion
}
