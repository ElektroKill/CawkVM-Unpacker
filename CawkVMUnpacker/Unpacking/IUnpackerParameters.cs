namespace CawkVMUnpacker.Unpacking {
	internal interface IUnpackerParameters {
		string? FilePath { get; }
		bool PreserveMetadata { get; }
		bool KeepExtraPEData { get; set; }

		// Unpacker specific parameters
		string? DataResourceName { get; }
	}
}
