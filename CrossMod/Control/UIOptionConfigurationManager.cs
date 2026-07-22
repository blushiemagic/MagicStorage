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
	/// <summary>
	/// Loads, saves, and applies the configurable sorting and filtering option lists used by Magic Storage UIs.
	/// </summary>
	public sealed class UIOptionConfigurationManager {
		/// <summary>
		/// Serializable reference to a sorting or filtering option by mod and internal name.
		/// </summary>
		public struct OptionDefinition {
			/// <summary>
			/// The owning mod name, or <see langword="null"/> for Magic Storage built-in options.
			/// </summary>
			public readonly string mod;
			/// <summary>
			/// The option's internal <see cref="ModType.Name"/>.
			/// </summary>
			public readonly string name;

			/// <summary>
			/// Gets whether the referenced option is currently loaded.
			/// </summary>
			public bool Exists => def != 0;

			/// <summary>
			/// Gets whether this definition references a <see cref="SortingOption"/>.
			/// </summary>
			public bool DefinesSorter => def == 1;

			/// <summary>
			/// Gets whether this definition references a <see cref="FilteringOption"/>.
			/// </summary>
			public bool DefinesFilter => def == 2;

			internal readonly int def;

			internal OptionDefinition(string name) : this(null, name) { }

			/// <summary>
			/// Creates a definition from a mod name and option name.
			/// </summary>
			public OptionDefinition(string mod, string name) {
				this.mod = string.IsNullOrEmpty(mod) ? null : mod;
				this.name = name;
				def = 0;

				def = GetSortOption() is not null ? 1 : GetFilterOption() is not null ? 2 : 0;
			}

			/// <summary>
			/// Creates a definition for a sorting option.
			/// </summary>
			public OptionDefinition(SortingOption option) {
				mod = option.Mod == MagicStorageMod.Instance ? null : option.Mod.Name;
				name = option.Name;
				def = 1;
			}

			/// <summary>
			/// Creates a definition for a filtering option.
			/// </summary>
			public OptionDefinition(FilteringOption option) {
				mod = option.Mod == MagicStorageMod.Instance ? null : option.Mod.Name;
				name = option.Name;
				def = 2;
			}

			/// <summary>
			/// Resolves this definition to its loaded sorting option, or <see langword="null"/> if unavailable.
			/// </summary>
			public SortingOption GetSortOption() => ModLoader.TryGetMod(mod ?? "MagicStorage", out Mod source) && source.TryFind(name, out SortingOption option) ? option : null;

			/// <summary>
			/// Resolves this definition to its loaded filtering option, or <see langword="null"/> if unavailable.
			/// </summary>
			public FilteringOption GetFilterOption() => ModLoader.TryGetMod(mod ?? "MagicStorage", out Mod source) && source.TryFind(name, out FilteringOption option) ? option : null;

			/// <summary>
			/// Serializes this definition to persistent config data.
			/// </summary>
			public TagCompound SerializeData()
				=> new() {
					["mod"] = mod,
					["name"] = name
				};

			/// <summary>
			/// Deserializes an option definition from persistent config data.
			/// </summary>
			public static OptionDefinition DeserializeData(TagCompound tag) => new(tag.GetString("mod"), tag.GetString("name"));
		}

		internal OptionDefinition?[] sortingOptions, filteringOptions;

		internal List<OptionDefinition> unloadedOptions;

		//Normally i'd just use consts here, but that causes VS debugging to crash for whatever reason
		// -- absoluteAquarian
		/// <summary>
		/// Relative folder under <see cref="Main.SavePath"/> where the option configuration file is stored.
		/// </summary>
		public static readonly string RelativeDestinationFolder = "ModConfigs";
		/// <summary>
		/// File name used for the option configuration data.
		/// </summary>
		public static readonly string RelativeDestinationFile = "MagicStorage_Options.nbt";

		/// <summary>
		/// Full folder path where the option configuration file is stored.
		/// </summary>
		public static string DestinationFolder => Path.Combine(Main.SavePath, RelativeDestinationFolder);
		/// <summary>
		/// Full path to the option configuration file.
		/// </summary>
		public static string DestinationPath => Path.Combine(Main.SavePath, RelativeDestinationFolder, RelativeDestinationFile);

		/// <summary>
		/// Toggles whether a sorting option is enabled in configurable button mode.
		/// </summary>
		public void ToggleEnabled(SortingOption option) => SetEnabled(option, sortingOptions[option.Type] is null);

		/// <summary>
		/// Sets whether a sorting option is enabled in configurable button mode.
		/// </summary>
		public void SetEnabled(SortingOption option, bool enabled) {
			sortingOptions[option.Type] = enabled ? new(option) : null;

			if (MagicUI.craftingUI?.TryGetDefaultPage(out CraftingUIState.RecipesPage recipesPage) is true)
				recipesPage.pendingConfiguration = true;

			if (MagicUI.storageUI?.TryGetDefaultPage(out StorageUIState.StoragePage storagePage) is true)
				storagePage.pendingConfiguration = true;

			//Default to the first available option if this option was removed and it's selected
			if (!enabled && SortingOptionLoader.Selected == option.Type) {
				bool craftingGUI = !Main.gameMenu && StoragePlayer.IsStorageCraftingOrDecrafting();
				var options = GetSortingOptions(craftingGUI);

				SortingOptionLoader.Selected = !options.Any() ? -1 : options.First().Type;
			}
		}

		/// <summary>
		/// Toggles whether a filtering option is enabled in configurable button mode.
		/// </summary>
		public void ToggleEnabled(FilteringOption option) => SetEnabled(option, filteringOptions[option.Type] is null);

		/// <summary>
		/// Sets whether a filtering option is enabled in configurable button mode.
		/// </summary>
		public void SetEnabled(FilteringOption option, bool enabled) {
			filteringOptions[option.Type] = enabled ? new(option) : null;

			if (MagicUI.craftingUI?.TryGetDefaultPage(out CraftingUIState.RecipesPage recipesPage) is true)
				recipesPage.pendingConfiguration = true;

			if (MagicUI.storageUI?.TryGetDefaultPage(out StorageUIState.StoragePage storagePage) is true)
				storagePage.pendingConfiguration = true;

			//Default to the first available option if this option was removed and it's selected
			if (!enabled) {
				if (!option.IsGeneralFilter && FilteringOptionLoader.Selected == option.Type) {
					bool craftingGUI = !Main.gameMenu && StoragePlayer.IsStorageCraftingOrDecrafting();
					var options = GetFilteringOptions(craftingGUI).Where(o => !o.IsGeneralFilter);

					FilteringOptionLoader.Selected = !options.Any() ? -1 : options.First().Type;
				} else if (option.IsGeneralFilter)
					FilteringOptionLoader.GeneralSelections.Remove(option.Type);
			}
		}

		internal void Initialize() {
			try {
				Directory.CreateDirectory(DestinationFolder);

				if (!File.Exists(DestinationPath)) {
					//No file?  Default to a base configuration
					goto UseDefault;
				} else {
					try {
						TagCompound tag = TagIO.FromFile(DestinationPath);

						if (tag.GetList<TagCompound>("options") is not { Count: >0 } tags) {
							MagicStorageMod.Instance.Logger.Warn("Options file \"" + RelativeDestinationFile + "\" was malformed");
							goto UseDefault;
						}

						List<OptionDefinition> options = tags.Select(OptionDefinition.DeserializeData).ToList();

						sortingOptions = BuildArray(options.Where(o => o.DefinesSorter), o => o.GetSortOption().Type, SortingOptionLoader.Count);
						filteringOptions = BuildArray(options.Where(o => o.DefinesFilter), o => o.GetFilterOption().Type, FilteringOptionLoader.TotalCount);
						unloadedOptions = options.Where(o => !o.Exists).ToList();
						return;
					} catch {
						MagicStorageMod.Instance.Logger.Warn("Options file \"" + RelativeDestinationFile + "\" was malformed");
						goto UseDefault;
					}
				}
			} catch {
				MagicStorageMod.Instance.Logger.Warn("Options file \"" + RelativeDestinationFile + "\" could not be loaded");
				goto UseDefault;
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

		/// <summary>
		/// Gets enabled sorting options that are visible for the requested UI.
		/// </summary>
		public IEnumerable<SortingOption> GetSortingOptions(bool craftingGUI) => SortingOptionLoader.GetVisibleOptions(craftingGUI).Where(o => sortingOptions[o.Type] is not null);

		/// <summary>
		/// Gets enabled filtering options that are visible for the requested UI.
		/// </summary>
		public IEnumerable<FilteringOption> GetFilteringOptions(bool craftingGUI) => FilteringOptionLoader.GetVisibleOptions(craftingGUI).Where(o => filteringOptions[o.Type] is not null);

		private static OptionDefinition?[] BuildArray(IEnumerable<SortingOption> options) {
			OptionDefinition?[] result = new OptionDefinition?[SortingOptionLoader.Count];

			foreach (var option in options)
				result[option.Type] = new(option);

			return result;
		}

		private static OptionDefinition?[] BuildArray(IEnumerable<FilteringOption> options) {
			OptionDefinition?[] result = new OptionDefinition?[FilteringOptionLoader.TotalCount];

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
