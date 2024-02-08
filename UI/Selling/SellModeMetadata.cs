using Ionic.Zlib;
using MagicStorage.Common;
using MagicStorage.Common.IO;
using MagicStorage.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.UI.Selling {
	public static class SellModeMetadata {
		private class SelectedItem {
			public readonly byte[] data;
			public int stack;

			public readonly int _fastGetItemType;
			public readonly int _fastGetItemValue;
			public readonly int _fastGetPrefix;

			public Item possiblyUnreliableItemInstance;

			public SelectedItem(Item item, int stack) {
				// Stack can't be saved in the byte array due to comparisons not knowing the stacks of the selected items
				using (ObjectSwitch.Create(ref item.stack, 1))
					data = Utility.ToByteArrayNoCompression(item);

				this.stack = stack;
				_fastGetItemType = item.type;
				_fastGetItemValue = item.value;
				_fastGetPrefix = item.prefix;
				possiblyUnreliableItemInstance = item;
			}

			public SelectedItem(byte[] data, int stack) {
				this.data = data;
				this.stack = stack;

				Item item = Utility.FromByteArrayNoCompression(data);
				possiblyUnreliableItemInstance = item;
				_fastGetItemType = item.type;
				_fastGetItemValue = item.value;
				_fastGetPrefix = item.prefix;
			}

			public bool Matches(Item item, ReadOnlySpan<byte> itemData) {
				if (_fastGetItemType != item.type)
					return false;

				if (_fastGetPrefix != item.prefix)
					return false;

				return itemData.SequenceEqual(data);
			}
		}

		public readonly struct Coins {
			public readonly int platinum;
			public readonly int gold;
			public readonly int silver;
			public readonly int copper;

			public Coins(long coppers) {
				platinum = (int)(coppers / 1000000);
				coppers %= 1000000;
				gold = (int)(coppers / 10000);
				coppers %= 10000;
				silver = (int)(coppers / 100);
				copper = (int)(coppers % 100);
			}

			public Coins(int platinum, int gold, int silver, int copper) {
				this.platinum = platinum;
				this.gold = gold;
				this.silver = silver;
				this.copper = copper;
			}

			public long TotalValue => platinum * 1000000 + gold * 10000 + silver * 100 + copper;

			public string ToChatTags() {
				StringBuilder sb = new();
				
				if (platinum > 0)
					sb.Append($"[i/s{platinum}:PlatinumCoin] ");
				if (gold > 0)
					sb.Append($"[i/s{gold}:GoldCoin] ");
				if (silver > 0)
					sb.Append($"[i/s{silver}:SilverCoin] ");
				if (copper > 0)
					sb.Append($"[i/s{copper}:CopperCoin] ");

				if (sb.Length == 0)
					return "Nothing";

				sb.Length -= 1;
				return sb.ToString();
			}
		}

		private static readonly List<SelectedItem> _items = new();

		public static int Count { get; private set; }

		public static bool IsValidForSelling(Item item) => !item.IsAir && item.type is not (ItemID.CopperCoin or ItemID.SilverCoin or ItemID.GoldCoin or ItemID.PlatinumCoin);
		
		public static bool? Add(Item item, int stack) {
			if (stack <= 0)
				return Remove(item) ? false : null;

			if (!IsValidForSelling(item))
				return null;

			if (Find(item, out var selected)) {
				Count -= selected.stack;
				selected.stack = stack;
				Count += stack;
				selected.possiblyUnreliableItemInstance = item;
				return false;
			}

			_items.Add(new SelectedItem(item, stack));
			Count += stack;
			return true;
		}

		public static void ChangeQuantity(Item item, int stack) {
			if (stack <= 0) {
				Remove(item);
				return;
			}

			if (!IsValidForSelling(item))
				return;

			if (Find(item, out var selected)) {
				Count -= selected.stack;
				selected.stack = stack;
				Count += stack;
			} else
				throw new InvalidOperationException("Item not found in cache");
		}

		public static bool Remove(Item item) {
			if (!IsValidForSelling(item))
				return false;

			ReadOnlySpan<byte> itemData;
			using (ObjectSwitch.Create(ref item.stack, 1))
				itemData = Utility.ToByteSpanNoCompression(item);

			for (int i = 0; i < _items.Count; i++) {
				var selectedItem = _items[i];
				if (selectedItem.Matches(item, itemData)) {
					Count -= selectedItem.stack;
					_items.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		public static void Clear() {
			_items.Clear();
			Count = 0;
		}

		public static bool HasItem(Item item, out int selectedQuantity) {
			if (!IsValidForSelling(item)) {
				selectedQuantity = -1;
				return false;
			}

			if (Find(item, out var selected)) {
				selectedQuantity = selected.stack;
				return true;
			}

			selectedQuantity = -1;
			return false;
		}

		private static bool Find(Item item, out SelectedItem selected) {
			ReadOnlySpan<byte> itemData;
			using (ObjectSwitch.Create(ref item.stack, 1))
				itemData = Utility.ToByteSpanNoCompression(item);

			foreach (var selectedItem in _items) {
				if (selectedItem.Matches(item, itemData)) {
					selected = selectedItem;
					return true;
				}
			}

			selected = null;
			return false;
		}

		internal static void HandleSell(TEStorageHeart heart, out int soldItemCount, out Coins sellValue, Player sellingPlayer = null) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (NetHelper.RequestDuplicateSelling(heart.Position))
					Clear();

				soldItemCount = 0;
				sellValue = default;
				return;
			}

			GetSellValuesWithSellAction(sellingPlayer ?? Main.LocalPlayer, out sellValue, out soldItemCount);

			ConditionalWeakTable<Item, byte[]> savedItemTagIO = new();
			foreach (var item in _items)
				heart.TryDeleteExactItem(item.data, itemStackOverride: item.stack, savedItemTagIO);

			if (sellValue.platinum > 0)
				heart.DepositItem(new Item(ItemID.PlatinumCoin, sellValue.platinum));
			if (sellValue.gold > 0)
				heart.DepositItem(new Item(ItemID.GoldCoin, sellValue.gold));
			if (sellValue.silver > 0)
				heart.DepositItem(new Item(ItemID.SilverCoin, sellValue.silver));
			if (sellValue.copper > 0)
				heart.DepositItem(new Item(ItemID.CopperCoin, sellValue.copper));

			Clear();
		}

		internal static void ClientReportSell(int soldItemCount, int totalItemCount, Coins sellValue) {
			if (Main.netMode == NetmodeID.Server)
				return;

			string text;
			if (soldItemCount > 0) {
				if (sellValue.TotalValue > 0)
					text = Language.GetTextValue("Mods.MagicStorage.StorageGUI.SellDuplicatesMenu.SoldItemsReport.GotCoins", soldItemCount, totalItemCount, sellValue.ToChatTags());
				else
					text = Language.GetTextValue("Mods.MagicStorage.StorageGUI.SellDuplicatesMenu.SoldItemsReport.NoCoins", soldItemCount, totalItemCount);
			} else
				text = Language.GetTextValue("Mods.MagicStorage.StorageGUI.SellDuplicatesMenu.SoldItemsReport.NoSell");

			Main.NewText(text);
		}

		internal static bool NetSend(BinaryWriter writer, int consumedPacketSpace = 0) {
			int maximumCapacity = 65536 - consumedPacketSpace - 4;
			MemoryStream ms = new MemoryStream(maximumCapacity);
			using (BinaryWriter compressedWriter = new BinaryWriter(ms)) {
				// Write the items
				compressedWriter.Write7BitEncodedInt(_items.Count);
				foreach (var item in _items) {
					compressedWriter.Write7BitEncodedInt(item.data.Length);
					compressedWriter.Write(item.data);
					compressedWriter.Write(item.stack);
				}
			}

			byte[] uncompressedData = ms.ToArray();
			byte[] compressedData = NetCompression.Compress(uncompressedData, CompressionLevel.BestCompression);

			// Do not write to the actual writer if the compressed data is too large
			if (compressedData.Length >= maximumCapacity)
				return false;

			writer.Write7BitEncodedInt(compressedData.Length);
			writer.Write(compressedData);

			return true;
		}

		internal static void NetReceive(BinaryReader reader) {
			Clear();

			int compressedLength = reader.Read7BitEncodedInt();
			byte[] compressedData = reader.ReadBytes(compressedLength);
			byte[] uncompressedData = NetCompression.Decompress(compressedData, CompressionLevel.BestCompression);

			using MemoryStream ms = new MemoryStream(uncompressedData);
			using BinaryReader decompressedReader = new BinaryReader(ms);

			// Read the items
			int itemCount = decompressedReader.Read7BitEncodedInt();
			for (int i = 0; i < itemCount; i++) {
				int itemDataLength = decompressedReader.Read7BitEncodedInt();
				byte[] itemData = decompressedReader.ReadBytes(itemDataLength);
				int stack = decompressedReader.ReadInt32();

				_items.Add(new SelectedItem(itemData, stack));
				Count += stack;
			}
		}

		private static readonly NPC _dummyNPCForShop = new();

		public static void GetSellValues(Player sellingPlayer, out Coins coins) {
			ClampedLongArithmetic sum = 0;

			foreach (var item in _items) {
				if (!PlayerLoader.CanSellItem(sellingPlayer, _dummyNPCForShop, Array.Empty<Item>(), item.possiblyUnreliableItemInstance))
					continue;

				sum += (long)item._fastGetItemValue * item.stack;
			}

			coins = new Coins(sum);
		}

		private static void GetSellValuesWithSellAction(Player sellingPlayer, out Coins coins, out int soldItemCount) {
			ClampedLongArithmetic sum = 0;
			soldItemCount = 0;

			foreach (var item in _items) {
				if (!PlayerLoader.CanSellItem(sellingPlayer, _dummyNPCForShop, Array.Empty<Item>(), item.possiblyUnreliableItemInstance))
					continue;

				sum += (long)item._fastGetItemValue * item.stack;
				soldItemCount += item.stack;

				PlayerLoader.PostSellItem(sellingPlayer, _dummyNPCForShop, Array.Empty<Item>(), item.possiblyUnreliableItemInstance);
			}

			coins = new Coins(sum);
		}
	}
}
