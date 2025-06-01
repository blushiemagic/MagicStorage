using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.Players {
	internal class PityLootDrops : ModPlayer {
		public FailedDropAttemptsDatabase Attempts { get; private set; } = new();

		public override void SaveData(TagCompound tag) => Attempts.Save(tag);

		public override void LoadData(TagCompound tag) => Attempts.Load(tag);

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)MessageType.SyncPityDropsPlayer);
			packet.Write((byte)Player.whoAmI);
			Attempts.NetSend(packet);
			packet.Send(toWho, fromWho);
		}

		internal void ReceiveSync(BinaryReader reader) {
			Attempts.NetReceive(reader);
		}

		public override void CopyClientState(ModPlayer targetCopy) {
			PityLootDrops modPlayer = (PityLootDrops)targetCopy;

			Attempts.NetCopyTo(modPlayer.Attempts);
		}

		public override void SendClientChanges(ModPlayer clientPlayer) {
			PityLootDrops modPlayer = (PityLootDrops)clientPlayer;

			if (!Attempts.IsEquivalentTo(modPlayer.Attempts))
				SyncPlayer(-1, Main.myPlayer, false);
		}
	}
}
