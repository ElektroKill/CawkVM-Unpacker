using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace CawkVMUnpacker.Unpacking {
	internal sealed class CawkVMToCilBodyConverter {
		private readonly ModuleDefMD _module;

		internal CawkVMToCilBodyConverter(ModuleDefMD module) {
			_module = module;
		}

		internal CilBody Convert(MethodDef method, IList<VMInstruction> instrs, IList<VMExceptionHandler> exceptionHandlers) {
			var newInstrs = ConvertInstructions(method, instrs);

			FixBrancheTargets(newInstrs);

			var newExHandlers = ConvertExceptionHandlers(exceptionHandlers, newInstrs);

			return new CilBody(method.Body.InitLocals, newInstrs, newExHandlers, method.Body.Variables) {
				LocalVarSigTok = method.Body.LocalVarSigTok,
				PdbMethod = method.Body.PdbMethod
			};
		}

		private IList<Instruction> ConvertInstructions(MethodDef method, IList<VMInstruction> instrs) {
			List<Instruction> newInstrs = new List<Instruction>();
			foreach (VMInstruction parsedInstr in instrs) {
				switch (parsedInstr.OperandType) {
					case OperandType.InlineNone:
						newInstrs.Add(new Instruction(parsedInstr.OpCode.ToOpCode(), null));
						break;
					case OperandType.InlineBrTarget:
					case OperandType.InlineSwitch:
					case OperandType.ShortInlineBrTarget:
						if (parsedInstr.Operand is null)
							throw new InvalidOperationException();
						newInstrs.Add(new Instruction(parsedInstr.OpCode.ToOpCode(), parsedInstr.Operand));
						break;
					case OperandType.InlineField:
					case OperandType.InlineMethod:
					case OperandType.InlineTok:
					case OperandType.InlineType:
						if (parsedInstr.Operand is null)
							throw new InvalidOperationException();
						newInstrs.Add(
							new Instruction(parsedInstr.OpCode.ToOpCode(), _module.ResolveToken((int)parsedInstr.Operand)));
						break;
					case OperandType.InlineI:
					case OperandType.InlineI8:
					case OperandType.InlineR:
					case OperandType.InlineString:
					case OperandType.ShortInlineI:
					case OperandType.ShortInlineR:
						if (parsedInstr.Operand is null)
							throw new InvalidOperationException();
						newInstrs.Add(new Instruction(parsedInstr.OpCode.ToOpCode(), parsedInstr.Operand));
						break;
					case OperandType.InlineVar:
					case OperandType.ShortInlineVar:
						if (parsedInstr.Operand is null)
							throw new InvalidOperationException();
						(int index, bool isParamLoad) = ((int, bool))parsedInstr.Operand;
						newInstrs.Add(new Instruction(parsedInstr.OpCode.ToOpCode(),
							isParamLoad ? method.Parameters[index] : method.Body.Variables[index]));
						break;
				}
			}

			return newInstrs;
		}

		private static void FixBrancheTargets(IList<Instruction> instrs) {
			foreach (var instr in instrs) {
				switch (instr.OpCode.OperandType) {
					case OperandType.InlineBrTarget:
					case OperandType.ShortInlineBrTarget:
						instr.Operand = instrs[(int)instr.Operand];
						break;
					case OperandType.InlineSwitch: {
						int[] oldOperand = (int[])instr.Operand;
						var newOperand = new Instruction[oldOperand.Length];
						for (int j = 0; j < oldOperand.Length; j++)
							newOperand[j] = instrs[oldOperand[j]];
						instr.Operand = newOperand;
						break;
					}
				}
			}
		}

		private IList<ExceptionHandler> ConvertExceptionHandlers(IList<VMExceptionHandler> exHandlers, IList<Instruction> instrs) {
			var result = new List<ExceptionHandler>();
			foreach (var exHandler in exHandlers) {
				var exType = exHandler.HandlerType switch {
					1 => ExceptionHandlerType.Catch,
					2 => ExceptionHandlerType.Duplicated,
					3 => ExceptionHandlerType.Fault,
					4 => ExceptionHandlerType.Filter,
					5 => ExceptionHandlerType.Finally,
					_ => throw new ArgumentOutOfRangeException()
				};

				var handler = new ExceptionHandler(exType) {
					CatchType = exHandler.CatchTypeMDToken != -1
						? _module.ResolveToken(exHandler.CatchTypeMDToken) as ITypeDefOrRef
						: null,
					FilterStart = exHandler.FilterStart != -1 ? instrs[exHandler.FilterStart] : null,
					HandlerEnd = exHandler.HandlerEnd != -1 ? instrs[exHandler.HandlerEnd] : null,
					HandlerStart = exHandler.HandlerStart != -1 ? instrs[exHandler.HandlerStart] : null,
					TryEnd = exHandler.TryEnd != -1 ? instrs[exHandler.TryEnd] : null,
					TryStart = exHandler.TryStart != -1 ?instrs[exHandler.TryStart] : null
				};

				result.Add(handler);
			}

			return result;
		}
	}
}
