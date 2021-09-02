using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace CawkVMUnpacker {
	internal static class DnlibUtils {
		internal static int? FindFirstCodeIndex(this CilBody? body, Code code) {
			if (body is null)
				return null;
			var instrs = body.Instructions;
			for (int i = 0; i < instrs.Count; i++) {
				if (instrs[i].OpCode.Code == code)
					return i;
			}

			return null;
		}

		// TODO: replace this with proper stack analysis.
		internal static int? FindFirstNonNopPrecedingCode(this CilBody? body, int index) {
			if (body is null)
				return null;
			var instrs = body.Instructions;
			for (int i = index - 1; i >= 0; i--) {
				if (instrs[i].OpCode.Code != Code.Nop)
					return i;
			}

			return null;
		}

		internal static bool IsMethod(IMethod? method, string returnType, string parameters) =>
			method != null && method.FullName == returnType + " " + method.DeclaringType.FullName + "::" + method.Name + parameters;

		internal static byte[] GetOriginalRawILBytes(this MethodDef methodDef, ModuleDefMD module) {
			var reader = module.Metadata.PEImage.CreateReader(methodDef.RVA);

			byte b = reader.ReadByte();

			// parse header info and determine code size
			uint codeSize = 0;
			switch (b & 7) {
				case 2:
				case 6:
					codeSize = (uint)(b >> 2);
					break;
				case 3:
					ushort header = (ushort)(reader.ReadByte() << 8 | b);
					int headerSize = (header >> 12) * sizeof(uint);
					reader.ReadUInt16();
					codeSize = reader.ReadUInt32();
					reader.Position = (uint)headerSize;
					break;
			}

			// read actual body
			byte[] ilBytes = new byte[codeSize];
			reader.ReadBytes(ilBytes, 0, ilBytes.Length);
			return ilBytes;
		}
	}
}
