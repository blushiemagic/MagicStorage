using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class CraftingAccess : StorageAccess
	{
		public override ModTileEntity GetTileEntity() => mod.GetTileEntity("TECraftingAccess");

		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.CraftingAccess>();

		public override bool HasSmartInteract() => true;

		public override TEStorageHeart GetHeart(int i, int j)
		{
			Point16 point = TEStorageComponent.FindStorageCenter(new Point16(i, j));
			if (point.X < 0 || point.Y < 0)
			{
				return null;
			}

			if (TileEntity.ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageCenter center)
			{
				return center.GetHeart();
			}

			return null;
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (Main.tile[i, j].frameX > 0)
			{
				i--;
			}

			if (Main.tile[i, j].frameY > 0)
			{
				j--;
			}

			var pos = new Point16(i, j);
			if (TileEntity.ByPosition.TryGetValue(pos, out TileEntity te) && te is TECraftingAccess access)
			{
				if (access.stations.Any(item => !item.IsAir))
				{
					fail = true;
				}
			}
		}
	}
}
