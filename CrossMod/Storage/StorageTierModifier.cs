using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod.Storage {
	/// <summary>
	/// The base class for modifiers that can change the loaded <see cref="StorageUnitTier"/> instances
	/// </summary>
	public abstract class StorageTierModifier : ModType {
		/// <summary>
		/// The ID of this <see cref="StorageTierModifier"/>
		/// </summary>
		public int Type { get; private set; }

		protected sealed override void Register() {
			ModTypeLookup<StorageTierModifier>.Register(this);
			Type = StorageTierModifierLoader.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

		/// <summary>
		/// Modify which tiers can be upgraded to/from the given <paramref name="tier"/>.<br/>
		/// To add connections between tiers, use <see cref="StorageUnitTier.SetUpgradeableTo(StorageUnitTier)"/> and <see cref="StorageUnitTier.SetUpgradeableFrom(StorageUnitTier)"/><br/>
		/// To remove connections, use <see cref="StorageUnitTier.RemoveConnections(StorageUnitTier)"/> and <see cref="StorageUnitTier.RemoveAllConnections"/>
		/// </summary>
		public virtual void ModifyUpgradeConnections(StorageUnitTier tier) { }
	}

	public static class StorageTierModifierLoader {
		private class Loadable : ILoadable {
			void ILoadable.Load(Mod mod) { }
			void ILoadable.Unload() {
				_modifiers.Clear();
			}
		}

		private static readonly List<StorageTierModifier> _modifiers = [];

		public static int Count => _modifiers.Count;

		internal static int Add(StorageTierModifier modifier) {
			_modifiers.Add(modifier);
			return _modifiers.Count - 1;
		}

		public static StorageTierModifier Get(int type) => type < 0 || type >= _modifiers.Count ? null : _modifiers[type];

		internal static void PostSetupContent() {
			List<StorageUnitTier> tiers = ModContent.GetContent<StorageUnitTier>().ToList();

			foreach (var modifier in _modifiers) {
				foreach (var tier in tiers)
					modifier.ModifyUpgradeConnections(tier);
			}
		}
	}
}
