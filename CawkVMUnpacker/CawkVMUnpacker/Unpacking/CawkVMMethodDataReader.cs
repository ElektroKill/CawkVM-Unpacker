using System.Collections.Generic;
using dnlib.DotNet.Emit;
using dnlib.IO;

namespace CawkVMUnpacker.Unpacking {
	internal static class CawkVMMethodDataReader {
		internal static (IList<VMInstruction> Instructions, IList<VMExceptionHandler> ExceptionHandlers) ReadVMMethodData(byte[] data) {
			var reader = ByteArrayDataReaderFactory.CreateReader(data);

			var exHandlers = ReadExceptionHandlers(ref reader);
			var instrs = ReadInstructions(ref reader);

			return (instrs, exHandlers);
		}

		private static IList<VMExceptionHandler> ReadExceptionHandlers(ref DataReader reader) {
			List<VMExceptionHandler> response = new List<VMExceptionHandler>();
			int exCount = reader.ReadInt32();
			for (int i = 0; i < exCount; i++) {
				VMExceptionHandler handler = new VMExceptionHandler {
					CatchTypeMDToken = reader.ReadInt32(),
					FilterStart = reader.ReadInt32(),
					HandlerEnd = reader.ReadInt32(),
					HandlerStart = reader.ReadInt32(),
					HandlerType = reader.ReadByte(),
					TryEnd = reader.ReadInt32(),
					TryStart = reader.ReadInt32()
				};
				response.Add(handler);
			}

			return response;
		}

		private static IList<VMInstruction> ReadInstructions(ref DataReader reader) {
			List<VMInstruction> response = new List<VMInstruction>();
			int instrCount = reader.ReadInt32();
			for (int i = 0; i < instrCount; i++) {
				VMInstruction instr = new VMInstruction {
					OpCode = (Code)reader.ReadInt16()
				};
				switch (reader.ReadByte()) {
					case 0:
						instr.OperandType = OperandType.InlineNone;
						instr.Operand = null;
						break;
					case 1:
						instr.OperandType = OperandType.InlineMethod;
						instr.Operand = reader.ReadInt32();
						break;
					case 2:
						instr.OperandType = OperandType.InlineString;
						instr.Operand = reader.ReadSerializedString();
						break;
					case 3:
						instr.OperandType = OperandType.InlineI;
						instr.Operand = reader.ReadInt32();
						break;
					case 4:
						instr.OperandType = OperandType.ShortInlineVar;
						instr.Operand = (reader.ReadInt32(), reader.ReadBoolean());
						break;
					case 5:
						instr.OperandType = OperandType.InlineField;
						instr.Operand = reader.ReadInt32();
						break;
					case 6:
						instr.OperandType = OperandType.InlineType;
						instr.Operand = reader.ReadInt32();
						break;
					case 7:
						instr.OperandType = OperandType.ShortInlineBrTarget;
						instr.Operand = reader.ReadInt32();
						break;
					case 8:
						instr.OperandType = OperandType.ShortInlineI;
						instr.Operand = reader.ReadByte();
						break;
					case 9: {
						instr.OperandType = OperandType.InlineSwitch;
						int count = reader.ReadInt32();
						int[] indexes = new int[count];
						for (int j = 0; j < count; j++) {
							indexes[j] = reader.ReadInt32();
						}

						instr.Operand = indexes;
						break;
					}
					case 10:
						instr.OperandType = OperandType.InlineBrTarget;
						instr.Operand = reader.ReadInt32();
						break;
					case 11:
						instr.OperandType = OperandType.InlineTok;
						instr.Operand = reader.ReadInt32();
						reader.Position += 1;
						break;
					case 12:
						instr.OperandType = OperandType.InlineVar;
						instr.Operand = (reader.ReadInt32(), reader.ReadBoolean());
						break;
					case 13:
						instr.OperandType = OperandType.ShortInlineR;
						instr.Operand = reader.ReadSingle();
						break;
					case 14:
						instr.OperandType = OperandType.InlineR;
						instr.Operand = reader.ReadDouble();
						break;
					case 15:
						instr.OperandType = OperandType.InlineI8;
						instr.Operand = reader.ReadInt64();
						break;
				}

				response.Add(instr);
			}

			return response;
		}
	}
}
