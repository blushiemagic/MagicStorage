using MagicStorage.Common.Systems;
using MagicStorage.Sorting;
using MagicStorage.UI;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod {
	/// <summary>
	/// Defines a storage UI item filter that can be registered by Magic Storage or by another mod.
	/// </summary>
	public abstract partial class FilteringOption : ModTexturedType, ILocalizedModType {
		/// <summary>
		/// Gets the loader-assigned numeric identifier for this filtering option.
		/// </summary>
		public int Type { get; private set; }

		/// <inheritdoc/>
		public string LocalizationCategory => "FilteringOption";

		/// <summary>
		/// Gets the localized tooltip shown while hovering this option in the UI.
		/// </summary>
		public LocalizedText Tooltip => this.GetLocalization(nameof(Tooltip), PrettyPrintName);

		/// <summary>
		/// Gets the texture asset used for this option button.
		/// </summary>
		public Asset<Texture2D> TextureAsset => ModContent.Request<Texture2D>(Texture);

		/// <summary>
		/// The delegate that this filter uses.  <see cref="ItemFilter.Filter"/> takes an <see cref="Item"/> as input and returns a <see langword="bool"/>
		/// </summary>
		public abstract ItemFilter.Filter Filter { get; }

		/// <summary>
		/// Whether this filter is for a damage class of items<br/>
		/// If this property returns true, this filter is blacklisted by <see cref="ItemFilter.WeaponOther"/>
		/// </summary>
		public virtual bool FiltersDamageClass => false;

		/// <summary>
		/// Whether this filter uses its cached list of valid recipes from <see cref="MagicCache.FilteredRecipesCache"/>
		/// </summary>
		public virtual bool UsesFilterCache => true;

		/// <summary>
		/// Whether this filter is considered a "general" filter.<br/>
		/// Multiple general filters can be selected at once, and each acts as a whitelist for items rather than a blacklist.
		/// </summary>
		public virtual bool IsGeneralFilter => false;

		/// <inheritdoc/>
		protected sealed override void Register() {
			ModTypeLookup<FilteringOption>.Register(this);

			Type = FilteringOptionLoader.Add(this);
		}

		/// <inheritdoc/>
		public sealed override void SetupContent() {
			SetStaticDefaults();

			// Force the tooltip to be generated if it's not present
			_ = Tooltip;
		}

		/// <summary>
		/// Gets whether this option is currently visible in the option list.
		/// </summary>
		public bool Visible { get; private set; } = true;

		/// <summary>
		/// This method executes whenever this option is clicked in the UI
		/// </summary>
		/// <param name="choiceIndex">Which button set this option is currently assigned to</param>
		/// <param name="source">Which button index this option refers to</param>
		public virtual void OnSelected(NewUIButtonChoice source, int choiceIndex) { }

		private readonly List<FilteringOption> childrenBefore = new();
		/// <summary>
		/// Gets options that should be placed before this option after ordering is resolved.
		/// </summary>
		public IReadOnlyList<FilteringOption> ChildrenBefore => childrenBefore;

		private readonly List<FilteringOption> childrenAfter = new();
		/// <summary>
		/// Gets options that should be placed after this option after ordering is resolved.
		/// </summary>
		public IReadOnlyList<FilteringOption> ChildrenAfter => childrenAfter;

		/// <summary>
		/// Hides this option until visibility is reset from its default visibility rule.
		/// </summary>
		public void Hide() => Visible = false;

		internal void AddChildBefore(FilteringOption child) => childrenBefore.Add(child);
		internal void AddChildAfter(FilteringOption child) => childrenAfter.Add(child);

		internal void ClearChildren() {
			childrenBefore.Clear();
			childrenAfter.Clear();
		}

		/// <summary> Returns the option's default visibility. This is usually called as an option is read for refreshing logic, but modders can call it too for information. </summary>
		/// <param name="craftingGUI">Whether the option is being queried for the crafting UI instead of the storage UI.</param>
		/// <returns> Whether or not this option will be visible by default. Modders can hide options later, if needed.</returns>
		public virtual bool GetDefaultVisibility(bool craftingGUI) => true;

		/// <summary>
		/// Returns the option's default position in regards to other options.
		/// Make use of e.g <see cref="BeforeParent"/>/<see cref="AfterParent"/>, and provide an option (usually a default one from <see cref="FilteringOptionLoader"/>).
		/// </summary>
		public abstract Position GetDefaultPosition();

		internal void ResetVisibility(bool craftingGUI) {
			foreach (var child in ChildrenBefore)
				child.ResetVisibility(craftingGUI);

			Visible = GetDefaultVisibility(craftingGUI);

			foreach (var child in ChildrenAfter)
				child.ResetVisibility(craftingGUI);
		}

		/// <inheritdoc/>
		public override string ToString() => Name;
	}

	/// <summary>
	/// Runtime wrapper used when one filtering option needs to appear in multiple ordered slots.
	/// </summary>
	[Autoload(false)]
	public class FilteringOptionSlot : FilteringOption {
		/// <summary>
		/// Gets the original filtering option represented by this slot.
		/// </summary>
		public FilteringOption Option { get; }

		/// <summary>
		/// Gets the visibility condition for this slot.
		/// </summary>
		public Multiple.Condition Condition { get; }

		/// <inheritdoc/>
		public override ItemFilter.Filter Filter => Option.Filter;

		private readonly int _slot;

		/// <inheritdoc/>
		public override string Name => $"{Option.Name}_slot{_slot}";

		internal FilteringOptionSlot(FilteringOption option, Multiple.Condition cond, int slot) {
			Option = option;
			Condition = cond;
			_slot = slot;
			AddChildAfter(Option);
		}

		/// <inheritdoc/>
		public override Position GetDefaultPosition() => throw new NotImplementedException();

		/// <inheritdoc/>
		public override bool GetDefaultVisibility(bool craftingGUI) => Condition(craftingGUI);
	}

	internal class FilteringOptionElement : BaseOptionElement {
		public readonly FilteringOption option;

		public FilteringOptionElement(FilteringOption option) {
			this.option = option;
		}

		protected override string GetHoverText() => option.Tooltip.Value;

		protected override Asset<Texture2D> GetIcon() => option.TextureAsset;

		protected override bool IsSelected() => MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernConfigurable
			? MagicStorageMod.Instance.optionsConfig.filteringOptions[option.Type] is not null
			: option.Type == FilteringOptionLoader.Selected || FilteringOptionLoader.GeneralSelections.Contains(option.Type);

		protected override bool IsGeneralOption() => option.IsGeneralFilter;

		public override int CompareTo(object obj) {
			if (obj is not FilteringOptionElement other)
				return base.CompareTo(obj);

			return option.Type.CompareTo(other.option.Type);
		}
	}

	/// <summary>
	/// Provides access to registered filtering options and their resolved UI order.
	/// </summary>
	public static class FilteringOptionLoader {
		/// <summary>
		/// Stores references to Magic Storage's built-in filtering options after they are loaded.
		/// </summary>
		public static class Definitions {
			/// <summary>The built-in all-items filter.</summary>
			public static FilteringOption All { get; internal set; }
			/// <summary>The built-in weapon filter group.</summary>
			public static FilteringOption Weapon { get; internal set; }
			/// <summary>The built-in melee weapon filter.</summary>
			public static FilteringOption Melee { get; internal set; }
			/// <summary>The built-in ranged weapon filter.</summary>
			public static FilteringOption Ranged { get; internal set; }
			/// <summary>The built-in magic weapon filter.</summary>
			public static FilteringOption Magic { get; internal set; }
			/// <summary>The built-in summon weapon filter.</summary>
			public static FilteringOption Summon { get; internal set; }
			/// <summary>The built-in throwing weapon filter.</summary>
			public static FilteringOption Throwing { get; internal set; }
			/// <summary>The built-in ammunition filter.</summary>
			public static FilteringOption Ammo { get; internal set; }
			/// <summary>The built-in tools and fishing filter group.</summary>
			public static FilteringOption ToolsAndFishing { get; internal set; }
			/// <summary>The built-in tools filter.</summary>
			public static FilteringOption Tools { get; internal set; }
			/// <summary>The built-in fishing filter.</summary>
			public static FilteringOption Fishing { get; internal set; }
			/// <summary>The built-in armor and equipment filter group.</summary>
			public static FilteringOption ArmorAndEquips { get; internal set; }
			/// <summary>The built-in armor filter.</summary>
			public static FilteringOption Armor { get; internal set; }
			/// <summary>The built-in equipment filter.</summary>
			public static FilteringOption Equips { get; internal set; }
			/// <summary>The built-in vanity filter.</summary>
			public static FilteringOption Vanity { get; internal set; }
			/// <summary>The built-in potion filter.</summary>
			public static FilteringOption Potion { get; internal set; }
			/// <summary>The built-in placeable tile filter.</summary>
			public static FilteringOption Tiles { get; internal set; }
			/// <summary>The built-in miscellaneous gameplay item filter.</summary>
			public static FilteringOption MiscGameplayItems { get; internal set; }
			/// <summary>The built-in miscellaneous item filter.</summary>
			public static FilteringOption Misc { get; internal set; }
			/// <summary>The built-in recent-items filter.</summary>
			public static FilteringOption Recent { get; internal set; }
			/// <summary>The built-in non-standard weapon class filter.</summary>
			public static FilteringOption OtherWeapons { get; internal set; }
			/// <summary>The built-in unstackable-items general filter.</summary>
			public static FilteringOption Unstackables { get; internal set; }
			/// <summary>The built-in stackable-items general filter.</summary>
			public static FilteringOption Stackables { get; internal set; }
			/// <summary>The built-in not-fully-researched general filter.</summary>
			public static FilteringOption NotFullyResearched { get; internal set; }
			/// <summary>The built-in fully-researched general filter.</summary>
			public static FilteringOption FullyResearched { get; internal set; }
			/// <summary>The built-in material filter.</summary>
			public static FilteringOption Material { get; internal set; }
			/// <summary>The built-in selling-items general filter.</summary>
			public static FilteringOption SellingItems { get; internal set; }
		}

		private static readonly List<FilteringOption> allOptions = new();
		private static readonly List<FilteringOption> options = new();
		private static readonly List<FilteringOption> generalOptions = new();
		internal static readonly Dictionary<string, HashSet<string>> optionNames = new();

		/// <summary>
		/// Gets registered non-general filtering options.
		/// </summary>
		public static IReadOnlyList<FilteringOption> Options => options.AsReadOnly();

		/// <summary>
		/// Gets registered general filtering options.
		/// </summary>
		public static IReadOnlyList<FilteringOption> GeneralOptions => generalOptions.AsReadOnly();

		private static FilteringOption[] order;
		private static FilteringOption[] generalOrder;

		/// <summary>
		/// Gets the resolved non-general option order.
		/// </summary>
		public static IReadOnlyList<FilteringOption> Order => order;

		/// <summary>
		/// Gets the resolved general option order.
		/// </summary>
		public static IReadOnlyList<FilteringOption> GeneralOrder => generalOrder;

		/// <summary>
		/// Gets the currently selected non-general filtering option type.
		/// </summary>
		public static int Selected { get; internal set; }

		/// <summary>
		/// Gets the selected general filtering option types.
		/// </summary>
		public static HashSet<int> GeneralSelections { get; } = new();

		/// <summary>
		/// Gets the number of registered non-general filtering options.
		/// </summary>
		public static int Count => options.Count;

		/// <summary>
		/// Gets the number of registered general filtering options.
		/// </summary>
		public static int GeneralCount => generalOptions.Count;

		/// <summary>
		/// Gets the total number of registered filtering options.
		/// </summary>
		public static int TotalCount => allOptions.Count;

		internal static int Add(FilteringOption option) {
			//Ensure that the name doesn't conflict with a SortingOption
			if (SortingOptionLoader.optionNames.TryGetValue(option.Mod.Name, out var hash) && hash.Contains(option.Name))
				throw new Exception($"Cannot add a FilteringOption with the name \"{option.Mod.Name}:{option.Name}\".  A SortingOption with that name already exists.");

			int count = TotalCount;

			if (option.IsGeneralFilter) {
				generalOptions.Add(option);
				generalOrder = null;
			} else {
				options.Add(option);
				order = null;
			}

			allOptions.Add(option);

			if (!optionNames.TryGetValue(option.Mod.Name, out hash))
				optionNames[option.Mod.Name] = hash = new();

			hash.Add(option.Name);

			return count;
		}

		/// <summary>
		/// Gets a registered filtering option by loader index.
		/// </summary>
		/// <param name="index">The loader index to query.</param>
		/// <returns>The matching option, or <see langword="null"/> when <paramref name="index"/> is out of range.</returns>
		public static FilteringOption Get(int index) => index < 0 || index >= allOptions.Count ? null : allOptions[index];

		/// <summary>
		/// Gets the built-in filtering options shown as the base configurable choices.
		/// </summary>
		public static IEnumerable<FilteringOption> BaseOptions
			=> new FilteringOption[] {
				// Standard filters
				Definitions.All,
				Definitions.Weapon,
				Definitions.ToolsAndFishing,
				Definitions.ArmorAndEquips,
				Definitions.Potion,
				Definitions.Tiles,
				Definitions.Misc,
				Definitions.Recent,
				// General filters
				Definitions.Unstackables,
				Definitions.Stackables,
				Definitions.NotFullyResearched,
				Definitions.FullyResearched,
				Definitions.SellingItems
			};

		internal static void Load() {
			MagicStorageMod mod = MagicStorageMod.Instance;

			// Standard filters
			mod.AddContent(Definitions.All = new FilterAll());
			mod.AddContent(Definitions.Weapon = new FilterWeapons());
			mod.AddContent(Definitions.Melee = new FilterMelee());
			mod.AddContent(Definitions.Ranged = new FilterRanged());
			mod.AddContent(Definitions.Magic = new FilterMagic());
			mod.AddContent(Definitions.Summon = new FilterSummon());
			mod.AddContent(Definitions.Throwing = new FilterThrowing());
			mod.AddContent(Definitions.OtherWeapons = new FilterOtherWeaponClasses());
			mod.AddContent(Definitions.Ammo = new FilterAmmo());
			mod.AddContent(Definitions.ToolsAndFishing = new FilterToolsAndFishing());
			mod.AddContent(Definitions.Tools = new FilterTools());
			mod.AddContent(Definitions.Fishing = new FilterFishing());
			mod.AddContent(Definitions.ArmorAndEquips = new FilterArmorAndEquips());
			mod.AddContent(Definitions.Armor = new FilterArmor());
			mod.AddContent(Definitions.Equips = new FilterEquips());
			mod.AddContent(Definitions.Vanity = new FilterVanity());
			mod.AddContent(Definitions.Potion = new FilterPotion());
			mod.AddContent(Definitions.Tiles = new FilterTiles());
			mod.AddContent(Definitions.MiscGameplayItems = new FilterMiscGamePlayItems());
			mod.AddContent(Definitions.Material = new FilterMaterials());
			mod.AddContent(Definitions.Misc = new FilterMisc());
			mod.AddContent(Definitions.Recent = new FilterRecent());

			// General filters
			mod.AddContent(Definitions.Unstackables = new FilterUnstackables());
			mod.AddContent(Definitions.Stackables = new FilterStackables());
			mod.AddContent(Definitions.NotFullyResearched = new FilterNotFullyResearched());
			mod.AddContent(Definitions.FullyResearched = new FilterFullyResearched());
			mod.AddContent(Definitions.SellingItems = new FilterSellingItems());
		}

		internal static void Unload() {
			options.Clear();
			generalOptions.Clear();
			allOptions.Clear();
			Selected = 0;
			GeneralSelections.Clear();

			optionNames.Clear();

			foreach (var field in typeof(Definitions).GetFields().Where(f => f.FieldType == typeof(FilteringOption)))
				field.SetValue(null, null);
		}

		internal static void InitializeOrder() {
			InitializeOrder(Options, ref order);
			InitializeOrder(GeneralOptions, ref generalOrder);
		}

		private static void InitializeOrder(IReadOnlyList<FilteringOption> list, ref FilteringOption[] order) {
			var positions = list.ToDictionary(l => l, l => l.GetDefaultPosition());

			foreach (var (option, pos) in positions) {
				switch (pos) {
					case FilteringOption.Between _:
						continue;
					case FilteringOption.BeforeParent b:
						if (option.IsGeneralFilter != b.Parent.IsGeneralFilter)
							throw new ArgumentException($"FilteringOption {option} and its parent {b.Parent} have different IsGeneralFilter values");

						b.Parent.AddChildBefore(option);
						break;
					case FilteringOption.AfterParent a:
						if (option.IsGeneralFilter != a.Parent.IsGeneralFilter)
							throw new ArgumentException($"FilteringOption {option} and its parent {a.Parent} have different IsGeneralFilter values");

						a.Parent.AddChildAfter(option);
						break;
					case FilteringOption.Multiple m:
						int slot = 0;
						foreach (var (mulPos, cond) in m.Positions) {
							if (option.IsGeneralFilter != mulPos.Option1.IsGeneralFilter)
								throw new ArgumentException($"FilteringOption {option} and its parent {mulPos.Option1} have different IsGeneralFilter values");
							if (option.IsGeneralFilter != mulPos.Option2.IsGeneralFilter)
								throw new ArgumentException($"FilteringOption {option} and its parent {mulPos.Option2} have different IsGeneralFilter values");

							positions.Add(new FilteringOptionSlot(option, cond, slot++), mulPos);
						}
						break;
					default:
						throw new ArgumentException($"FilteringOption {option} has unknown Position type {pos}");
				}

				positions.Remove(option);
			}

			var sort = new TopoSort<FilteringOption>(positions.Keys,
				l => new[] { ((FilteringOption.Between)positions[l]).Option1 }.Where(l => l != null),
				l => new[] { ((FilteringOption.Between)positions[l]).Option2 }.Where(l => l != null));

			order = sort.Sort().ToArray();
		}

		/// <summary>
		/// Gets all filtering options after recalculating visibility for the requested UI.
		/// </summary>
		/// <param name="craftingGUI">Whether options are being queried for the crafting UI instead of the storage UI.</param>
		/// <returns>The ordered visible and hidden filtering options.</returns>
		public static IEnumerable<FilteringOption> GetOptions(bool craftingGUI) {
			foreach (var option in order)
				option.ResetVisibility(craftingGUI);
			foreach (var option in generalOrder)
				option.ResetVisibility(craftingGUI);

			return order.Concat(generalOrder);
		}

		/// <summary>
		/// Gets visible filtering options for the requested UI.
		/// </summary>
		/// <param name="craftingGUI">Whether options are being queried for the crafting UI instead of the storage UI.</param>
		/// <returns>The ordered filtering options whose current visibility is enabled.</returns>
		public static IEnumerable<FilteringOption> GetVisibleOptions(bool craftingGUI) => GetOptions(craftingGUI).Where(o => o.Visible);
	}
}
