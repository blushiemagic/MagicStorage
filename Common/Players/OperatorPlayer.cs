using MagicStorage.Common.Systems;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Common.Players {
	public class OperatorPlayer : ModPlayer {
		public bool hasOp;

		internal bool manualOp;

		#if TML_144
		public override void OnEnterWorld() {
		#else
		public override void OnEnterWorld(Player player) {
		#endif
			Netcode.RequestingOperatorKey = false;
		}
	}
}
