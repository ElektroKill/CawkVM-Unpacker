namespace CawkVMUnpacker.Unpacking {
	internal interface IUnpackerParameters {
		string? FilePath { get; }
		bool PreserveMetadata { get; }

		// Unpacker specific parameters
		string? DataResourceName { get; }
	}
}
