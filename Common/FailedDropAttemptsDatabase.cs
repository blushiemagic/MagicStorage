using System.Collections.Generic;
using System.IO;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common {
	/// <summary>
	/// Stores failed drop attempt counts by item type for pity-drop calculations.
	/// </summary>
	public class FailedDropAttemptsDatabase {
		private readonly Dictionary<int, int> _failedAttempts = [];
		private readonly List<TagCompound> _unloaded = [];

		/// <summary>
		/// Clears all loaded and unloaded attempt data.
		/// </summary>
		public void Clear() {
			_failedAttempts.Clear();
			_unloaded.Clear();
		}

		/// <summary>
		/// Saves failed drop attempt counts to a tag.
		/// </summary>
		public void Save(TagCompound tag) {
			List<TagCompound> list = [];
			
			foreach (var (type, attempts) in _failedAttempts) {
				if (attempts <= 0)
					continue;

				TagCompound entry = new();

				if (type < ItemID.Count) {
					entry["mod"] = "Terraria";
					entry["id"] = type;
				} else {
					ModItem modItem = ModContent.GetModItem(type);
					entry["mod"] = modItem.Mod.Name;
					entry["name"] = modItem.Name;
				}

				entry["attempts"] = attempts;

				list.Add(entry);
			}

			list.AddRange(_unloaded);

			if (list.Count > 0)
				tag["attemptList"] = list;
		}

		/// <summary>
		/// Loads failed drop attempt counts from a tag, preserving entries for unloaded modded items.
		/// </summary>
		public void Load(TagCompound tag) {
			_failedAttempts.Clear();
			_unloaded.Clear();

			foreach (var entry in tag.GetList<TagCompound>("attemptList")) {
				string mod = entry.GetString("mod");

				int type;
				if (mod == "Terraria") {
					// Vanilla item
					type = entry.GetInt("id");
				} else if (ModLoader.TryGetMod(mod, out Mod source) && source.TryFind<ModItem>(entry.GetString("name"), out ModItem modItem)) {
					// Modded item
					type = modItem.Type;
				} else {
					// Unknown item
					_unloaded.Add(entry);
					continue;
				}

				_failedAttempts[type] = entry.GetInt("attempts");
			}
		}

		/// <summary>
		/// Writes loaded failed drop attempt counts for network sync.
		/// </summary>
		public void NetSend(BinaryWriter writer) {
			writer.Write(_failedAttempts.Count);
			foreach (var (type, attempts) in _failedAttempts) {
				writer.Write(type);
				writer.Write(attempts);
			}
		}

		/// <summary>
		/// Reads loaded failed drop attempt counts from network sync data.
		/// </summary>
		public void NetReceive(BinaryReader reader) {
			_failedAttempts.Clear();

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				int type = reader.ReadInt32();
				int attempts = reader.ReadInt32();

				_failedAttempts[type] = attempts;
			}
		}

		/// <summary>
		/// Gets the number of consecutive failed drop attempts for an item type.
		/// </summary>
		public int GetFailedAttempts(int type) => _failedAttempts.TryGetValue(type, out int attempts) ? attempts : 0;

		/// <summary>
		/// Records a failed drop attempt for an item type.
		/// </summary>
		public void OnFailedDrop(int type) {
			if (_failedAttempts.TryGetValue(type, out int attempts))
				_failedAttempts[type] = attempts + 1;
			else
				_failedAttempts[type] = 1;
		}

		/// <summary>
		/// Resets failed drop attempts for an item type after a successful drop.
		/// </summary>
		public void OnSuccessfulDrop(int type) => _failedAttempts[type] = 0;

		/// <summary>
		/// Compares loaded attempt counts with another database.
		/// </summary>
		public bool IsEquivalentTo(FailedDropAttemptsDatabase other) {
			if (_failedAttempts.Count != other._failedAttempts.Count)
				return false;

			foreach (var (type, attempts) in _failedAttempts) {
				if (!other._failedAttempts.TryGetValue(type, out int otherAttempts) || attempts != otherAttempts)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Copies loaded network-synchronized attempt counts to another database.
		/// </summary>
		public void NetCopyTo(FailedDropAttemptsDatabase other) {
			other._failedAttempts.Clear();
			foreach (var (type, attempts) in _failedAttempts)
				other._failedAttempts[type] = attempts;
		}
	}
}
