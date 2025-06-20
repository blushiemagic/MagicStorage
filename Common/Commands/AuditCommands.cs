using MagicStorage.Common.Systems.Auditing;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Commands {
	internal class PrintAuditFile : ModCommand {
		public override string Command => "msaudit";

		public override CommandType Type => CommandType.Chat | CommandType.Console;

		public override string Usage => this.GetUsageText();

		public override string Description => Mod.GetLocalization("AuditLogging.CommandInfo.Descriptions.msaudit").Value;

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				string msg = Mod.GetLocalization("AuditLogging.CommandInfo.NoArguments").Value;

				if (Main.netMode == NetmodeID.Server)
					Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Red, ConsoleColor.Black);
				else
					caller.Reply(msg, Color.Red);

				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				caller.Reply(Mod.GetLocalization("AuditLogging.CommandInfo.MultiplayerOnly").Value, Color.Red);
				return;
			}

			if (!MagicStorageServerConfig.AuditLoggingEnabled) {
				string msg = Mod.GetLocalization("AuditLogging.CommandInfo.LoggingDisabled").Value;

				if (Main.netMode == NetmodeID.Server)
					Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Red, ConsoleColor.Black);
				else
					caller.Reply(msg, Color.Red);

				return;
			}

			if (Main.netMode == NetmodeID.Server) {
				string path = AuditSystem.AuditPath;
				if (!File.Exists(path)) {
					Utility.PrettyWriteLineToConsole(Mod.GetLocalization("AuditLogging.CommandInfo.NoLogFile").Value, ConsoleColor.Red, ConsoleColor.Black);
					return;
				}

				AuditSystem.TranslateAuditFile();
			} else
				AuditSystem.RequestTranslatedAuditFile();
		}
	}

	internal class ClearAuditFile : ModCommand {
		public override string Command => "msauditclear";

		public override CommandType Type => CommandType.Chat | CommandType.Console;

		public override string Usage => this.GetUsageText();

		public override string Description => Mod.GetLocalization("AuditLogging.CommandInfo.Descriptions.msauditclear").Value;

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				string msg = Mod.GetLocalization("AuditLogging.CommandInfo.NoArguments").Value;

				if (Main.netMode == NetmodeID.Server)
					Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Red, ConsoleColor.Black);
				else
					caller.Reply(msg, Color.Red);

				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				caller.Reply(Mod.GetLocalization("AuditLogging.CommandInfo.MultiplayerOnly").Value, Color.Red);
				return;
			}

			if (!MagicStorageServerConfig.AuditLoggingEnabled) {
				string msg = Mod.GetLocalization("AuditLogging.CommandInfo.LoggingDisabled").Value;

				if (Main.netMode == NetmodeID.Server)
					Utility.PrettyWriteLineToConsole(msg, ConsoleColor.Red, ConsoleColor.Black);
				else
					caller.Reply(msg, Color.Red);

				return;
			}

			if (Main.netMode == NetmodeID.Server)
				AuditSystem.ClearAudits();
			else
				AuditSystem.RequestAuditFileClear();
		}
	}
}
