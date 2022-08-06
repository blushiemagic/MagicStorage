using MagicStorage.UI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.CrossMod {
	public abstract partial class SortingOption : ModTexturedType {
		public int Type { get; private set; }

		public ModTranslation Tooltip { get; private set; }

		public Asset<Texture2D> TextureAsset => ModContent.Request<Texture2D>(Texture);

		public abstract IComparer<Item> Sorter { get; }

		public virtual Action OnSelected { get; }

		protected sealed override void Register() {
			ModTypeLookup<SortingOption>.Register(this);

			Type = SortingOptionLoader.Add(this);

			Tooltip = LocalizationLoader.GetOrCreateTranslation(Mod, $"SortingOption.{Name}");
		}

		public sealed override void SetupContent() {
			AutoStaticDefaults();
			SetStaticDefaults();
		}

		/// <summary>
		/// Automatically sets certain static defaults. Override this if you do not want the properties to be set for you.
		/// </summary>
		public virtual void AutoStaticDefaults() {
			if (Tooltip.IsDefault())
				Tooltip.SetDefault(Regex.Replace(Name, "([A-Z])", " $1").Trim());
		}

		public bool Visible { get; private set; } = true;

		private readonly List<SortingOption> childrenBefore = new();
		public IReadOnlyList<SortingOption> ChildrenBefore => childrenBefore;

		private readonly List<SortingOption> childrenAfter = new();
		public IReadOnlyList<SortingOption> ChildrenAfter => childrenAfter;

		public void Hide() => Visible = false;

		internal void AddChildBefore(SortingOption child) => childrenBefore.Add(child);
		internal void AddChildAfter(SortingOption child) => childrenAfter.Add(child);

		internal void ClearChildren() {
			childrenBefore.Clear();
			childrenAfter.Clear();
		}

		/// <summary> Returns the layer's default visibility. This is usually called as a layer is queued for drawing, but modders can call it too for information. </summary>
		/// <returns> Whether or not this layer will be visible by default. Modders can hide layers later, if needed.</returns>
		public virtual bool GetDefaultVisibility(bool craftinGUI) => true;

		/// <summary>
		/// Returns the layer's default position in regards to other options.
		/// Make use of e.g <see cref="BeforeParent"/>/<see cref="AfterParent"/>, and provide an option (usually a vanilla one from <see cref="SortingOptionLoader"/>).
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
	public class SortingOptionSlot : SortingOption {
		public SortingOption Option { get; }
		public Multiple.Condition Condition { get; }

		public override IComparer<Item> Sorter => Option.Sorter;

		private readonly int _slot;

		public override string Name => $"{Option.Name}_slot{_slot}";

		internal SortingOptionSlot(SortingOption option, Multiple.Condition cond, int slot) {
			Option = option;
			Condition = cond;
			_slot = slot;
			AddChildAfter(Option);
		}

		public override Position GetDefaultPosition() => throw new NotImplementedException();

		public override bool GetDefaultVisibility(bool craftingGUI) => Condition(craftingGUI);
	}

	internal class SortingOptionElement : BaseOptionElement {
		public readonly SortingOption option;

		public SortingOptionElement(SortingOption option) {
			this.option = option;
		}

		protected override string GetHoverText() => option.Tooltip.GetTranslation(Language.ActiveCulture);

		protected override Asset<Texture2D> GetIcon() => option.TextureAsset;

		protected override bool IsSelected() => option.Type == SortingOptionLoader.Selected;

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);

			SortingOptionLoader.Selected = option.Type;

			option.OnSelected?.Invoke();
		}
	}

	public static class SortingOptionLoader {
		public static class Definitions {
			public static SortingOption Default { get; internal set; }
			public static SortingOption ID { get; internal set; }
			public static SortingOption Name { get; internal set; }
			public static SortingOption Value { get; internal set; }
			public static SortingOption Quantity { get; internal set; }
			public static SortingOption QuantityRatio { get; internal set; }
			public static SortingOption Damage { get; internal set; }
		}

		private static readonly List<SortingOption> options = new();

		public static IReadOnlyList<SortingOption> Options => options.AsReadOnly();

		private static SortingOption[] order;

		public static IReadOnlyList<SortingOption> Order => order;

		public static int Selected { get; internal set; }

		public static int Count => options.Count;

		internal static int Add(SortingOption option) {
			int count = Count;

			options.Add(option);
			order = null;

			return count;
		}

		public static SortingOption Get(int index) => index < 0 || index >= options.Count ? null : options[index];

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
						throw new ArgumentException($"PlayerDrawLayer {option} has unknown Position type {pos}");
				}

				positions.Remove(option);
			}

			var sort = new TopoSort<SortingOption>(positions.Keys,
				l => new[] { ((SortingOption.Between)positions[l]).Option1 }.Where(l => l != null),
				l => new[] { ((SortingOption.Between)positions[l]).Option2 }.Where(l => l != null));

			order = sort.Sort().ToArray();
		}

		public static SortingOption[] GetOptions(bool craftingGUI) {
			foreach (var option in order)
				option.ResetVisibility(craftingGUI);

			return order;
		}
	}
}
