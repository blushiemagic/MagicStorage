using MagicStorage.CrossMod;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Items.ErrorDisplay {
	[Autoload(false)]
	internal class BaseErrorDummyItem : ModItem {
		internal class Loadable : ILoadable {
			public void Load(Mod mod) {
				ModItem item;
				mod.AddContent(item = new BaseErrorDummyItem("Error_ItemSlotRenderFail"));
				RenderFailItemType = item.Type;

				mod.AddContent(item = new BaseErrorDummyItem("Error_ItemNetReadFail"));
				NetReadFailItemType = item.Type;
			}

			public void Unload() {
				RenderFailItemType = 0;
				NetReadFailItemType = 0;
			}
		}

		internal const bool LoadTestItems = false;

		public static int RenderFailItemType { get; private set; }

		public static int NetReadFailItemType { get; private set; }

		[CloneByReference]
		public readonly string name;

		public override string Name => name;

		protected override bool CloneNewInstances => true;

		public BaseErrorDummyItem(string name) {
			this.name = name;
		}

		public override void SetDefaults() {
			Item.maxStack = int.MaxValue;
		}

		internal class Aggregator : StorageAggregator {
			public override bool AppliesToItem(Item item) => item.ModItem is BaseErrorDummyItem;

			public override bool? CanAggregateItems(Item destination, Item checking) => false;
		}
	}
}
