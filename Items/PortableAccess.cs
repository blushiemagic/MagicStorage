using System.Collections.Generic;
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
			DisplayName.SetDefault("Portable Remote Storage Access");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Портативный Модуль Удаленного Доступа к Хранилищу");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "便携式远程存储装置");

			Tooltip.SetDefault("<right> Storage Heart to store location" + "\nCurrently not set to any location" + "\nUse item to access your storage");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "<right> по Cердцу Хранилища чтобы запомнить его местоположение" + "\nВ данный момент Сердце Хранилища не привязанно" + "\nИспользуйте что бы получить доступ к вашему Хранилищу");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "<right>存储核心可储存其定位点" + "\n目前未设置为任何位置" + "\n使用可直接访问你的存储");

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
			Item.value = Item.sellPrice(0, 10);
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				if (location.X >= 0 && location.Y >= 0)
				{
					Tile tile = Main.tile[location.X, location.Y];
					if (!tile.IsActive || tile.type != ModContent.TileType<Components.StorageHeart>() || tile.frameX != 0 || tile.frameY != 0)
						Main.NewText("Storage Heart is missing!");
					else
						OpenStorage(player);
				}
				else
				{
					Main.NewText("Locator is not set to any Storage Heart");
				}
			}

			return true;
		}

		private void OpenStorage(Player player)
		{
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			if (player.sign > -1)
			{
				SoundEngine.PlaySound(11);
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
			Point16 toOpen = location;
			Point16 prevOpen = modPlayer.ViewingStorage();
			if (prevOpen == toOpen)
			{
				modPlayer.CloseStorage();
				SoundEngine.PlaySound(11);
				lock (BlockRecipes.activeLock)
				{
					Recipe.FindRecipes();
				}
			}
			else
			{
				bool hadOtherOpen = prevOpen.X >= 0 && prevOpen.Y >= 0;
				modPlayer.OpenStorage(toOpen, true);
				modPlayer.timeSinceOpen = 0;
				Main.playerInventory = true;
				Main.recBigList = false;
				SoundEngine.PlaySound(hadChestOpen || hadOtherOpen ? 12 : 10);
				lock (BlockRecipes.activeLock)
				{
					Recipe.FindRecipes();
				}
			}
		}

		public override void ModifyTooltips(List<TooltipLine> lines)
		{
			bool isSet = location.X >= 0 && location.Y >= 0;
			for (int k = 0; k < lines.Count; k++)
				if (isSet && lines[k].mod == "Terraria" && lines[k].Name == "Tooltip1")
				{
					lines[k].text = Language.GetTextValue("Mods.MagicStorage.SetTo", location.X, location.Y);
				}
				else if (!isSet && lines[k].mod == "Terraria" && lines[k].Name == "Tooltip2")
				{
					lines.RemoveAt(k);
					k--;
				}
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(Mod, "LocatorDisk");
			recipe.AddIngredient(Mod, "RadiantJewel");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 3);
			recipe.AddIngredient(ItemID.Ruby, 3);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.Register();

			if (ModLoader.TryGetMod("CalamityMod", out Mod otherMod))
			{
				recipe = CreateRecipe();
				recipe.AddIngredient(Mod, "LocatorDisk");
				recipe.AddIngredient(otherMod, "CosmiliteBar", 20);
				recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 3);
				recipe.AddIngredient(ItemID.Ruby, 3);
				recipe.AddTile(TileID.LunarCraftingStation);
				recipe.Register();
			}
		}
	}
}
