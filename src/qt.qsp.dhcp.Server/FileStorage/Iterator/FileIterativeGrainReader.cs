using Microsoft.Extensions.Options;
using Orleans.Iterator.Abstraction;
using Orleans.Iterator.Abstraction.Server;
using Orleans.Runtime;
using System.Collections;

namespace qt.qsp.dhcp.Server.FileStorage.Iterator;

public class FileIterativeGrainReader<IGrainInterface>(
	IOptions<FileGrainIteratorOptions> options,
	params GrainDescriptor[] grainDescriptions)
	: IIterativeServerGrainReader
	where IGrainInterface : IGrain
{
	#region fields
	private string[]? _files;
	#endregion

	#region IIterativeGrainReader
	public bool ReadAllowed => _files is not null;
	public Task<bool> StartRead(CancellationToken cancellationToken)
	{
		_files = Directory.GetFiles(options.Value.RootDirectory);
		return Task.FromResult(ReadAllowed);
	}
	public Task StopRead(CancellationToken cancellationToken)
	{
		_files = null;
		return Task.CompletedTask;
	}
	#endregion

	#region IEnumerable<GrainId>
	public IEnumerator<GrainId?> GetEnumerator()
	{
		if (!ReadAllowed)
		{
			yield break;
		}
		foreach (var file in _files!)
		{
			var fileName = Path.GetFileNameWithoutExtension(file);
			//default.DHCP_LEASE_DNS.setting
			var split = fileName.Split('.');
			if (!grainDescriptions.Any(gd => gd.GrainType == split[2]))
			{
				continue;
			}

			yield return GrainId.Create(split[2], split[1]);
		}
	}
	#endregion

	#region IEnumerable
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	#endregion
}
