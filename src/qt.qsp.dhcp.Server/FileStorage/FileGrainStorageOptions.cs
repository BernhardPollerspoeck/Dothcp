using Orleans.Storage;

namespace qt.qsp.dhcp.Server.FileStorage;

public sealed class FileGrainStorageOptions : IStorageProviderSerializerOptions
{
	#region properties
	public required string RootDirectory { get; set; }

	public required IGrainStorageSerializer GrainStorageSerializer { get; set; }
	#endregion
}