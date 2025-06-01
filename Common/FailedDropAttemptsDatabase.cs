using System.Collections.Generic;
using System.IO;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common {
	public class FailedDropAttemptsDatabase {
		private readonly Dictionary<int, int> _failedAttempts = [];
		private readonly List<TagCompound> _unloaded = [];

		public void Clear() {
			_failedAttempts.Clear();
			_unloaded.Clear();
		}

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

		public void NetSend(BinaryWriter writer) {
			writer.Write(_failedAttempts.Count);
			foreach (var (type, attempts) in _failedAttempts) {
				writer.Write(type);
				writer.Write(attempts);
			}
		}

		public void NetReceive(BinaryReader reader) {
			_failedAttempts.Clear();

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				int type = reader.ReadInt32();
				int attempts = reader.ReadInt32();

				_failedAttempts[type] = attempts;
			}
		}

		public int GetFailedAttempts(int type) => _failedAttempts.TryGetValue(type, out int attempts) ? attempts : 0;

		public void OnFailedDrop(int type) {
			if (_failedAttempts.TryGetValue(type, out int attempts))
				_failedAttempts[type] = attempts + 1;
			else
				_failedAttempts[type] = 1;
		}

		public void OnSuccessfulDrop(int type) => _failedAttempts[type] = 0;

		public bool IsEquivalentTo(FailedDropAttemptsDatabase other) {
			if (_failedAttempts.Count != other._failedAttempts.Count)
				return false;

			foreach (var (type, attempts) in _failedAttempts) {
				if (!other._failedAttempts.TryGetValue(type, out int otherAttempts) || attempts != otherAttempts)
					return false;
			}

			return true;
		}

		public void NetCopyTo(FailedDropAttemptsDatabase other) {
			other._failedAttempts.Clear();
			foreach (var (type, attempts) in _failedAttempts)
				other._failedAttempts[type] = attempts;
		}
	}
}
