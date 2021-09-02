using CawkVMUnpacker.Unpacking;

namespace CawkVMUnpacker.CLI {
	internal sealed class CommandLineArguments : IUnpackerParameters {
		internal bool ShowHelp { get; set; }
		public string? FilePath { get; set; }
		public bool PreserveMetadata { get; set; }
		public string? DataResourceName { get; set; }
	}
}
