using Terraria.ModLoader;

namespace MagicStorage.Items.ShimmerDisplay {
	[Autoload(false)]
	internal class BaseShimmerItem : ModItem {
		internal class Loadable : ILoadable {
			public void Load(Mod mod) {
				mod.AddContent(new BaseShimmerItem("Shimmer_CoinLuck"));
			}

			public void Unload() { }
		}

		[CloneByReference]
		public readonly string name;

		public override string Name => name;

		protected override bool CloneNewInstances => true;

		public BaseShimmerItem(string name) {
			this.name = name;
		}
	}
}
