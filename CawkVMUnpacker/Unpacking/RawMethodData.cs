namespace CawkVMUnpacker.Unpacking {
	internal sealed record RawMethodData {
		internal int Offset { get; set; }
		internal int Length { get; set; }
	}
}
