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
		}

		public override void PreUpdate() {
			// Normally, this code would go in OnEnterWorld, but the client doesn't have the necessary info
			int whoAmI = Player.whoAmI;
			if (Main.netMode != NetmodeID.SinglePlayer && !hasOp && Main.countsAsHostForGameplay[whoAmI]) {
				// Grant "Server Admin" to the local host
				hasOp = true;
				manualOp = true;

				if (whoAmI == Main.myPlayer)
					NetHelper.ClientSendPlayerHasOp(whoAmI);
			}
		}
	}
}
