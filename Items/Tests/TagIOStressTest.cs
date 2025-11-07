using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Items.Tests {
	[Autoload(true)]
	internal class TagIOStressTest : ModItem {
		public override string Texture => "Terraria/Images/Item_" + ItemID.IronPickaxe;

		public override void SaveData(TagCompound tag) {
			tag["serialized"] = new SerializedType<int>(value: 10);
			tag["list"] = new List<SerializedType<short>>() {
				new(1),
				new(100),
				new(10000)
			};
		}

		public override void LoadData(TagCompound tag) {
			var serialized = tag.Get<SerializedType<int>>("serialized");
			var list = tag.GetList<SerializedType<int>>("list");
		}

		private class SerializedType<T>(T value) : TagSerializable {
			private readonly T _value = value;

			public static readonly Func<TagCompound, SerializedType<T>> DESERIALIZER = tag => {
				T value = tag.Get<T>("value");
				return new(value);
			};

			TagCompound TagSerializable.SerializeData() {
				return new() {
					["value"] = _value
				};
			}
		}
	}
}
