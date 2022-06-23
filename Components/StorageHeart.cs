using MagicStorage.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class StorageHeart : StorageAccess
	{
		public override TEStorageHeart GetTileEntity() => ModContent.GetInstance<TEStorageHeart>();

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.StorageHeart>();

		public override TEStorageHeart GetHeart(int i, int j)
		{
			//return (TEStorageHeart) TileEntity.ByPosition[new Point16(i, j)];
			if (TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity tileEntity))
				return (TEStorageHeart) tileEntity;
			return null;
		}

		public override bool RightClick(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Item item = player.HeldItem;
			if (item.type == ModContent.ItemType<Locator>() || item.type == ModContent.ItemType<LocatorDisk>() || item.type == ModContent.ItemType<PortableAccess>())
			{
				if (Main.tile[i, j].TileFrameX % 36 == 18)
					i--;
				if (Main.tile[i, j].TileFrameY % 36 == 18)
					j--;
				Locator locator = (Locator) item.ModItem;
				locator.location = new Point16(i, j);
				if (player.selectedItem == 58)
					Main.mouseItem = item.Clone();
				Main.NewText(Language.GetTextValue("Mods.MagicStorage.LocatorSet", i, j));
				return true;
			}

			return base.RightClick(i, j);
		}
	}
}
