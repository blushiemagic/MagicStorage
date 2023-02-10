using MagicStorage.Common.Systems;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Players {
	public class OperatorPlayer : ModPlayer {
		public bool hasOp;

		internal bool manualOp;

		public override void OnEnterWorld() {
			Netcode.RequestingOperatorKey = false;
		}
	}
}
