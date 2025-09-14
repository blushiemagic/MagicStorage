using Terraria.DataStructures;

namespace MagicStorage {
	partial class Utility {
		public static TileEntity ResolveToTileEntity(this Point16 position) => position.X >= 0 && position.Y >= 0 && TileEntity.ByPosition.TryGetValue(position, out TileEntity entity) ? entity : null;

		public static T ResolveToTileEntity<T>(this Point16 position) where T : TileEntity => position.ResolveToTileEntity() is T entity ? entity : null;
	}
}
