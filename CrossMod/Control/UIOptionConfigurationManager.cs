using MagicStorage.Common.Systems;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.CrossMod.Control {
	public sealed class UIOptionConfigurationManager {
		public struct OptionDefinition {
			public readonly string mod, name;

			public bool Exists => def != 0;

			public bool DefinesSorter => def == 1;

			public bool DefinesFilter => def == 2;

			internal readonly int def;

			internal OptionDefinition(string name) : this(null, name) { }

			public OptionDefinition(string mod, string name) {
				this.mod = string.IsNullOrEmpty(mod) ? null : mod;
				this.name = name;
				def = 0;

				def = GetSortOption() is not null ? 1 : GetFilterOption() is not null ? 2 : 0;
			}

			public OptionDefinition(SortingOption option) {
				mod = option.Mod == MagicStorageMod.Instance ? null : option.Mod.Name;
				name = option.Name;
				def = 1;
			}

			public OptionDefinition(FilteringOption option) {
				mod = option.Mod == MagicStorageMod.Instance ? null : option.Mod.Name;
				name = option.Name;
				def = 2;
			}

			public SortingOption GetSortOption() => ModLoader.TryGetMod(mod ?? "MagicStorage", out Mod source) && source.TryFind(name, out SortingOption option) ? option : null;

			public FilteringOption GetFilterOption() => ModLoader.TryGetMod(mod ?? "MagicStorage", out Mod source) && source.TryFind(name, out FilteringOption option) ? option : null;

			public TagCompound SerializeData()
				=> new() {
					["mod"] = mod,
					["name"] = name
				};

			public static OptionDefinition DeserializeData(TagCompound tag) => new(tag.GetString("mod"), tag.GetString("name"));
		}

		internal OptionDefinition?[] sortingOptions, filteringOptions;

		internal List<OptionDefinition> unloadedOptions;

		//Normally i'd just use consts here, but that causes VS debugging to crash for whatever reason
		// -- absoluteAquarian
		public static readonly string RelativeDestinationFolder = "ModConfigs";
		public static readonly string RelativeDestinationFile = "MagicStorage_Options.nbt";

		public static string DestinationFolder => Path.Combine(Main.SavePath, RelativeDestinationFolder);
		public static string DestinationPath => Path.Combine(Main.SavePath, RelativeDestinationFolder, RelativeDestinationFile);

		public void ToggleEnabled(SortingOption option) => SetEnabled(option, sortingOptions[option.Type] is null);

		public void SetEnabled(SortingOption option, bool enabled) {
			sortingOptions[option.Type] = enabled ? new(option) : null;

			if (MagicUI.craftingUI?.TryGetDefaultPage(out CraftingUIState.RecipesPage recipesPage) is true)
				recipesPage.pendingConfiguration = true;

			if (MagicUI.storageUI?.TryGetDefaultPage(out StorageUIState.StoragePage storagePage) is true)
				storagePage.pendingConfiguration = true;

			//Default to the first available option if this option was removed and it's selected
			if (!enabled && SortingOptionLoader.Selected == option.Type) {
				bool craftingGUI = !Main.gameMenu && StoragePlayer.LocalPlayer.StorageCrafting();
				var options = GetSortingOptions(craftingGUI);

				SortingOptionLoader.Selected = !options.Any() ? -1 : options.First().Type;
			}
		}

		public void ToggleEnabled(FilteringOption option) => SetEnabled(option, filteringOptions[option.Type] is null);

		public void SetEnabled(FilteringOption option, bool enabled) {
			filteringOptions[option.Type] = enabled ? new(option) : null;

			if (MagicUI.craftingUI?.TryGetDefaultPage(out CraftingUIState.RecipesPage recipesPage) is true)
				recipesPage.pendingConfiguration = true;

			if (MagicUI.storageUI?.TryGetDefaultPage(out StorageUIState.StoragePage storagePage) is true)
				storagePage.pendingConfiguration = true;

			//Default to the first available option if this option was removed and it's selected
			if (!enabled && FilteringOptionLoader.Selected == option.Type) {
				bool craftingGUI = !Main.gameMenu && StoragePlayer.LocalPlayer.StorageCrafting();
				var options = GetFilteringOptions(craftingGUI);

				FilteringOptionLoader.Selected = !options.Any() ? -1 : options.First().Type;
			}
		}

		internal void Initialize() {
			Directory.CreateDirectory(DestinationFolder);

			if (!File.Exists(DestinationPath)) {
				//No file?  Default to a base configuration
				goto UseDefault;
			} else {
				try {
					TagCompound tag = TagIO.FromFile(DestinationPath);

					if (tag.GetList<TagCompound>("options") is not List<TagCompound> tags) {
						MagicStorageMod.Instance.Logger.Warn("Options file \"" + RelativeDestinationFile + "\" was malformed");
						goto UseDefault;
					}

					List<OptionDefinition> options = tags.Select(OptionDefinition.DeserializeData).ToList();

					sortingOptions = BuildArray(options.Where(o => o.DefinesSorter), o => o.GetSortOption().Type, SortingOptionLoader.Count);
					filteringOptions = BuildArray(options.Where(o => o.DefinesFilter), o => o.GetFilterOption().Type, FilteringOptionLoader.Count);
					unloadedOptions = options.Where(o => !o.Exists).ToList();
					return;
				} catch {
					MagicStorageMod.Instance.Logger.Warn("Options file \"" + RelativeDestinationFile + "\" was malformed");
					goto UseDefault;
				}
			}

			UseDefault:
			sortingOptions = BuildArray(SortingOptionLoader.BaseOptions);
			filteringOptions = BuildArray(FilteringOptionLoader.BaseOptions);
			unloadedOptions = new();
			Save();
		}

		internal void Save() {
			List<OptionDefinition> options = sortingOptions.OfType<OptionDefinition>()
				.Concat(filteringOptions.OfType<OptionDefinition>())
				.Concat(unloadedOptions)
				.OrderByDescending(o => o.mod is null ? 1 : 0)
				.ThenByDescending(o => o.mod ?? "MagicStorage")
				.ThenByDescending(o => o.name)
				.ToList();

			TagCompound root = new() {
				["options"] = options.Select(o => o.SerializeData()).ToList()
			};

			TagIO.ToFile(root, DestinationPath);
		}

		public IEnumerable<SortingOption> GetSortingOptions(bool craftingGUI) => SortingOptionLoader.GetVisibleOptions(craftingGUI).Where(o => sortingOptions[o.Type] is not null);

		public IEnumerable<FilteringOption> GetFilteringOptions(bool craftingGUI) => FilteringOptionLoader.GetVisibleOptions(craftingGUI).Where(o => filteringOptions[o.Type] is not null);

		private static OptionDefinition?[] BuildArray(IEnumerable<SortingOption> options) {
			OptionDefinition?[] result = new OptionDefinition?[SortingOptionLoader.Count];

			foreach (var option in options)
				result[option.Type] = new(option);

			return result;
		}

		private static OptionDefinition?[] BuildArray(IEnumerable<FilteringOption> options) {
			OptionDefinition?[] result = new OptionDefinition?[FilteringOptionLoader.Count];

			foreach (var option in options)
				result[option.Type] = new(option);

			return result;
		}

		private static OptionDefinition?[] BuildArray(IEnumerable<OptionDefinition> options, Func<OptionDefinition, int> getIndex, int count) {
			OptionDefinition?[] result = new OptionDefinition?[count];

			foreach (var option in options)
				result[getIndex(option)] = option;

			return result;
		}
	}
}
