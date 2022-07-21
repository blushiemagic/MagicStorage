using MagicStorage.Components;
using System;
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

			//Player uses 17 tiles for the quick stack range
			bool CheckDistance(Point16 target) => Math.Abs(target.X - centerTile.X) <= 17 && Math.Abs(target.Y - centerTile.Y) <= 17;

			bool CloseEnough(TEStorageCenter center) => CheckDistance(center.Position) || CheckDistance(center.Position + unitX) || CheckDistance(center.Position + unitY) || CheckDistance(center.Position + one);

			IEnumerable<TEStorageCenter> storageAccesses = TileEntity.ByPosition.Values.OfType<TEStorageCenter>().Where(CloseEnough);

			//Same check in the original method
			List<Item> items = self.inventory.Skip(10).Take(40).Where(i => !i.IsAir && !i.favorited && !i.IsACoin).ToList();

			bool couldDeposit = false;

			foreach (TEStorageCenter access in storageAccesses) {
				if (!StorageGUI.TryDeposit(access, items))
					break;

				couldDeposit = true;

				items = new(items.Where(i => !i.IsAir));

				if (items.Count == 0)
					break;
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
