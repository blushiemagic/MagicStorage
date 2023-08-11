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

			if (!item.IsAir && item.ModItem is Locator locator && item.ModItem is not PortableCraftingAccess)
			{
				if (Main.tile[i, j].TileFrameX % 36 == 18)
					i--;
				if (Main.tile[i, j].TileFrameY % 36 == 18)
					j--;
				locator.Location = new Point16(i, j);
				if (player.selectedItem == 58)
					Main.mouseItem = item.Clone();
				
				Utility.ConvertToGPSCoordinates(new Point16(i, j).ToWorldCoordinates(), out string compassText, out string depthText);

				Main.NewText(Language.GetTextValue("Mods.MagicStorage.LocatorSet", compassText, depthText));
				return true;
			}

			return base.RightClick(i, j);
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (Main.tile[i, j].TileFrameX > 0)
				i--;
			if (Main.tile[i, j].TileFrameY > 0)
				j--;

			if (!TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity te) || te is not TEStorageHeart heart)
				return;

			NetHelper.Report(true, $"Checking if heart entity at location X={i}, Y={j} is used by any clients...");

			if (heart.AnyClientUsingThis()) {
				NetHelper.Report(false, "Heart entity is currently in use, preventing destruction");
				fail = true;
			} else
				NetHelper.Report(false, "Heart entity is currently not in use, allowing destruction");
		}
	}
}
