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
	public abstract partial class FilteringOption : ModTexturedType, ILocalizedModType {
		public int Type { get; private set; }

		public string LocalizationCategory => "FilteringOption";

		public LocalizedText Tooltip => this.GetLocalization(nameof(Tooltip), PrettyPrintName);

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

		protected sealed override void Register() {
			ModTypeLookup<FilteringOption>.Register(this);

			Type = FilteringOptionLoader.Add(this);
		}

		public sealed override void SetupContent() {
			SetStaticDefaults();

			// Force the tooltip to be generated if it's not present
			_ = Tooltip;
		}

		public bool Visible { get; private set; } = true;

		/// <summary>
		/// This method executes whenever this option is clicked in the UI
		/// </summary>
		/// <param name="choiceIndex">Which button set this option is currently assigned to</param>
		/// <param name="source">Which button index this option refers to</param>
		public virtual void OnSelected(NewUIButtonChoice source, int choiceIndex) { }

		private readonly List<FilteringOption> childrenBefore = new();
		public IReadOnlyList<FilteringOption> ChildrenBefore => childrenBefore;

		private readonly List<FilteringOption> childrenAfter = new();
		public IReadOnlyList<FilteringOption> ChildrenAfter => childrenAfter;

		public void Hide() => Visible = false;

		internal void AddChildBefore(FilteringOption child) => childrenBefore.Add(child);
		internal void AddChildAfter(FilteringOption child) => childrenAfter.Add(child);

		internal void ClearChildren() {
			childrenBefore.Clear();
			childrenAfter.Clear();
		}

		/// <summary> Returns the layer's default visibility. This is usually called as a layer is queued for drawing, but modders can call it too for information. </summary>
		/// <returns> Whether or not this layer will be visible by default. Modders can hide layers later, if needed.</returns>
		public virtual bool GetDefaultVisibility(bool craftingGUI) => true;

		/// <summary>
		/// Returns the layer's default position in regards to other options.
		/// Make use of e.g <see cref="BeforeParent"/>/<see cref="AfterParent"/>, and provide an option (usually a vanilla one from <see cref="FilteringOptionLoader"/>).
		/// </summary>
		public abstract Position GetDefaultPosition();

		internal void ResetVisibility(bool craftingGUI) {
			foreach (var child in ChildrenBefore)
				child.ResetVisibility(craftingGUI);

			Visible = GetDefaultVisibility(craftingGUI);

			foreach (var child in ChildrenAfter)
				child.ResetVisibility(craftingGUI);
		}

		public override string ToString() => Name;
	}

	[Autoload(false)]
	public class FilteringOptionSlot : FilteringOption {
		public FilteringOption Option { get; }
		public Multiple.Condition Condition { get; }

		public override ItemFilter.Filter Filter => Option.Filter;

		private readonly int _slot;

		public override string Name => $"{Option.Name}_slot{_slot}";

		internal FilteringOptionSlot(FilteringOption option, Multiple.Condition cond, int slot) {
			Option = option;
			Condition = cond;
			_slot = slot;
			AddChildAfter(Option);
		}

		public override Position GetDefaultPosition() => throw new NotImplementedException();

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

	public static class FilteringOptionLoader {
		public static class Definitions {
			public static FilteringOption All { get; internal set; }
			public static FilteringOption Weapon { get; internal set; }
			public static FilteringOption Melee { get; internal set; }
			public static FilteringOption Ranged { get; internal set; }
			public static FilteringOption Magic { get; internal set; }
			public static FilteringOption Summon { get; internal set; }
			public static FilteringOption Throwing { get; internal set; }
			public static FilteringOption Ammo { get; internal set; }
			public static FilteringOption ToolsAndFishing { get; internal set; }
			public static FilteringOption Tools { get; internal set; }
			public static FilteringOption Fishing { get; internal set; }
			public static FilteringOption ArmorAndEquips { get; internal set; }
			public static FilteringOption Armor { get; internal set; }
			public static FilteringOption Equips { get; internal set; }
			public static FilteringOption Vanity { get; internal set; }
			public static FilteringOption Potion { get; internal set; }
			public static FilteringOption Tiles { get; internal set; }
			public static FilteringOption MiscGameplayItems { get; internal set; }
			public static FilteringOption Misc { get; internal set; }
			public static FilteringOption Recent { get; internal set; }
			public static FilteringOption OtherWeapons { get; internal set; }
			public static FilteringOption Unstackables { get; internal set; }
			public static FilteringOption Stackables { get; internal set; }
			public static FilteringOption NotFullyResearched { get; internal set; }
			public static FilteringOption FullyResearched { get; internal set; }
			public static FilteringOption Material { get; internal set; }
		}

		private static readonly List<FilteringOption> allOptions = new();
		private static readonly List<FilteringOption> options = new();
		private static readonly List<FilteringOption> generalOptions = new();
		internal static readonly Dictionary<string, HashSet<string>> optionNames = new();

		public static IReadOnlyList<FilteringOption> Options => options.AsReadOnly();

		public static IReadOnlyList<FilteringOption> GeneralOptions => generalOptions.AsReadOnly();

		private static FilteringOption[] order;
		private static FilteringOption[] generalOrder;

		public static IReadOnlyList<FilteringOption> Order => order;

		public static IReadOnlyList<FilteringOption> GeneralOrder => generalOrder;

		public static int Selected { get; internal set; }

		public static HashSet<int> GeneralSelections { get; } = new();

		public static int Count => options.Count;

		public static int GeneralCount => generalOptions.Count;

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

		public static FilteringOption Get(int index) => index < 0 || index >= allOptions.Count ? null : allOptions[index];

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
				Definitions.FullyResearched
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

		public static IEnumerable<FilteringOption> GetOptions(bool craftingGUI) {
			foreach (var option in order)
				option.ResetVisibility(craftingGUI);
			foreach (var option in generalOrder)
				option.ResetVisibility(craftingGUI);

			return order.Concat(generalOrder);
		}

		public static IEnumerable<FilteringOption> GetVisibleOptions(bool craftingGUI) => GetOptions(craftingGUI).Where(o => o.Visible);
	}
}
