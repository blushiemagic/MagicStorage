using MagicStorage.Items;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class CraftingAccess : StorageAccess
	{
		public override TECraftingAccess GetTileEntity() => ModContent.GetInstance<TECraftingAccess>();

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.CraftingAccess>();

		public override TEStorageHeart GetHeart(int i, int j)
		{
			Point16 point = TEStorageComponent.FindStorageCenter(new Point16(i, j));
			if (point == Point16.NegativeOne)
				return null;

			if (TileEntity.ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageCenter center)
				return center.GetHeart();

			return null;
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (Main.tile[i, j].TileFrameX > 0)
				i--;
			if (Main.tile[i, j].TileFrameY > 0)
				j--;

			if (!TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity te) || te is not TECraftingAccess access)
				return;

			if (access.stations.Any(item => !item.IsAir))
				fail = true;
		}

		public override bool CanExplode(int i, int j) {
			bool fail = false, discard = false, discard2 = false;

			KillTile(i, j, ref fail, ref discard, ref discard2);

			return !fail;
		}

		public override bool RightClick(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Item item = player.HeldItem;
			if (!item.IsAir && item.ModItem is PortableCraftingAccess locator)
			{
				if (Main.tile[i, j].TileFrameX % 36 == 18)
					i--;
				if (Main.tile[i, j].TileFrameY % 36 == 18)
					j--;
				locator.location = new Point16(i, j);
				locator.pendingDictionarySave = true;
				if (player.selectedItem == 58)
					Main.mouseItem = item.Clone();
				Main.NewText(Language.GetTextValue("Mods.MagicStorage.LocatorSet", i, j));
				return true;
			}

			return base.RightClick(i, j);
		}
	}
}
