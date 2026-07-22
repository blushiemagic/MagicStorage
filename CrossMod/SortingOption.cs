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
	/// Defines a storage UI item sorter that can be registered by Magic Storage or by another mod.
	/// </summary>
	public abstract partial class SortingOption : ModTexturedType, ILocalizedModType {
		/// <summary>
		/// Gets the loader-assigned numeric identifier for this sorting option.
		/// </summary>
		public int Type { get; private set; }

		/// <inheritdoc/>
		public string LocalizationCategory => "SortingOption";

		/// <summary>
		/// Gets the localized tooltip shown while hovering this option in the UI.
		/// </summary>
		public LocalizedText Tooltip => this.GetLocalization(nameof(Tooltip), PrettyPrintName);

		/// <summary>
		/// Gets the texture asset used for this option button.
		/// </summary>
		public Asset<Texture2D> TextureAsset => ModContent.Request<Texture2D>(Texture);

		/// <summary>
		/// Gets the comparer used to order matching items.
		/// </summary>
		public abstract IComparer<Item> Sorter { get; }

		/// <summary>
		/// Whether <see cref="Sorter"/> is used again after calculating sort order from fuzzy sorting
		/// </summary>
		public virtual bool SortAgainAfterFuzzy => false;

		/// <summary>
		/// Whether <see cref="Sorter"/> is used during mod loading to initialze a collection for fuzzy sorting.<br/>
		/// <b>NOTE:</b> Not performing fuzzy sorting will usually slow down sorting.  Please use this property only when necessary.
		/// </summary>
		public virtual bool CacheFuzzySorting => true;

		/// <summary>
		/// Whether <see cref="Sorter"/> orders items in descending order.  Defaults to <see langword="false"/>.
		/// </summary>
		public virtual bool SortInDescendingOrder => false;

		/// <inheritdoc/>
		protected sealed override void Register() {
			ModTypeLookup<SortingOption>.Register(this);

			Type = SortingOptionLoader.Add(this);
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

		private readonly List<SortingOption> childrenBefore = new();
		/// <summary>
		/// Gets options that should be placed before this option after ordering is resolved.
		/// </summary>
		public IReadOnlyList<SortingOption> ChildrenBefore => childrenBefore;

		private readonly List<SortingOption> childrenAfter = new();
		/// <summary>
		/// Gets options that should be placed after this option after ordering is resolved.
		/// </summary>
		public IReadOnlyList<SortingOption> ChildrenAfter => childrenAfter;

		/// <summary>
		/// Hides this option until visibility is reset from its default visibility rule.
		/// </summary>
		public void Hide() => Visible = false;

		internal void AddChildBefore(SortingOption child) => childrenBefore.Add(child);
		internal void AddChildAfter(SortingOption child) => childrenAfter.Add(child);

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
		/// Make use of e.g <see cref="BeforeParent"/>/<see cref="AfterParent"/>, and provide an option (usually a default one from <see cref="SortingOptionLoader"/>).
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
	/// Runtime wrapper used when one sorting option needs to appear in multiple ordered slots.
	/// </summary>
	[Autoload(false)]
	public class SortingOptionSlot : SortingOption {
		/// <summary>
		/// Gets the original sorting option represented by this slot.
		/// </summary>
		public SortingOption Option { get; }

		/// <summary>
		/// Gets the visibility condition for this slot.
		/// </summary>
		public Multiple.Condition Condition { get; }

		/// <inheritdoc/>
		public override IComparer<Item> Sorter => Option.Sorter;

		private readonly int _slot;

		/// <inheritdoc/>
		public override string Name => $"{Option.Name}_slot{_slot}";

		internal SortingOptionSlot(SortingOption option, Multiple.Condition cond, int slot) {
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

	internal class SortingOptionElement : BaseOptionElement {
		public readonly SortingOption option;

		public SortingOptionElement(SortingOption option) {
			this.option = option;
		}

		protected override string GetHoverText() => option.Tooltip.Value;

		protected override Asset<Texture2D> GetIcon() => option.TextureAsset;

		protected override bool IsSelected() => MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernConfigurable
			? MagicStorageMod.Instance.optionsConfig.sortingOptions[option.Type] is not null
			: option.Type == SortingOptionLoader.Selected;

		protected override bool IsGeneralOption() => false;

		public override int CompareTo(object obj) {
			if (obj is not SortingOptionElement other)
				return base.CompareTo(obj);

			return option.Type.CompareTo(other.option.Type);
		}
	}

	/// <summary>
	/// Provides access to registered sorting options and their resolved UI order.
	/// </summary>
	public static class SortingOptionLoader {
		/// <summary>
		/// Stores references to Magic Storage's built-in sorting options after they are loaded.
		/// </summary>
		public static class Definitions {
			/// <summary>The built-in default sorter.</summary>
			public static SortingOption Default { get; internal set; }
			/// <summary>The built-in item ID sorter.</summary>
			public static SortingOption ID { get; internal set; }
			/// <summary>The built-in item name sorter.</summary>
			public static SortingOption Name { get; internal set; }
			/// <summary>The built-in item value sorter.</summary>
			public static SortingOption Value { get; internal set; }
			/// <summary>The built-in stack quantity sorter.</summary>
			public static SortingOption Quantity { get; internal set; }
			/// <summary>The built-in stack fill-ratio sorter.</summary>
			public static SortingOption QuantityRatio { get; internal set; }
			/// <summary>The built-in damage sorter.</summary>
			public static SortingOption Damage { get; internal set; }
		}

		private static readonly List<SortingOption> options = new();
		internal static readonly Dictionary<string, HashSet<string>> optionNames = new();

		/// <summary>
		/// Gets all registered sorting options.
		/// </summary>
		public static IReadOnlyList<SortingOption> Options => options.AsReadOnly();

		private static SortingOption[] order;

		/// <summary>
		/// Gets the resolved sorting option order.
		/// </summary>
		public static IReadOnlyList<SortingOption> Order => order;

		/// <summary>
		/// Gets the currently selected sorting option type.
		/// </summary>
		public static int Selected { get; internal set; }

		/// <summary>
		/// Gets the number of registered sorting options.
		/// </summary>
		public static int Count => options.Count;

		internal static int Add(SortingOption option) {
			//Ensure that the name doesn't conflict with a FilteringOption
			if (FilteringOptionLoader.optionNames.TryGetValue(option.Mod.Name, out var hash) && hash.Contains(option.Name))
				throw new Exception($"Cannot add a SortingOption with the name \"{option.Mod.Name}:{option.Name}\".  A FilteringOption with that name already exists.");

			int count = Count;

			options.Add(option);

			if (!optionNames.TryGetValue(option.Mod.Name, out hash))
				optionNames[option.Mod.Name] = hash = new();

			hash.Add(option.Name);

			order = null;

			return count;
		}

		/// <summary>
		/// Gets a registered sorting option by loader index.
		/// </summary>
		/// <param name="index">The loader index to query.</param>
		/// <returns>The matching option, or <see langword="null"/> when <paramref name="index"/> is out of range.</returns>
		public static SortingOption Get(int index) => index < 0 || index >= options.Count ? null : options[index];

		/// <summary>
		/// Gets the built-in sorting options shown as the base configurable choices.
		/// </summary>
		public static IEnumerable<SortingOption> BaseOptions
			=> new SortingOption[] {
				Definitions.Default,
				Definitions.ID,
				Definitions.Name,
				Definitions.Value,
				Definitions.Damage,
				Definitions.Quantity
			};

		internal static void Load() {
			MagicStorageMod mod = MagicStorageMod.Instance;

			mod.AddContent(Definitions.Default = new SortDefault());
			mod.AddContent(Definitions.ID = new SortID());
			mod.AddContent(Definitions.Name = new SortName());
			mod.AddContent(Definitions.Value = new SortValue());
			mod.AddContent(Definitions.Damage = new SortDamage());
			mod.AddContent(Definitions.Quantity = new SortQuantityAbsolute());
			mod.AddContent(Definitions.QuantityRatio = new SortQuantityRatio());
		}

		internal static void Unload() {
			options.Clear();
			Selected = 0;

			optionNames.Clear();

			foreach (var field in typeof(Definitions).GetFields().Where(f => f.FieldType == typeof(SortingOption)))
				field.SetValue(null, null);
		}

		internal static void InitializeOrder() {
			var positions = Options.ToDictionary(l => l, l => l.GetDefaultPosition());

			foreach (var (option, pos) in positions) {
				switch (pos) {
					case SortingOption.Between _:
						continue;
					case SortingOption.BeforeParent b:
						b.Parent.AddChildBefore(option);
						break;
					case SortingOption.AfterParent a:
						a.Parent.AddChildAfter(option);
						break;
					case SortingOption.Multiple m:
						int slot = 0;
						foreach (var (mulPos, cond) in m.Positions)
							positions.Add(new SortingOptionSlot(option, cond, slot++), mulPos);
						break;
					default:
						throw new ArgumentException($"SortingOption {option} has unknown Position type {pos}");
				}

				positions.Remove(option);
			}

			var sort = new TopoSort<SortingOption>(positions.Keys,
				l => new[] { ((SortingOption.Between)positions[l]).Option1 }.Where(l => l != null),
				l => new[] { ((SortingOption.Between)positions[l]).Option2 }.Where(l => l != null));

			order = sort.Sort().ToArray();
		}

		/// <summary>
		/// Gets all sorting options after recalculating visibility for the requested UI.
		/// </summary>
		/// <param name="craftingGUI">Whether options are being queried for the crafting UI instead of the storage UI.</param>
		/// <returns>The ordered sorting options.</returns>
		public static SortingOption[] GetOptions(bool craftingGUI) {
			foreach (var option in order)
				option.ResetVisibility(craftingGUI);

			return order;
		}

		/// <summary>
		/// Gets visible sorting options for the requested UI.
		/// </summary>
		/// <param name="craftingGUI">Whether options are being queried for the crafting UI instead of the storage UI.</param>
		/// <returns>The ordered sorting options whose current visibility is enabled.</returns>
		public static IEnumerable<SortingOption> GetVisibleOptions(bool craftingGUI) => GetOptions(craftingGUI).Where(o => o.Visible);
	}
}
