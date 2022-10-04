using Terraria.ModLoader;

namespace MagicStorage.Items.ConfigDisplay {
	[Autoload(false)]
	internal class BaseConfigItem : ModItem {
		internal class Loadable : ILoadable {
			public void Load(Mod mod) {
				mod.AddContent(new BaseConfigItem("Config_GlowItemNotGlowing"));
				mod.AddContent(new BaseConfigItem("Config_GlowItemGlowing"));
				mod.AddContent(new BaseConfigItem("Config_RecipeFilterAvailable"));
				mod.AddContent(new BaseConfigItem("Config_RecipeFilterAll"));
			}

			public void Unload() { }
		}

		[CloneByReference]
		public readonly string name;

		public override string Name => name;

		protected override bool CloneNewInstances => true;

		public BaseConfigItem(string name) {
			this.name = name;
		}
	}
}
