using MagicStorage.Common.Systems.Auditing;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Commands {
	internal class PrintAuditFile : ModCommand {
		public override string Command => "msaudit";

		public override CommandType Type => CommandType.Chat | CommandType.Console;

		public override string Usage => "[c/ff6a00:Usage: /msaudit]";

		public override string Description => Mod.GetLocalization("AuditLogging.CommandInfo.Descriptions.msaudit").Value;

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				caller.Reply(Mod.GetLocalization("AuditLogging.CommandInfo.NoArguments").Value, Color.Red);
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				caller.Reply(Mod.GetLocalization("AuditLogging.CommandInfo.SingleplayerOnly").Value, Color.Red);
				return;
			}

			if (!MagicStorageServerConfig.AuditLoggingEnabled) {
				caller.Reply(Mod.GetLocalization("AuditLogging.CommandInfo.NoLogFile").Value, Color.Red);
				return;
			}

			string path = AuditSystem.AuditPath;
			if (!File.Exists(path)) {
				caller.Reply(Mod.GetLocalization("AuditLogging.CommandInfo.NoLogFile").Value, Color.Red);
				return;
			}

			caller.Reply("Generating human-readable audit log...", Color.Yellow);
			caller.Reply($"File will be located at: {path}", Color.Yellow);

			AuditSystem.DeconstructAuditFile();
		}
	}
}
