using MagicStorageExtra.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorageExtra.Components
{
	public class StorageHeart : StorageAccess
	{
		public override ModTileEntity GetTileEntity() {
			return mod.GetTileEntity("TEStorageHeart");
		}

		public override int ItemType(int frameX, int frameY) {
			return ModContent.ItemType<Items.StorageHeart>();
		}

		public override bool HasSmartInteract() {
			return true;
		}

		public override TEStorageHeart GetHeart(int i, int j) {
			return (TEStorageHeart)TileEntity.ByPosition[new Point16(i, j)];
		}

		public override bool NewRightClick(int i, int j) {
			Player player = Main.player[Main.myPlayer];
			Item item = player.inventory[player.selectedItem];
			if (item.type == ModContent.ItemType<Locator>() || item.type == ModContent.ItemType<LocatorDisk>() || item.type == ModContent.ItemType<PortableAccess>()) {
				if (Main.tile[i, j].frameX % 36 == 18)
					i--;
				if (Main.tile[i, j].frameY % 36 == 18)
					j--;
				var locator = (Locator)item.modItem;
				locator.location = new Point16(i, j);
				if (player.selectedItem == 58)
					Main.mouseItem = item.Clone();
				Main.NewText("Locator successfully set to: X=" + i + ", Y=" + j);
				return true;
			}
			return base.NewRightClick(i, j);
		}
	}
}
