using MagicStorage.Components;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	public class QuickStackDetour : ILoadable {
		public Mod Mod { get; private set; } = null!;

		public void Load(Mod mod) {
			Mod = mod;

			On.Terraria.Player.QuickStackAllChests += Player_QuickStackAllChests;
		}

		private void Player_QuickStackAllChests(On.Terraria.Player.orig_QuickStackAllChests orig, Player self) {
			orig(self);

			//Attempt to quick stack into nearby storage systems
			Point16 centerTile = self.Center.ToTileCoordinates16();
			Point16 unitX = new(1, 0), unitY = new(0, 1), one = new(1, 1);

			List<(StorageAccess, Point16)> storageAccesses = new();

			for (int x = centerTile.X - 17; x <= centerTile.X + 17; x++) {
				if (x < 0 || x >= Main.maxTilesX)
					continue;

				for (int y = centerTile.Y - 17; y <= centerTile.Y + 17; y++) {
					if (y < 0 || y >= Main.maxTilesY)
						continue;

					if (TileLoader.GetTile(Main.tile[x, y].TileType) is StorageAccess access)
						storageAccesses.Add((access, new Point16(x, y)));
				}
			}

			//Same check in the original method
			List<Item> items = self.inventory.Skip(10).Take(40).Where(i => !i.IsAir && !i.favorited && !i.IsACoin).ToList();

			bool couldDeposit = false;

			IEnumerable<TEStorageHeart> hearts = storageAccesses.Select(t => (t.Item1.GetHeart(t.Item2.X, t.Item2.Y), t.Item2))
				.Where(t => t.Item1 is not null)
				.DistinctBy(t => t.Item2)
				.Select(t => t.Item1);

			if (Main.netMode == NetmodeID.SinglePlayer) {
				foreach (TEStorageHeart heart in hearts) {
					if (!StorageGUI.TryDeposit(heart, items, quickStack: true))
						continue;

					couldDeposit = true;

					items = new(items.Where(i => !i.IsAir));

					if (items.Count == 0)
						break;
				}
			} else {
				//Must manually check for units that can have items quick stacked into them, then manually send deposit requests for them
				foreach (TEStorageHeart heart in hearts) {
					foreach (Item item in items) {
						foreach (TEStorageUnit unit in heart.GetStorageUnits().OfType<TEStorageUnit>()) {
							if (unit.HasSpaceFor(item)) {
								heart.TryDeposit(item);
								couldDeposit = true;
								break;
							}
						}
					}

					items = new(items.Where(i => !i.IsAir));

					if (items.Count == 0)
						break;
				}
			}

			if (couldDeposit)
				SoundEngine.PlaySound(SoundID.Grab);
		}

		public void Unload()
		{
			On.Terraria.Player.QuickStackAllChests -= Player_QuickStackAllChests;

			Mod = null!;
		}
	}
}
