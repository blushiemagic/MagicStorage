using MagicStorage.Common.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Players {
	public class OperatorPlayer : ModPlayer {
		public bool hasOp;

		internal bool manualOp;

		public override void OnEnterWorld() {
			Netcode.RequestingOperatorKey = false;

			// Grant "Server Admin" to the local host
			if (Main.netMode != NetmodeID.SinglePlayer && NetMessage.DoesPlayerSlotCountAsAHost(Player.whoAmI)) {
				hasOp = true;
				manualOp = true;
				NetHelper.ClientSendPlayerHasOp(Player.whoAmI);
			}
		}
	}
}
