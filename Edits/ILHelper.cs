using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal static class ILHelper {
		public static bool LogILEdits { get; set; } = true;

		private static void PrepareInstruction(Instruction instr, out string offset, out string opcode, out string operand) {
			offset = $"IL_{instr.Offset:X5}:";

			opcode = instr.OpCode.Name;

			if (instr.Operand is null)
				operand = "";
			else if (instr.Operand is ILLabel label)  //This label's target should NEVER be null!  If it is, the IL edit wouldn't load anyway
				operand = $"IL_{label.Target.Offset:X5}";
			else if (instr.OpCode == OpCodes.Switch)
				operand = "(" + string.Join(", ", (instr.Operand as ILLabel[])!.Select(l => $"IL_{l.Target.Offset:X5}")) + ")";
			else
				operand = instr.Operand.ToString()!;
		}

		public static void CompleteLog(Mod mod, ILCursor c, bool beforeEdit = false) {
			if (!LogILEdits)
				return;

			int index = 0;

			//Get the method name
			string method = c.Method.Name;
			if (!method.Contains("ctor"))
				method = method[(method.LastIndexOf(':') + 1)..];
			else
				method = method[method.LastIndexOf('.')..];

			if (beforeEdit)
				method += " - Before";
			else
				method += " - After";

			//And the storage path
			string path = Program.SavePath;

			//Use the mod type's namespace start
			//Can't use "Mod.Name" since that uses "Mod.File" which might be null
			string modName = mod.GetType().Namespace!;

			if (modName.Contains('.'))
				modName = modName[..modName.IndexOf('.')];

			path = Path.Combine(path, "aA Mods", modName);
			Directory.CreateDirectory(path);

			//Get the class name
			string type = c.Method.Name;
			type = type[..type.IndexOf(':')];
			type = type[(type.LastIndexOf('.') + 1)..];

			FileStream file = File.Open(Path.Combine(path, $"{type}.{method}.txt"), FileMode.Create);

			using StreamWriter writer = new(file);

			writer.WriteLine(DateTime.Now.ToString("'['ddMMMyyyy '-' HH:mm:ss']'"));
			writer.WriteLine($"// ILCursor: {c.Method.Name}\n");

			writer.WriteLine("// Arguments:");

			var args = c.Method.Parameters;
			if (args.Count == 0)
				writer.WriteLine($"{"none",8}");
			else {
				foreach (var arg in args) {
					string argIndex = $"[{arg.Index}]";
					writer.WriteLine($"{argIndex,8} {arg.ParameterType.FullName} {arg.Name}");
				}
			}

			writer.WriteLine();

			writer.WriteLine("// Locals:");

			if (!c.Body.HasVariables)
				writer.WriteLine($"{"none",8}");
			else {
				foreach (var local in c.Body.Variables) {
					string localIndex = $"[{local.Index}]";
					writer.WriteLine($"{localIndex,8} {local.VariableType.FullName} V_{local.Index}");
				}
			}

			writer.WriteLine();

			writer.WriteLine("// Body:");
			do {
				PrepareInstruction(c.Instrs[index], out string offset, out string opcode, out string operand);

				writer.WriteLine($"{offset,-10}{opcode,-12} {operand}");
				index++;
			} while (index < c.Instrs.Count);
		}

		public static void UpdateInstructionOffsets(ILCursor c) {
			if (!LogILEdits)
				return;

			var instrs = c.Instrs;
			int curOffset = 0;

			static Instruction[] ConvertToInstructions(ILLabel[] labels) {
				Instruction[] ret = new Instruction[labels.Length];

				for (int i = 0; i < labels.Length; i++)
					ret[i] = labels[i].Target;

				return ret;
			}

			foreach (var ins in instrs) {
				ins.Offset = curOffset;

				if (ins.OpCode != OpCodes.Switch)
					curOffset += ins.GetSize();
				else {
					//'switch' opcodes don't like having the operand as an ILLabel[] when calling GetSize()
					//thus, this is required to even let the mod compile

					Instruction copy = Instruction.Create(ins.OpCode, ConvertToInstructions((ILLabel[])ins.Operand));
					curOffset += copy.GetSize();
				}
			}
		}

		public static void InitMonoModDumps() {
			//Disable assembly dumping until this bug is fixed by MonoMod
			//see: https://discord.com/channels/103110554649894912/445276626352209920/953380019072270419
			bool noLog = false;
			if (!LogILEdits || noLog)
				return;

			//Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE","Auto");
			//Environment.SetEnvironmentVariable("MONOMOD_DMD_DEBUG","1");

			string dumpDir = Path.GetFullPath("MonoModDump");

			Directory.CreateDirectory(dumpDir);

			Environment.SetEnvironmentVariable("MONOMOD_DMD_DUMP", dumpDir);
		}

		public static void DeInitMonoModDumps() {
			bool noLog = false;
			if (!LogILEdits || noLog)
				return;

			Environment.SetEnvironmentVariable("MONOMOD_DMD_DEBUG", "0");
		}

		public static string GetInstructionString(ILCursor c, int index) {
			if (index < 0 || index >= c.Instrs.Count)
				return "ERROR: Index out of bounds.";

			PrepareInstruction(c.Instrs[index], out string offset, out string opcode, out string operand);

			return $"{offset} {opcode}   {operand}";
		}

		public static void EnsureAreNotNull(params (MemberInfo? member, string identifier)[] memberInfos) {
			foreach (var (member, identifier) in memberInfos)
				if (member is null)
					throw new NullReferenceException($"Member reference \"{identifier}\" is null");
		}
	}
}
