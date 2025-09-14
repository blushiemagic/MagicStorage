using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		public static string GetUsageText(this ModCommand command) => $"[c/ff6a00: {command.Command}]";

		public static string GetUsageText(this ModCommand command, string args) => $"[c/ff6a00: {command.Command} {args}]";
	}
}
