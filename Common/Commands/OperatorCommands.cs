using MagicStorage.Common.Players;
using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Commands {
	internal class RequestOperator : ModCommand {
		public override string Command => "reqop";

		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /reqop]";

		public override string Description => Mod.GetLocalization("ServerOperator.CommandInfo.Descriptions.reqop").Value;

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.NoArguments").Value, Color.Red);
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.MultiplayerOnly").Value, Color.Red);
				return;
			}

			if (caller.Player.GetModPlayer<OperatorPlayer>().manualOp) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.AlreadyAdmin").Value, Color.Red);
				return;
			}

			NetHelper.ClientRequestServerOperator();
		}
	}

	internal abstract class ChangeOperatorStatusCommand : ModCommand {
		public abstract bool GivesOperatorStatus { get; }

		public override CommandType Type => CommandType.Chat;

		public override string Usage => $"[c/ff6a00:Usage: /{Command} <number>]";

		public override string Description => Mod.GetLocalization("ServerOperator.CommandInfo.Descriptions." + (GivesOperatorStatus ? "op" : "deop")).Value;

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 1) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.OneIntArgument").Value, Color.Red);
				return;
			}

			if (!int.TryParse(args[0], out int client) || client < 0 || client > 255) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.ArgumentNotByte").Value, Color.Red);
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.MultiplayerOnly").Value, Color.Red);
				return;
			}

			if (!caller.Player.GetModPlayer<OperatorPlayer>().manualOp) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.MultiplayerAdminOnly").Value, Color.Red);
				return;
			}

			Player plr = Main.player[client];

			if (!plr.active) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.NotConnected").WithFormatArgs(client).Value, Color.Red);
				return;
			}

			var mp = plr.GetModPlayer<OperatorPlayer>();

			if (mp.hasOp == GivesOperatorStatus) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.ClientAlready" + (GivesOperatorStatus ? "Op" : "Deop")).WithFormatArgs(client).Value, Color.Red);
				return;
			}

			mp.hasOp = GivesOperatorStatus;
			mp.manualOp = false;

			NetHelper.ClientSendPlayerHasOp(client);

			Netcode.ClientPrintKeyReponse(valid: true);
		}
	}

	internal class GiveOperator : ChangeOperatorStatusCommand {
		public override string Command => "op";

		public override bool GivesOperatorStatus => true;
	}

	internal class RemoveOperator : ChangeOperatorStatusCommand {
		public override string Command => "deop";

		public override bool GivesOperatorStatus => false;
	}

	internal class WhoAreClients : ModCommand {
		public override string Command => "whois";

		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /whois]";

		public override string Description => Mod.GetLocalization("ServerOperator.CommandInfo.Descriptions.whois").Value;

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.NoArguments").Value, Color.Red);
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.MultiplayerOnly").Value, Color.Red);
				return;
			}

			if (!caller.Player.GetModPlayer<OperatorPlayer>().hasOp) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.MultiplayerOperatorOnly").Value, Color.Red);
				return;
			}

			IEnumerable<string> print = Main.player
				.Select((p, i) => (player: p, whoAmI: i))
				.Where(t => t.player.active)
				.Select(t => $"[{t.whoAmI}]: {t.player.name}")
				.Prepend(Mod.GetLocalization("ServerOperator.CommandInfo.Players").Value);

			caller.Reply(string.Join("\n  ", print), Color.LightGray);
		}
	}

	internal class WhoAmI : ModCommand {
		public override string Command => "whoami";

		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /whoami]";

		public override string Description => Mod.GetLocalization("ServerOperator.CommandInfo.Descriptions.whoami").Value;

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.NoArguments").Value, Color.Red);
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer) {
				caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.MultiplayerOnly").Value, Color.Red);
				return;
			}

			caller.Reply(Mod.GetLocalization("ServerOperator.CommandInfo.YouAreClient").WithFormatArgs(caller.Player.whoAmI).Value, Color.LightGray);
		}
	}
}
