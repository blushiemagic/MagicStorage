using MagicStorage.Common.Systems;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Players {
	public class OperatorPlayer : ModPlayer {
		public bool hasOp;

		internal bool manualOp;

		public override void OnEnterWorld(Player player) {
			Netcode.RequestingOperatorKey = false;
		}
	}
}
