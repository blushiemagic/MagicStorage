using Ionic.Zlib;
using MagicStorage.Common;
using MagicStorage.Common.IO;
using MagicStorage.Components;
using MagicStorage.Items.ErrorDisplay;
using MagicStorage.NPCs;
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
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;

namespace MagicStorage.UI.Selling {
	public static class SellModeMetadata {
		private class SelectedItems {
			public int totalStack;

			public int _fastGetItemType = -42069;
			public int _fastGetItemValue;
			public int _fastGetPrefix;
			public byte[] _fastGetData;

			private Item _iconicItem;

			private readonly List<Item> _items = [];
			public IReadOnlyList<Item> Items => _items.AsReadOnly();

			public static LengthCompressor<uint> _countTiers;

			static SelectedItems() {
				var tier0 = EncodingTier.CreateZero<uint>  (prefix: 0b_00, 2, size: 16);
				var tier1 = tier0.CreateSuccessive         (prefix: 0b_01, 2, size: 64);
				var tier2 = tier1.CreateSuccessive         (prefix: 0b_10, 2, size: 256);
				var tier3 = tier2.CreateSuccessive         (prefix: 0b_11, 2, size: 16384);
				var tier4 = tier3.CreateSuccessive         (prefix: 0b011, 3, size: 131072);
				var tier5 = tier4.CreateSuccessiveUnbounded(prefix: 0b111, 3);

				_countTiers = new LengthCompressor<uint>(tier0, tier1, tier2, tier3, tier4, tier5);
			}

			public void Add(Item item, ReadOnlySpan<byte> data) {
				if (_fastGetItemType < 0) {
					_fastGetItemType = item.type;
					_fastGetItemValue = item.value;
					_fastGetPrefix = item.prefix;
					_fastGetData = data.ToArray();

					_iconicItem = item;
				}

				_items.Add(item);
				totalStack += item.stack;
			}

			public bool Matches(Item item, ReadOnlySpan<byte> itemData) {
				if (_fastGetItemType != item.type)
					return false;

				if (_fastGetPrefix != item.prefix)
					return false;

				return itemData.SequenceEqual(_fastGetData);
			}

			public void Write(ValueWriter writer) {
				if (_iconicItem is null || totalStack < 0) {
					// Invalid state, cannot write
					_countTiers.WriteTo(writer, 0);
					return;
				}

				_countTiers.WriteTo(writer, (uint)totalStack);

			//	using var _ = FlagSwitch.Create(ref ValueWriter.LogWrites, true);

				SaveCompression.SaveItem(_iconicItem, writer, writeStack: false, writeFavorite: true);

				_countTiers.WriteTo(writer, (uint)_items.Count);

				var stackCompressor = new StackCompressor(ContentSamples.ItemsByType[_fastGetItemType].maxStack);

				foreach (Item item in _items)
					stackCompressor.WriteTo(writer, item.stack);
			}

			public void Read(ValueReader reader) {
				_fastGetItemType = -42069;
				_fastGetItemValue = 0;
				_fastGetPrefix = 0;
				_fastGetData = null;
				_iconicItem = null;
				_items.Clear();

				totalStack = (int)_countTiers.ReadFrom(reader);

				if (totalStack <= 0) {
					// Nothing to read
					return;
				}

			//	using var _ = FlagSwitch.Create(ref ValueReader.LogReads, true);

				var item = SaveCompression.LoadItem(reader, readStack: false, readFavorite: true);

				// Use Add() to set the fast-get fields
				Add(item, Utility.ToByteSpanNoCompression(item));

				_items.Clear();

				int itemCount = (int)_countTiers.ReadFrom(reader);

				var stackDecompressor = new StackCompressor(ContentSamples.ItemsByType[_fastGetItemType].maxStack);

				for (int i = 0; i < itemCount; i++) {
					int stack = stackDecompressor.ReadFrom(reader);

					var clone = item.Clone();
					clone.stack = stack;

					_items.Add(clone);
				}
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

		private static readonly List<SelectedItems> _items = new();

		public static int Count { get; private set; }

		public static bool IsValidForSelling(Item item) => !item.IsAir && item.type is not (ItemID.CopperCoin or ItemID.SilverCoin or ItemID.GoldCoin or ItemID.PlatinumCoin) && item.ModItem is not (UnloadedItem or BaseErrorDummyItem);
		
		public static bool? Add(Item item, int stack) {
			if (stack <= 0)
				return Remove(item) ? false : null;

			if (!IsValidForSelling(item))
				return null;

			ReadOnlySpan<byte> data;
			using (ObjectSwitch.Create(ref item.stack, 1))
				data = Utility.ToByteSpanNoCompression(item);

			if (!Find(item, data, out var selected)) {
				selected = new SelectedItems();
				_items.Add(selected);
			}

			var clone = item.Clone();
			clone.stack = stack;
			
			selected.Add(clone, data);
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
				int difference = stack - selected.totalStack;
				if (difference != 0) {
					selected.totalStack = stack;
					Count += difference;
				}
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
					Count -= selectedItem.totalStack;
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
				selectedQuantity = selected.totalStack;
				return true;
			}

			selectedQuantity = -1;
			return false;
		}

