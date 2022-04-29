using MagicStorage.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class RemoteAccess : StorageAccess
	{
		public override TERemoteAccess GetTileEntity() => ModContent.GetInstance<TERemoteAccess>();

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.RemoteAccess>();

		public override TEStorageHeart GetHeart(int i, int j)
		{
			if (TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity te) && te is TERemoteAccess remoteAccess)
				return remoteAccess.GetHeart();

			return null;
		}

		public override bool RightClick(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Item item = player.HeldItem;
			if (item.type == ModContent.ItemType<Locator>() || item.type == ModContent.ItemType<LocatorDisk>())
			{
				if (Main.tile[i, j].TileFrameX % 36 == 18)
					i--;
				if (Main.tile[i, j].TileFrameY % 36 == 18)
					j--;

				if (!TileEntity.ByPosition.TryGetValue(new Point16(i, j), out var te) || te is not TERemoteAccess remoteAccess)
					return false;

				Locator locator = (Locator)item.ModItem;
				if (remoteAccess.TryLocate(locator.location, out string message))
				{
					if (item.type == ModContent.ItemType<LocatorDisk>())
						locator.location = Point16.NegativeOne;
					else
						item.SetDefaults();
				}

				if (player.selectedItem == 58)
					Main.mouseItem = item.Clone();
				Main.NewText(message);
				return true;
			}

			return base.RightClick(i, j);
		}
	}
}
