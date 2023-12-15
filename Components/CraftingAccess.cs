using MagicStorage.Items;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class CraftingAccess : StorageAccess
	{
		public override TECraftingAccess GetTileEntity() => ModContent.GetInstance<TECraftingAccess>();

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.CraftingAccess>();

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

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
				locator.Location = new Point16(i, j);
				if (player.selectedItem == 58)
					Main.mouseItem = item.Clone();

				Utility.ConvertToGPSCoordinates(new Point16(i, j).ToWorldCoordinates(), out string compassText, out string depthText);

				Main.NewText(Language.GetTextValue("Mods.MagicStorage.LocatorSet", compassText, depthText));
				return true;
			}

			return base.RightClick(i, j);
		}
	}
}
