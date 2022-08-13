using MagicStorage.Common.Systems;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	internal class CursorLogicDetours : ILoadable {
		public Mod Mod { get; private set; } = null!;

		public void Load(Mod mod)
		{
			Mod = mod;

			On.Terraria.Main.DrawInterface_36_Cursor += Main_DrawInterface_36_Cursor;
		}

		private void Main_DrawInterface_36_Cursor(On.Terraria.Main.orig_DrawInterface_36_Cursor orig) {
			if (MagicUI.MouseCache.didBlockActions) {
				//Prevent the cursor from being drawn by the MouseText methods until we're good and ready to do so
				return;
			}

			orig();
		}

		public void Unload()
		{
			On.Terraria.Main.DrawInterface_36_Cursor -= Main_DrawInterface_36_Cursor;

			Mod = null!;
		}
	}
}
