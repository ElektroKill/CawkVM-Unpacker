namespace CawkVMUnpacker.Unpacking {
	internal sealed record VMExceptionHandler {
		internal int CatchTypeMDToken { get; set; }
		internal int FilterStart { get; set; }
		internal int HandlerEnd { get; set; }
		internal int HandlerStart { get; set; }
		internal int HandlerType { get; set; }
		internal int TryEnd { get; set; }
		internal int TryStart { get; set; }
	}
}
