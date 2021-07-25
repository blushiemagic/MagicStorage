using MagicStorage.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class RemoteAccess : StorageAccess
	{
		public override ModTileEntity GetTileEntity() => mod.GetTileEntity("TERemoteAccess");

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.RemoteAccess>();

		public override bool HasSmartInteract() => true;

		public override TEStorageHeart GetHeart(int i, int j)
		{
			TileEntity ent = TileEntity.ByPosition[new Point16(i, j)];
			return ((TERemoteAccess) ent).GetHeart();
		}

		public override bool NewRightClick(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Item item = player.inventory[player.selectedItem];
			if (item.type == ModContent.ItemType<Locator>() || item.type == ModContent.ItemType<LocatorDisk>())
			{
				if (Main.tile[i, j].frameX % 36 == 18)
				{
					i--;
				}

				if (Main.tile[i, j].frameY % 36 == 18)
				{
					j--;
				}

				var ent = (TERemoteAccess) TileEntity.ByPosition[new Point16(i, j)];
				var locator = (Locator) item.modItem;
				if (ent.TryLocate(locator.location, out string message))
				{
					if (item.type == ModContent.ItemType<LocatorDisk>())
					{
						locator.location = new Point16(-1, -1);
					}
					else
					{
						item.SetDefaults();
					}
				}

				if (player.selectedItem == 58)
				{
					Main.mouseItem = item.Clone();
				}

				Main.NewText(message);
				return true;
			}

			return base.NewRightClick(i, j);
		}
	}
}
