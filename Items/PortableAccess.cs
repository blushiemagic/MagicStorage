using MagicStorage.Components;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class PortableAccess : Locator
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Purple;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 28;
			Item.useTime = 28;
			Item.value = Item.sellPrice(gold: 10);
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer) {
				if (Main.autoPause)
					player.GetModPlayer<StoragePlayer>().pendingRemoteOpen = true;
				else
					DoOpenStorage(player);
			}

			return true;
		}

		public override void HoldItem(Player player) {
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();

			if (!Main.autoPause)
				modPlayer.pendingRemoteOpen = false;

			if (player.whoAmI == Main.myPlayer && player.ItemTimeIsZero && modPlayer.pendingRemoteOpen) {
				DoOpenStorage(player);
				modPlayer.pendingRemoteOpen = false;
			}
		}

		/// <summary>
		/// This method determines if this portable access has a limited range and, if it does, what the range is
		/// </summary>
		/// <returns><see langword="true"/> to indicate that this portable access has a limited range, <see langword="false"/> otherwise.</returns>
		public virtual bool GetEffectiveRange(out float playerToPylonRange, out int pylonToStorageTileRange) {
			playerToPylonRange = -1;
			pylonToStorageTileRange = -1;
			return false;
		}

		protected virtual void OpenContext(out int validTileType, out string missingAccessKey, out string unlocatedAccessKey, out bool openCrafting) {
			validTileType = ModContent.TileType<Components.StorageHeart>();
			missingAccessKey = "Mods.MagicStorage.PortableAccessMissing";
			unlocatedAccessKey = "Mods.MagicStorage.PortableAccessUnlocated";
			openCrafting = false;
		}

		private void DoOpenStorage(Player player) {
			StoragePlayer mp = player.GetModPlayer<StoragePlayer>();

			OpenContext(out int validTileType, out string missingAccessKey, out string unlocatedAccessKey, out bool openCrafting);

			Point16 location = Location;
			if (location.X >= 0 && location.Y >= 0)
			{
				Tile tile = Main.tile[location.X, location.Y];
				if (!tile.HasTile || tile.TileType != validTileType || tile.TileFrameX != 0 || tile.TileFrameY != 0)
					Main.NewText(Language.GetTextValue(missingAccessKey));
				else {
					if (!GetEffectiveRange(out float playerRange, out int pylonRange) || playerRange < 0)
						mp.portableAccessRangePlayerToPylons = -1;
					else
						mp.portableAccessRangePlayerToPylons = playerRange;
					
					mp.portableAccessRangePylonsToStorage = pylonRange;

					OpenStorage(player, openCrafting);
				}
			}
			else
			{
				Main.NewText(Language.GetTextValue(unlocatedAccessKey));
			}
		}

		public static bool PlayerCanBeRemotelyConnectedToStorage(Player player, Point16 accessLocation) {
			StoragePlayer mp = player.GetModPlayer<StoragePlayer>();

			if (mp.wirelessLatency >= 0)
				return true;  // Pretend that the player is close enough

			mp.wirelessLatency = StoragePlayer.MaxLatency;

			if (accessLocation.X < 0 || accessLocation.Y < 0)
				return false;

			if (Utility.GetHeartFromAccess(accessLocation) is not TEStorageHeart heart)
				return false;

			if (Utility.PlayerIsNearAccess(player, accessLocation, mp.portableAccessRangePlayerToPylons))
				return true;

			return Utility.NearbyPylons(player, mp.portableAccessRangePlayerToPylons).Any() && Utility.StorageSystemHasNearbyPylon(player, heart, mp.portableAccessRangePylonsToStorage);
		}

		protected void OpenStorage(Player player, bool crafting = false)
		{
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			if (player.sign > -1)
			{
				SoundEngine.PlaySound(SoundID.MenuClose);
				player.sign = -1;
				Main.editSign = false;
				Main.npcChatText = string.Empty;
			}

			if (Main.editChest)
			{
				SoundEngine.PlaySound(SoundID.MenuTick);
				Main.editChest = false;
				Main.npcChatText = string.Empty;
			}

			if (player.editedChestName)
			{
				NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
				player.editedChestName = false;
			}

			if (player.talkNPC > -1)
			{
				player.SetTalkNPC(-1);
				Main.npcChatCornerItem = 0;
				Main.npcChatText = string.Empty;
			}

			bool hadChestOpen = player.chest != -1;
			player.chest = -1;
			Main.stackSplit = 600;
			Point16 location = Location;
			Point16 toOpen = location;
			Point16 prevOpen = modPlayer.ViewingStorage();

			bool canOpen = true;
			bool canAccessStorageFromAnywhere = modPlayer.portableAccessRangePlayerToPylons < 0;
			
			TEStorageHeart heart = Utility.GetHeartFromAccess(location);

			if (!canAccessStorageFromAnywhere && !Utility.PlayerIsNearAccess(player, location, modPlayer.portableAccessRangePlayerToPylons)) {
				if (!Utility.NearbyPylons(player, modPlayer.portableAccessRangePlayerToPylons).Any()) {
					Main.NewText(Language.GetTextValue("Mods.MagicStorage.PortableAccessOutOfRange"));
					canOpen = false;
				} else if (modPlayer.portableAccessRangePylonsToStorage >= 0 && !Utility.StorageSystemHasNearbyPylon(player, heart, modPlayer.portableAccessRangePylonsToStorage)) {
					Main.NewText(Language.GetTextValue("Mods.MagicStorage.PortableAccessNoPylons"));
					canOpen = false;
				}
			}

			if (!canOpen)
			{
				modPlayer.CloseStorage();
				Recipe.FindRecipes();
				modPlayer.portableAccessRangePlayerToPylons = 0;
				modPlayer.portableAccessRangePylonsToStorage = 0;
			}
			else if (prevOpen == toOpen)
			{
				modPlayer.CloseStorage();
				SoundEngine.PlaySound(SoundID.MenuClose);
				Recipe.FindRecipes();
			}
			else
			{
				bool hadOtherOpen = prevOpen.X >= 0 && prevOpen.Y >= 0;
				if (hadOtherOpen)
					modPlayer.CloseStorageUnsafely();

				modPlayer.OpenStorage(toOpen, true);
				modPlayer.remoteCrafting = crafting;
				modPlayer.timeSinceOpen = 0;
				modPlayer.wirelessLatency = StoragePlayer.MaxLatency;
				Main.playerInventory = true;
				Main.recBigList = false;
				SoundEngine.PlaySound(hadChestOpen || hadOtherOpen ? SoundID.MenuTick : SoundID.MenuOpen);
				Recipe.FindRecipes();
			}
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<LocatorDisk>();
			recipe.AddIngredient<RadiantJewel>();
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 3);
			recipe.AddIngredient(ItemID.Ruby, 3);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.Register();
		}
	}
}
