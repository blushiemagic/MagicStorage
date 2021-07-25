using MagicStorage.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class StorageHeart : StorageAccess
	{
		public override ModTileEntity GetTileEntity() => mod.GetTileEntity("TEStorageHeart");

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.StorageHeart>();

		public override bool HasSmartInteract() => true;

		public override TEStorageHeart GetHeart(int i, int j)
		{
			//return (TEStorageHeart) TileEntity.ByPosition[new Point16(i, j)];
			if (TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity tileEntity))
			{
				return (TEStorageHeart) tileEntity;
			}

			return null;
		}

		public override bool NewRightClick(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Item item = player.inventory[player.selectedItem];
			if (item.type == ModContent.ItemType<Locator>() || item.type == ModContent.ItemType<LocatorDisk>() || item.type == ModContent.ItemType<PortableAccess>())
			{
				if (Main.tile[i, j].frameX % 36 == 18)
				{
					i--;
				}

				if (Main.tile[i, j].frameY % 36 == 18)
				{
					j--;
				}

				var locator = (Locator) item.modItem;
				locator.location = new Point16(i, j);
				if (player.selectedItem == 58)
				{
					Main.mouseItem = item.Clone();
				}

				Main.NewText("Locator successfully set to: X=" + i + ", Y=" + j);
				return true;
			}

			return base.NewRightClick(i, j);
		}
	}
}
