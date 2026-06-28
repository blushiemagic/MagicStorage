using Terraria.DataStructures;

namespace MagicStorage {
	partial class Utility {
		public static TileEntity ResolveToTileEntity(this Point16 position) => position.X >= 0 && position.Y >= 0 && TileEntity.ByPosition.TryGetValue(position, out TileEntity entity) ? entity : null;

		public static T ResolveToTileEntity<T>(this Point16 position) where T : TileEntity => position.ResolveToTileEntity() is T entity ? entity : null;

		/// <summary>
		/// Returns a formatted string like <c>"(X: 10, Y: 25)"</c> for the given point
		/// </summary>
		public static string DebugString(this Point16 @this) => $"(X: {@this.X}, Y: {@this.Y})";
	}
}
