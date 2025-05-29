using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.CrossMod.Calls.Functions {
	[Autoload(false)]
	internal class DynamicAggregator : StorageAggregator {
		public string name;
		public override string Name => name;

		public Func<Item, bool> appliesToItem;
		public override bool AppliesToItem(Item item) => appliesToItem(item);

		public Func<Item, Item, bool?> canAggregateItems;
		public override bool? CanAggregateItems(Item destination, Item checking) => canAggregateItems?.Invoke(destination, checking) ?? base.CanAggregateItems(destination, checking);
	}

	[Autoload(false)]
	internal class DynamicAggregator_SelectData : StorageAggregator {
		public string name;
		public override string Name => name;

		public Func<Item, bool> appliesToItem;
		public override bool AppliesToItem(Item item) => appliesToItem(item);

		public Func<Item, Item, bool?> canAggregateItems;
		public override bool? CanAggregateItems(Item destination, Item checking) => canAggregateItems?.Invoke(destination, checking) ?? base.CanAggregateItems(destination, checking);

		public Action<ModItem, TagCompound> selectData;
		public override void SelectData(ModItem item, TagCompound tag) => selectData(item, tag);
	}

	[Autoload(false)]
	internal class DynamicAggregator_SelectGlobalData : StorageAggregator {
		public string name;
		public override string Name => name;

		public Func<Item, bool> appliesToItem;
		public override bool AppliesToItem(Item item) => appliesToItem(item);

		public Func<Item, Item, bool?> canAggregateItems;
		public override bool? CanAggregateItems(Item destination, Item checking) => canAggregateItems?.Invoke(destination, checking) ?? base.CanAggregateItems(destination, checking);

		public Action<GlobalItem, TagCompound> selectGlobalData;
		public override void SelectGlobalData(GlobalItem item, TagCompound tag) => selectGlobalData(item, tag);
	}

	[Autoload(false)]
	internal class DynamicAggregator_SelectAllData : StorageAggregator {
		public string name;
		public override string Name => name;

		public Func<Item, bool> appliesToItem;
		public override bool AppliesToItem(Item item) => appliesToItem(item);

		public Func<Item, Item, bool?> canAggregateItems;
		public override bool? CanAggregateItems(Item destination, Item checking) => canAggregateItems?.Invoke(destination, checking) ?? base.CanAggregateItems(destination, checking);

		public Action<ModItem, TagCompound> selectData;
		public override void SelectData(ModItem item, TagCompound tag) => selectData(item, tag);

		public Action<GlobalItem, TagCompound> selectGlobalData;
		public override void SelectGlobalData(GlobalItem item, TagCompound tag) => selectGlobalData(item, tag);
	}

	internal class CreateAggregator : BaseCallFunction {
		public override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 3);

			return Handle(
				GetOrThrowIfNot<Mod>(args, 0),
				GetOrThrowIfNot<string>(args, 1),
				GetOrThrowIfNot<Func<Item, bool>>(args, 2),
				GetOrThrowIfNotNothing<Func<Item, Item, bool?>>(args, 3),
				GetOrThrowIfNotNothing<Action<ModItem, TagCompound>>(args, 4),
				GetOrThrowIfNotNothing<Action<GlobalItem, TagCompound>>(args, 5)
			);
		}

		private static bool Handle(Mod mod, string name, Func<Item, bool> appliesToItem, NothingOr<Func<Item, Item, bool?>> canAggregateItems,
			NothingOr<Action<ModItem, TagCompound>> selectData, NothingOr<Action<GlobalItem, TagCompound>> selectGlobalData) {
			StorageAggregator instance;
			if (selectData.IsValue && selectGlobalData.IsValue) {
				DynamicAggregator_SelectAllData aggregator = new() {
					name = name,
					appliesToItem = appliesToItem,
					canAggregateItems = canAggregateItems.Value,
					selectData = selectData.Value,
					selectGlobalData = selectGlobalData.Value
				};
				instance = aggregator;
			} else if (selectData.IsValue) {
				DynamicAggregator_SelectData aggregator = new() {
					name = name,
					appliesToItem = appliesToItem,
					canAggregateItems = canAggregateItems.Value,
					selectData = selectData.Value
				};
				instance = aggregator;
			} else if (selectGlobalData.IsValue) {
				DynamicAggregator_SelectGlobalData aggregator = new() {
					name = name,
					appliesToItem = appliesToItem,
					canAggregateItems = canAggregateItems.Value,
					selectGlobalData = selectGlobalData.Value
				};
				instance = aggregator;
			} else {
				DynamicAggregator aggregator = new() {
					name = name,
					appliesToItem = appliesToItem,
					canAggregateItems = canAggregateItems.Value
				};
				instance = aggregator;
			}

			return mod.AddContent(instance);
		}
	}
}
