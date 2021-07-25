using System.Collections.Generic;
using System.Linq;
using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace MagicStorage
{
	public class StoragePlayer : ModPlayer
	{
		private readonly ItemTypeOrderedSet _craftedRecipes = new ItemTypeOrderedSet("CraftedRecipes");

		private readonly ItemTypeOrderedSet _hiddenRecipes = new ItemTypeOrderedSet("HiddenItems");

		private TEStorageHeart _latestAccessedStorage;
		public bool remoteAccess;
		private Point16 storageAccess = Point16.NegativeOne;

		public int timeSinceOpen = 1;

		public override bool CloneNewInstances => false;

		public IEnumerable<Item> HiddenRecipes => _hiddenRecipes.Items;

		public IEnumerable<Item> CraftedRecipes => _craftedRecipes.Items;

		public ItemTypeOrderedSet FavoritedRecipes { get; } = new ItemTypeOrderedSet("FavoritedRecipes");

		public ItemTypeOrderedSet SeenRecipes { get; } = new ItemTypeOrderedSet("SeenRecipes");

		public ItemTypeOrderedSet TestedRecipes { get; } = new ItemTypeOrderedSet("TestedRecipes");

		public ItemTypeOrderedSet AsKnownRecipes { get; } = new ItemTypeOrderedSet("AsKnownRecipes");

		public TEStorageHeart LatestAccessedStorage =>
			_latestAccessedStorage != null && _latestAccessedStorage.IsAlive ? _latestAccessedStorage : null;

		public bool IsRecipeHidden(Item item) => _hiddenRecipes.Contains(item);

		public bool AddToHiddenRecipes(Item item) => _hiddenRecipes.Add(item);

		public bool RemoveFromHiddenRecipes(Item item) => _hiddenRecipes.Remove(item);

		public bool AddToCraftedRecipes(Item item) => _craftedRecipes.Add(item);

		public override TagCompound Save()
		{
			var c = new TagCompound();

			_hiddenRecipes.Save(c);
			_craftedRecipes.Save(c);
			FavoritedRecipes.Save(c);
			SeenRecipes.Save(c);
			TestedRecipes.Save(c);
			AsKnownRecipes.Save(c);

			return c;
		}

		public override void Load(TagCompound tag)
		{
			_hiddenRecipes.Load(tag);
			_craftedRecipes.Load(tag);
			FavoritedRecipes.Load(tag);
			SeenRecipes.Load(tag);
			TestedRecipes.Load(tag);
			AsKnownRecipes.Load(tag);
		}

		public override void UpdateDead()
		{
			if (player.whoAmI == Main.myPlayer)
			{
				CloseStorage();
			}
		}

		public override void ResetEffects()
		{
			if (player.whoAmI != Main.myPlayer)
			{
				return;
			}

			if (timeSinceOpen < 1)
			{
				player.talkNPC = -1;
				Main.playerInventory = true;
				timeSinceOpen++;
			}

			if (storageAccess.X >= 0 && storageAccess.Y >= 0 && (player.chest != -1 || !Main.playerInventory || player.sign > -1 || player.talkNPC > -1))
			{
				CloseStorage();
				lock (BlockRecipes.activeLock)
				{
					Recipe.FindRecipes();
				}
			}
			else if (storageAccess.X >= 0 && storageAccess.Y >= 0)
			{
				int playerX = (int) (player.Center.X / 16f);
				int playerY = (int) (player.Center.Y / 16f);
				if (!remoteAccess && (playerX < storageAccess.X - player.lastTileRangeX || playerX > storageAccess.X + player.lastTileRangeX + 1 || playerY < storageAccess.Y - player.lastTileRangeY || playerY > storageAccess.Y + player.lastTileRangeY + 1))
				{
					Main.PlaySound(SoundID.MenuClose);
					CloseStorage();
					lock (BlockRecipes.activeLock)
					{
						Recipe.FindRecipes();
					}
				}
				else if (!(TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].type) is StorageAccess))
				{
					Main.PlaySound(SoundID.MenuClose);
					CloseStorage();
					lock (BlockRecipes.activeLock)
					{
						Recipe.FindRecipes();
					}
				}
			}
		}

		public void OpenStorage(Point16 point, bool remote = false)
		{
			storageAccess = point;
			remoteAccess = remote;
			_latestAccessedStorage = GetStorageHeart();
			if (MagicStorageConfig.useConfigFilter && CraftingGUI.recipeButtons != null)
			{
				CraftingGUI.recipeButtons.Choice = MagicStorageConfig.showAllRecipes ? 1 : 0;
			}

			StorageGUI.RefreshItems();
		}

		public void CloseStorage()
		{
			storageAccess = Point16.NegativeOne;
			Main.blockInput = false;
		}

		public Point16 ViewingStorage() => storageAccess;

		public static void GetItem(Item item, bool toMouse)
		{
			Player player = Main.LocalPlayer;
			if (toMouse && Main.playerInventory && Main.mouseItem.IsAir)
			{
				Main.mouseItem = item;
				item = new Item();
			}
			else if (toMouse && Main.playerInventory && Main.mouseItem.type == item.type)
			{
				int total = Main.mouseItem.stack + item.stack;
				if (total > Main.mouseItem.maxStack)
				{
					total = Main.mouseItem.maxStack;
				}

				int difference = total - Main.mouseItem.stack;
				Main.mouseItem.stack = total;
				item.stack -= difference;
			}

			if (!item.IsAir)
			{
				item = player.GetItem(Main.myPlayer, item, false, true);
				if (!item.IsAir)
				{
					player.QuickSpawnClonedItem(item, item.stack);
				}
			}
		}

		public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
		{
			if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo)
			{
				return false;
			}

			if (storageAccess.X < 0 || storageAccess.Y < 0)
			{
				return false;
			}

			Item item = inventory[slot];
			if (item.favorited || item.IsAir)
			{
				return false;
			}

			int oldType = item.type;
			int oldStack = item.stack;
			if (StorageCrafting())
			{
				if (false)
				{
					if (Main.netMode == NetmodeID.SinglePlayer)
					{
						GetCraftingAccess().TryDepositStation(item);
					}
					else
					{
						NetHelper.SendDepositStation(GetCraftingAccess().ID, item);
						item.SetDefaults(0, true);
					}
				}
			}
			else
			{
				if (Main.netMode == NetmodeID.SinglePlayer)
				{
					GetStorageHeart().DepositItem(item);
				}
				else
				{
					NetHelper.SendDeposit(GetStorageHeart().ID, item);
					item.SetDefaults(0, true);
				}
			}

			if (item.type != oldType || item.stack != oldStack)
			{
				Main.PlaySound(SoundID.Grab);
				StorageGUI.RefreshItems();
			}

			return true;
		}

		public TEStorageHeart GetStorageHeart()
		{
			if (storageAccess.X < 0 || storageAccess.Y < 0)
			{
				return null;
			}

			Tile tile = Main.tile[storageAccess.X, storageAccess.Y];
			if (tile is null)
			{
				return null;
			}

			int tileType = tile.type;
			ModTile modTile = TileLoader.GetTile(tileType);
			return (modTile as StorageAccess)?.GetHeart(storageAccess.X, storageAccess.Y);
		}

		public TECraftingAccess GetCraftingAccess()
		{
			if (storageAccess.X >= 0 && storageAccess.Y >= 0 && TileEntity.ByPosition.TryGetValue(storageAccess, out TileEntity te))
			{
				return te as TECraftingAccess;
			}

			return null;

		}

		public bool StorageCrafting()
		{
			if (storageAccess.X < 0 || storageAccess.Y < 0)
			{
				return false;
			}

			Tile tile = Main.tile[storageAccess.X, storageAccess.Y];
			return tile != null && tile.type == ModContent.TileType<CraftingAccess>();
		}

		public static bool IsStorageCrafting() => Main.LocalPlayer.GetModPlayer<StoragePlayer>().StorageCrafting();

		public override void ModifyHitByNPC(NPC npc, ref int damage, ref bool crit)
		{
			IEnumerable<Item> allItems = player.inventory.Concat(player.armor).Concat(player.dye).Concat(player.miscDyes).Concat(player.miscEquips);
			if (allItems.Any(CraftingGUI.IsTestItem))
			{
				damage *= 5;
			}
		}

		public override bool CanHitPvp(Item item, Player target) => !CraftingGUI.IsTestItem(item) && base.CanHitPvp(item, target);

		public override void OnRespawn(Player player)
		{
			IEnumerable<Item> allItems = player.inventory.Concat(player.armor).Concat(player.dye).Concat(player.miscDyes).Concat(player.miscEquips);
			foreach (Item item in allItems)
				if (CraftingGUI.IsTestItem(item))
				{
					item.TurnToAir();
				}

			if (CraftingGUI.IsTestItem(player.trashItem))
			{
				player.trashItem.TurnToAir();
			}

			base.OnRespawn(player);
		}
	}
}
