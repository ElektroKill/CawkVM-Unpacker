using dnlib.DotNet.Emit;

namespace CawkVMUnpacker.Unpacking {
	internal sealed record VMInstruction {
		internal Code OpCode { get; set; }
		internal OperandType OperandType { get; set; }
		internal object? Operand { get; set; }
	}
}
