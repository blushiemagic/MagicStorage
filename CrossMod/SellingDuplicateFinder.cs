using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod {
	/// <summary>
	/// This class is used to detect whether an item is a duplicate of another item when selling items via Item Selling Mode in the Storage UI
	/// </summary>
	public abstract class SellingDuplicateFinder : ModType, ILocalizedModType {
		public int Type { get; private set; }

		public string LocalizationCategory => "SellingDuplicateFinders";

		/// <summary>
		/// The label for this <see cref="SellingDuplicateFinder"/> that is rendered in the Sell Duplicates menu
		/// </summary>
		public virtual LocalizedText Label => this.GetLocalization(nameof(Label), PrettyPrintName);

		protected sealed override void Register() {
			ModTypeLookup<SellingDuplicateFinder>.Register(this);

			Type = SellingDuplicateFinderLoader.Add(this);
		}

		public sealed override void SetupContent() {
			SetStaticDefaults();

			// Force the label to be generated if it's not present
			_ = Label;
		}

		/// <summary>
		/// Return <see langword="true"/> if <paramref name="check"/> is a duplicate of <paramref name="orig"/>.<br/>
		/// The type for the two items will always be the same.
		/// </summary>
		public abstract bool IsValidForDuplicateSelling(Item orig, Item check);

		/// <summary>
		/// Return which of the two items, <paramref name="item1"/> or <paramref name="item2"/>, should be preserved when selling duplicates.<br/>
		/// The type for the two items will always be the same.
		/// </summary>
		public abstract Item GetBetterItem(Item item1, Item item2);
	}

	internal static class SellingDuplicateFinderLoader {
		private class Loadable : ILoadable {
			void ILoadable.Load(Mod mod) { }

			void ILoadable.Unload() {
				_finders.Clear();
			}
		}

		private static readonly List<SellingDuplicateFinder> _finders = new();

		public static int Count => _finders.Count;

		internal static int Add(SellingDuplicateFinder finder) {
			_finders.Add(finder);
			return _finders.Count - 1;
		}

		internal static SellingDuplicateFinder Get(int index) => index < 0 || index >= _finders.Count ? null : _finders[index];
	}
}