		private static bool Find(Item item, out SelectedItems selected) {
			ReadOnlySpan<byte> data;
			using (ObjectSwitch.Create(ref item.stack, 1))
				data = Utility.ToByteSpanNoCompression(item);
			return Find(item, data, out selected);
		}

		private static bool Find(Item item, ReadOnlySpan<byte> data, out SelectedItems selected) {
			foreach (var selectedItem in _items) {
				if (selectedItem.Matches(item, data)) {
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
			
			ConditionalWeakTable<Item, byte[]> savedItemTagIO = new();
			TEStorageHeart h = heart;
			GetSellValues(sellingPlayer ?? Main.LocalPlayer, out sellValue, out soldItemCount, true, (SelectedItems item, ref int sold) => {
				int toSell = sold;

				if (h.TryDeleteExactItem(item._fastGetData, out _, ref toSell, savedItemTagIO)) {
					// Some or all of the items could be deleted
					sold -= toSell;
					return true;
				}

				// No items could be deleted
				sold = 0;
				return false;
			});

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
				var bitWriter = new ValueWriter(compressedWriter);

				SelectedItems._countTiers.WriteTo(bitWriter, (uint)_items.Count);

				foreach (var item in _items)
					item.Write(bitWriter);

				bitWriter.Flush();
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

			var bitReader = new ValueReader(decompressedReader);

			// Read the items
			int itemCount = (int)SelectedItems._countTiers.ReadFrom(bitReader);

			for (int i = 0; i < itemCount; i++) {
				var item = new SelectedItems();
				item.Read(bitReader);

				if (item.totalStack > 0) {
					_items.Add(item);
					Count += item.totalStack;
				}
			}
		}

		private static readonly NPC _dummyNPCForShop = new();

		public static void GetSellValues(Player sellingPlayer, out Coins coins) => GetSellValues(sellingPlayer, out coins, out _, false, null);
		
		public static void GetSellValues(Player sellingPlayer, out Coins coins, out int soldItemCount, bool runSellEvents) => GetSellValues(sellingPlayer, out coins, out soldItemCount, runSellEvents, null);

		private delegate bool GetSellValueDelegate(SelectedItems selectedItems, ref int soldItemCount);

		private static void GetSellValues(Player sellingPlayer, out Coins coins, out int soldItemCount, bool runSellEvents, GetSellValueDelegate checkSellingFunc) {
			ClampedLongArithmetic sum = 0;
			soldItemCount = 0;

			foreach (var selectedItems in _items) {
				bool allowed = true;
				
				foreach (var item in selectedItems.Items) {
					if (!PlayerLoader.CanSellItem(sellingPlayer, _dummyNPCForShop, [], item)) {
						allowed = false;
						break;
					}
				}

				int sold = selectedItems.totalStack;
				if (checkSellingFunc is not null && !checkSellingFunc(selectedItems, ref sold))
					allowed = false;

				if (!allowed)
					continue;

				// NOTE: sell value = buy value / 5
				sum += (long)(selectedItems._fastGetItemValue / 5) * sold;
				soldItemCount += sold;

				if (runSellEvents) {
					foreach (var item in selectedItems.Items)
						PlayerLoader.PostSellItem(sellingPlayer, _dummyNPCForShop, [], item);
				}
			}

			// ShoppingSettings.PriceAdjustment is meant to be a multiplier to increase costs for worse happiness
			// Hence, we need to divide instead to make items worth less when happiness is worse
			sum = (long)(sum / GetAutomatonPriceAdjustment(sellingPlayer));

			coins = new Coins(sum);
		}

		private static double GetAutomatonPriceAdjustment(Player sellingPlayer) {
			double adjustment = 1.0;

			if (MagicStorageServerConfig.AutomatonHappinessAffectsSellPrices) {
				foreach (NPC npc in Main.ActiveNPCs) {
					if (npc.ModNPC is not Golem)
						continue;

					var settings = Main.ShopHelper.GetShoppingSettings(sellingPlayer, npc);
					adjustment *= settings.PriceAdjustment;
				}
			}

			return adjustment;
		}
	}
}
