using MagicStorage.Common.Systems;
using MagicStorage.Components;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace MagicStorage
{
	public class StoragePlayer : ModPlayer
	{
		public static StoragePlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<StoragePlayer>();

		public bool remoteAccess, remoteCrafting;
		private Point16 storageAccess = Point16.NegativeOne;
		public float portableAccessRangePlayerToPylons;
		public int portableAccessRangePylonsToStorage;

		public int timeSinceOpen = 1;

		internal bool pendingRemoteOpen;

		internal int wirelessLatency = -1;
		internal const int MaxLatency = 10;

		protected override bool CloneNewInstances => false;

		public ItemTypeOrderedSet HiddenRecipes { get; } = new("HiddenItems");

		public ItemTypeOrderedSet FavoritedRecipes { get; } = new("FavoritedRecipes");

		public override void SaveData(TagCompound tag)
		{
			HiddenRecipes.Save(tag);
			FavoritedRecipes.Save(tag);
		}

		public override void LoadData(TagCompound tag)
		{
			HiddenRecipes.Load(tag);
			FavoritedRecipes.Load(tag);
		}

		public override void OnEnterWorld(Player player) {
			if (MagicStorageMod.UsingPrivateBeta) {
				Main.NewTextMultiline("Thank you for helping test a private beta for Magic Storage!\n" +
					"Do note that using this private beta build will cause a ton of text to be printed to the chat (when the config is enabled) and to your log files.",
					c: Color.LightBlue);
			}
		}

		public override void UpdateDead()
		{
			if (Player.whoAmI == Main.myPlayer)
				CloseStorage();
		}

		public override void ResetEffects()
		{
			if (wirelessLatency >= 0)
				wirelessLatency--;

			if (Player.whoAmI != Main.myPlayer)
				return;

			if (timeSinceOpen < 1 && !Main.autoPause && storageAccess.X >= 0 && storageAccess.Y >= 0)
			{
				Player.SetTalkNPC(-1);
				Main.playerInventory = true;
				timeSinceOpen++;
			}

			if (storageAccess.X >= 0 && storageAccess.Y >= 0 && (Player.chest != -1 || !Main.playerInventory || Player.sign > -1 || Player.talkNPC > -1))
			{
				CloseStorage();
				Recipe.FindRecipes();
			}
			else if (storageAccess.X >= 0 && storageAccess.Y >= 0)
			{
				int playerX = (int)(Player.Center.X / 16f);
				int playerY = (int)(Player.Center.Y / 16f);
				var modTile = TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].TileType);

				if (!remoteAccess &&
					(playerX < storageAccess.X - Player.lastTileRangeX ||
					 playerX > storageAccess.X + Player.lastTileRangeX + 1 ||
					 playerY < storageAccess.Y - Player.lastTileRangeY ||
					 playerY > storageAccess.Y + Player.lastTileRangeY + 1))
				{
					SoundEngine.PlaySound(SoundID.MenuClose);
					CloseStorage();
					Recipe.FindRecipes();
				}
				else if (modTile is not StorageAccess || (remoteAccess && remoteCrafting && modTile is not CraftingAccess) || (remoteAccess && !Items.PortableAccess.PlayerCanBeRemotelyConnectedToStorage(Player, storageAccess)))
				{
					SoundEngine.PlaySound(SoundID.MenuClose);
					CloseStorage();
					Recipe.FindRecipes();
				}
			}
		}

		public void OpenStorage(Point16 point, bool remote = false)
		{
			storageAccess = point;
			remoteAccess = remote;

			Main.playerInventory = true;

			MagicUI.OpenUI();

			if (MagicStorageConfig.UseConfigFilter && MagicUI.craftingUI.GetPage<CraftingUIState.RecipesPage>("Crafting") is CraftingUIState.RecipesPage page)
			{
				page.recipeButtons.Choice = MagicStorageConfig.ShowAllRecipes
					? 1   //Show all recipes
					: 0;  //Show available recipes

				page.recipeButtons.OnChanged();
			}

			if (MagicStorageConfig.ClearSearchText)
			{
				MagicUI.storageUI?.GetPage<StorageUIState.StoragePage>("Storage")?.searchBar?.Reset();
				MagicUI.craftingUI?.GetPage<CraftingUIState.RecipesPage>("Crafting")?.searchBar?.Reset();
			}

			StorageGUI.RefreshItems();
		}

		//Intended to only be used with StorageHeartAccessWrapper
		internal void OpenStorageUnsafely(Point16 point) {
			storageAccess = point;
			remoteAccess = true;

			StorageGUI.RefreshItems();
		}

		public void CloseStorage()
		{
			CloseStorageUnsafely();

			remoteAccess = false;
			remoteCrafting = false;
			portableAccessRangePlayerToPylons = 0;
			portableAccessRangePylonsToStorage = 0;
		}

		//Intended to only be used with PortableAccess
		internal void CloseStorageUnsafely() {
			if (StorageEnvironment()) {
				NetHelper.ClientRequestForceCraftingGUIRefresh();

				EnvironmentGUI.currentAccess = null;
			}

			storageAccess = Point16.NegativeOne;
			Main.blockInput = false;

			wirelessLatency = -1;

			MagicUI.CloseUI();
		}

		public Point16 ViewingStorage() => storageAccess;

		public static void GetItem(IEntitySource source, Item item, bool toMouse)
		{
			Player player = Main.LocalPlayer;
			if (toMouse && Main.playerInventory && Main.mouseItem.IsAir)
			{
				Main.mouseItem = item;
				return;
			}

			if (toMouse && Main.playerInventory && Main.mouseItem.type == item.type)
			{
				int total = Main.mouseItem.stack + item.stack;
				if (total > Main.mouseItem.maxStack)
					total = Main.mouseItem.maxStack;
				int difference = total - Main.mouseItem.stack;
				Main.mouseItem.stack = total;
				item.stack -= difference;
			}

			if (item.IsAir)
				return;

			item = player.GetItem(Main.myPlayer, item, GetItemSettings.InventoryEntityToPlayerInventorySettings);
			if (item.IsAir)
				return;

			if (Main.mouseItem.IsAir)
			{
				Main.mouseItem = item;
				return;
			}

			if (ItemCombining.CanCombineItems(Main.mouseItem, item) && Main.mouseItem.stack + item.stack < Main.mouseItem.maxStack)
			{
				int stack = item.stack;
				Utility.CustomStackItems(Main.mouseItem, item);
				item.stack = stack;
				return;
			}

			player.QuickSpawnClonedItem(source, item, item.stack);
		}

		public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
		{
			if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo)
				return false;
			if (storageAccess.X < 0 || storageAccess.Y < 0)
				return false;
			Item item = inventory[slot];
			if (item.favorited || item.IsAir)
				return false;
			int oldType = item.type;
			int oldStack = item.stack;
			GetStorageHeart().TryDeposit(item);

			if (item.type != oldType || item.stack != oldStack)
			{
				SoundEngine.PlaySound(SoundID.Grab);
				StorageGUI.needRefresh = true;
			}

			return true;
		}

		public TEStorageHeart GetStorageHeart()
		{
			if (storageAccess.X < 0 || storageAccess.Y < 0)
				return null;
			Tile tile = Main.tile[storageAccess.X, storageAccess.Y];
			if (!tile.HasTile)
				return null;
			ModTile modTile = TileLoader.GetTile(tile.TileType);
			return (modTile as StorageAccess)?.GetHeart(storageAccess.X, storageAccess.Y);
		}

		public TECraftingAccess GetCraftingAccess()
		{
			if (storageAccess.X < 0 || storageAccess.Y < 0)
				return null;

			if (TileEntity.ByPosition.TryGetValue(storageAccess, out TileEntity te))
				return te as TECraftingAccess;

			return null;
		}

		public bool StorageCrafting()
		{
			if (storageAccess.X < 0 || storageAccess.Y < 0)
				return false;
			Tile tile = Main.tile[storageAccess.X, storageAccess.Y];
			return tile.HasTile && tile.TileType == ModContent.TileType<CraftingAccess>();
		}

		public static bool IsStorageCrafting() => StoragePlayer.LocalPlayer.StorageCrafting();

		public bool StorageEnvironment() {
			if (storageAccess.X < 0 || storageAccess.Y < 0)
				return false;
			Tile tile = Main.tile[storageAccess.X, storageAccess.Y];
			return tile.HasTile && tile.TileType == ModContent.TileType<EnvironmentAccess>();
		}

		public static bool IsStorageEnvironment() => StoragePlayer.LocalPlayer.StorageEnvironment();

		public class StorageHeartAccessWrapper {
			public Point16 Storage { get; private set; }

			public Point16 HeartLocation { get; private set; }

			public bool Valid => Storage.X >= 0 && Storage.Y >= 0 && HeartLocation.X >= 0 && HeartLocation.Y >= 0;

			public TEStorageHeart Heart => Valid && TileEntity.ByPosition.TryGetValue(HeartLocation, out TileEntity te) && te is TEStorageHeart heart ? heart : null;

			private Point16 oldPosition = Point16.NegativeOne;
			private bool oldRemote = false, oldCrafting = false;

			public StorageHeartAccessWrapper(TEStorageHeart heart) {
				Storage = HeartLocation = heart?.Position ?? Point16.NegativeOne;
			}

			public StorageHeartAccessWrapper(TEStorageCenter center) {
				Storage = center?.Position ?? Point16.NegativeOne;
				HeartLocation = center?.GetHeart()?.Position ?? Point16.NegativeOne;
			}

			public StorageHeartAccessWrapper(TECraftingAccess crafting) {
				Storage = crafting?.Position ?? Point16.NegativeOne;

				TEStorageHeart newHeart = crafting is not null ? ModContent.GetInstance<CraftingAccess>().GetHeart(crafting.Position.X, crafting.Position.Y) : null;

				HeartLocation = newHeart?.Position ?? Point16.NegativeOne;
			}

			public IDisposable OpenStorage() {
				oldPosition = LocalPlayer.ViewingStorage();
				oldRemote = LocalPlayer.remoteAccess;
				oldCrafting = LocalPlayer.remoteCrafting;
				LocalPlayer.OpenStorageUnsafely(Storage);

				return new Disposable(this);
			}

			public void CloseStorage() {
				if (Storage != Point16.NegativeOne && oldPosition != Point16.NegativeOne) {
					LocalPlayer.OpenStorageUnsafely(oldPosition);
					LocalPlayer.remoteAccess = oldRemote;
					LocalPlayer.remoteCrafting = oldCrafting;

					Storage = Point16.NegativeOne;
					HeartLocation = Point16.NegativeOne;
					oldPosition = Point16.NegativeOne;
					oldRemote = false;
					oldCrafting = false;
				}
			}

			private class Disposable : IDisposable {
				public readonly StorageHeartAccessWrapper wrapper;

				public Disposable(StorageHeartAccessWrapper wrapper) => this.wrapper = wrapper;

				public void Dispose() => wrapper.CloseStorage();
			}
		}
	}
}
