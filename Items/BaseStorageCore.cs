using Ionic.Zlib;
using MagicStorage.Common.IO;
using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.CrossMod.Storage;
using SerousCommonLib.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Items {
	public abstract class BaseStorageCore : ModItem, IValidateAtPostSetupContent {
		[CloneByReference]
		private byte[] _unitData;
		private int _hash;
		private int _itemCount;

		public int DataHash => _hash;

		public abstract StorageUnitTier Tier { get; }

		public override LocalizedText Tooltip => Language.GetText("Mods.MagicStorage.Items.StorageCore.CommonTooltip");

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 0;
		}

		void IValidateAtPostSetupContent.ValidateType() {
			if (Tier.CoreItemType != Type)
				throw new Exception($"Storage Core item \"{FullName}\" does not match the item ID assigned to its Storage Unit tier \"{Tier.FullName}\"");
		}

		public override void SetDefaults() {
			Item.width = 16;
			Item.height = 16;
			Item.maxStack = 1;
			Item.value = 0;

			int unitItem = Tier.StorageUnitItemType;
			if (ContentSamples.ItemsByType is null || !ContentSamples.ItemsByType.TryGetValue(unitItem, out Item sample))
				sample = new Item(unitItem);

			Item.rare = sample.rare;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			if (_unitData is null) {
				TooltipHelper.FindAndRemoveLine(tooltips, "<CAPACITY>");
				TooltipHelper.FindAndRemoveLine(tooltips, "<HASH>");
				TooltipHelper.FindAndRemoveLine(tooltips, "<BYTES>");
			} else {
				int capacity = Tier.Capacity;

				string countColorHex;
				if (_itemCount == 0)
					countColorHex = "18c242";  // Green
				else if (_itemCount < capacity)
					countColorHex = "e4d833";  // Yellow
				else
					countColorHex = "e43233";  // Red

				string dataColorHex;
				if (_unitData.Length < 400)
					dataColorHex = "18c242";  // Green
				else if (_unitData.Length < 1200)
					dataColorHex = "e4d833";  // Yellow
				else
					dataColorHex = "e43233";  // Red

				TooltipHelper.FindAndModify(tooltips, "<CAPACITY>", Mod.GetLocalization("Items.StorageCore.ItemsStored").Format(countColorHex, _itemCount, capacity));
				TooltipHelper.FindAndModify(tooltips, "<HASH>", Mod.GetLocalization("Items.StorageCore.Hash").Format(_hash));
				TooltipHelper.FindAndModify(tooltips, "<BYTES>", Mod.GetLocalization("Items.StorageCore.BytesUsed").Format(dataColorHex, _unitData.Length));
			}
		}

		public override void SaveData(TagCompound tag) {
			tag["data"] = _unitData;
			tag["count"] = _itemCount;
		}

		public override void LoadData(TagCompound tag) {
			_unitData = tag.GetByteArray("data");
			_itemCount = tag.GetInt("count");
			_hash = Utility.ComputeDataHash(_unitData);
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(_hash);
			writer.Write((ushort)_itemCount);
			writer.Write7BitEncodedInt(_unitData?.Length ?? 0);
			if (_unitData is not null)
				writer.Write(_unitData);
		}

		public override void NetReceive(BinaryReader reader) {
			_hash = reader.ReadInt32();
			_itemCount = reader.ReadUInt16();
			int length = reader.Read7BitEncodedInt();
			if (length > 0)
				_unitData = reader.ReadBytes(length);
			else
				_unitData = null;
		}

		public void SetDataFrom(TEStorageUnit unit) {
			StorageUnitTier unitTier = unit.GetCurrentTier()
				?? throw new InvalidOperationException("No Storage Unit tier was found for the provided Storage Unit");

			if (unitTier.Type != Tier.Type)
				throw new InvalidOperationException($"Unit tier ({unitTier.FullName}) does not match core tier ({Tier.FullName})");

			using MemoryStream ms = new(65536);
			using (BinaryWriter writer = new(ms)) {
				// Write the unit's contents to the stream
			//	using (FlagSwitch.ToggleTrue(ref ValueWriter.LogWrites)) {
				//	MagicStorageMod.Instance.Logger.Info("==============================");
				//	MagicStorageMod.Instance.Logger.Info($"Writing {unit.items.Count} items to Storage Core");
					NetCompression.SendItems(unit.items, writer, true, true, NetCompression.GetBitSize(Tier.Capacity));
				//	MagicStorageMod.Instance.Logger.Info($"SERIALIZED BYTES: {string.Join(' ', ms.ToArray().Select(static b => $"{b:X02}"))}");
				//	MagicStorageMod.Instance.Logger.Info("==============================");
			//	}
			}

			byte[] data = NetCompression.Compress(ms.ToArray(), CompressionLevel.BestCompression);

			SetUnitData(data, unit.items.Count);
		}

		protected void SetUnitData(byte[] data, int itemCount) {
			_unitData = data;
			_itemCount = itemCount;

			// Calculate a hash of the data
			_hash = Utility.ComputeDataHash(data);
		}

		public IEnumerable<Item> RetrieveItems() {
			if (_unitData is not { Length: >0 })
				return Array.Empty<Item>();

			using MemoryStream ms = new(NetCompression.Decompress(_unitData, CompressionLevel.BestCompression));
			using BinaryReader reader = new(ms);

		//	using (FlagSwitch.ToggleTrue(ref ValueReader.LogReads)) {
			//	MagicStorageMod.Instance.Logger.Info("==============================");
			//	MagicStorageMod.Instance.Logger.Info($"Retrieving {_itemCount} items from Storage Core");
			//	MagicStorageMod.Instance.Logger.Info($"SERIALIZED BYTES: {string.Join(' ', ms.ToArray().Select(static b => $"{b:X02}"))}");
				var items = NetCompression.ReceiveItems(reader, true, true, NetCompression.GetBitSize(Tier.Capacity));
			//	MagicStorageMod.Instance.Logger.Info("==============================");
				return items;
		//	}
		}
	}
}
