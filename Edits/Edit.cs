using Terraria.ModLoader;

namespace MagicStorage.Edits {
	public abstract class Edit : ILoadable {
		public Mod Mod { get; private set; }

		public void Load(Mod mod) {
			Mod = mod;

			LoadEdits();
		}

		public void Unload() {
			UnloadEdits();

			Mod = null;
		}

		public abstract void LoadEdits();

		public abstract void UnloadEdits();
	}
}
