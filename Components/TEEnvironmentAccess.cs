using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components {
	public class TEEnvironmentAccess : TEStoragePoint {
		public IEnumerable<EnvironmentModule> Modules => EnvironmentModuleLoader.modules.Where((m, i) => Enabled(i));

		private BitArray enabled;

		public int Count => enabled?.GetCardinality() ?? 0;

		private void EnsureInitialized() => enabled ??= new(EnvironmentModuleLoader.Count, true);

		public bool Enabled(int index) {
			if (EnvironmentModuleLoader.Count == 0 || index >= EnvironmentModuleLoader.Count)
				return false;

			EnsureInitialized();

			return enabled[index];
		}

		internal void SetEnabled(EnvironmentModule module, bool enabled) {
			int index = module.Type;

			if (EnvironmentModuleLoader.Count == 0 || index >= EnvironmentModuleLoader.Count)
				return;

			EnsureInitialized();

			this.enabled[index] = enabled;
		}

		public bool Enabled(EnvironmentModule module) => Enabled(module.Type);

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<EnvironmentAccess>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

		public override void SaveData(TagCompound tag) {
			base.SaveData(tag);

			tag["enabled"] = Modules.Select(m => new TagCompound() { ["mod"] = m.Mod.Name, ["name"] = m.Name }).ToList();
		}

		public override void LoadData(TagCompound tag) {
			base.LoadData(tag);

			if (tag.TryGet("enabled", out List<TagCompound> list)) {
				enabled = new(EnvironmentModuleLoader.Count);

				foreach (var module in list.Select(t => t.TryGet("mod", out string mod) && t.TryGet("name", out string name) && ModContent.TryFind(mod, name, out EnvironmentModule m) ? m : null).Where(m => m is not null)) {
					enabled[module.Type] = true;
				}
			} else {
				//Default to all enabled
				enabled = new(EnvironmentModuleLoader.Count, true);
			}
		}

		public override void NetSend(BinaryWriter writer) {
			base.NetSend(writer);

			EnsureInitialized();

			int length = (enabled.Length - 1) / 8 + 1;
			writer.Write((short)length);
			byte[] array = new byte[length];
			enabled.CopyTo(array, 0);
			writer.Write(array);
		}

		public override void NetReceive(BinaryReader reader) {
			base.NetReceive(reader);

			short length = reader.ReadInt16();
			enabled = new(reader.ReadBytes(length));
			enabled.Length = EnvironmentModuleLoader.Count;
		}
	}
}
