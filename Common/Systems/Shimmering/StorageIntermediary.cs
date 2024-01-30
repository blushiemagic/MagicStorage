using MagicStorage.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.Systems.Shimmering {
	public class StorageIntermediary {
		public readonly List<Item> toDeposit = new();
		public readonly List<Item> toWithdraw = new();
		public readonly Vector2 playerCenter;
		public readonly Vector2 playerBottom;

		public readonly TEStorageHeart heart;

		public StorageIntermediary(TEStorageHeart heart) {
			this.heart = heart;
			playerCenter = Main.LocalPlayer.Center;
			playerBottom = Main.LocalPlayer.Bottom;
		}

		public StorageIntermediary(TEStorageHeart heart, Vector2 playerCenter, Vector2 playerBottom) {
			this.heart = heart;
			this.playerCenter = playerCenter;
			this.playerBottom = playerBottom;
		}

		public void Deposit(Item item) {
			toDeposit.Add(item);
		}

		public void Withdraw(int item, int stack = 1) {
			toWithdraw.Add(new Item(item, stack));
		}

		public void Send(BinaryWriter writer) {
			writer.Write(heart.Position);
			writer.WriteVector2(playerCenter);
			writer.WriteVector2(playerBottom);
		}

		public static StorageIntermediary Receive(BinaryReader reader) {
			Point16 position = reader.ReadPoint16();

			Vector2 playerCenter = reader.ReadVector2();
			Vector2 playerBottom = reader.ReadVector2();

			if (!TileEntity.ByPosition.TryGetValue(position, out var te) || te is not TEStorageHeart heart)
				return null;

			return new StorageIntermediary(heart, playerCenter, playerBottom);
		}
	}
}
