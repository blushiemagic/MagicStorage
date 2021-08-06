using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class CraftingAccess : StorageAccess
	{
		public override ModTileEntity GetTileEntity() => ModContent.GetInstance<TECraftingAccess>();

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.CraftingAccess>();

		public override bool HasSmartInteract() => true;

		public override TEStorageHeart GetHeart(int i, int j)
		{
			Point16 point = TEStorageComponent.FindStorageCenter(new Point16(i, j));
			if (point.X < 0 || point.Y < 0 || !TileEntity.ByPosition.ContainsKey(point))
				return null;
			TileEntity heart = TileEntity.ByPosition[point];
			return heart is TEStorageCenter center ? center.GetHeart() : null;
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (Main.tile[i, j].frameX > 0)
				i--;
			if (Main.tile[i, j].frameY > 0)
				j--;
			Point16 pos = new(i, j);
			if (!TileEntity.ByPosition.ContainsKey(pos))
				return;
			if (TileEntity.ByPosition[new Point16(i, j)] is TECraftingAccess access)
				foreach (Item item in access.stations)
					if (!item.IsAir)
					{
						fail = true;
						break;
					}
		}
	}
}
