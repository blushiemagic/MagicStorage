using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	//Don't load until we've gotten the sprites
	[Autoload(false)]
	public class BiomeGlobe : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Biome Globe");
			Tooltip.SetDefault("'The world's power is at your fingertips'" +
							   "\nAllows the crafting of recipes that require the Snow biome, Ecto Mist, Demon/Crimson Altar and Water/Lava/Honey" +
							   "\nCan be in the inventory or a Crafting Interface's station slot" +
							   "\nWhile in the inventory, Marshmallows can be cooked without needing to stand near a Campfire" +
							   "\nAllows crafting of Cooked Marshmallows in the Crafting Interface" +
							   "\nActs like a Beach Ball when thrown");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults()
		{
			Item.width = 50;
			Item.height = 50;
			Item.scale = 0.75f;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.sellPrice(silver: 15);
			Item.maxStack = 99;
		}

		public override void UpdateInventory(Player player)
		{
			player.GetModPlayer<BiomePlayer>().biomeGlobe = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddRecipeGroup("MagicStorage:AnyTombstone", 5)
				.AddIngredient<SnowBiomeEmulator>()
				.AddIngredient(ItemID.WaterBucket)
				.AddIngredient(ItemID.LavaBucket)
				.AddIngredient(ItemID.HoneyBucket)
				.AddRecipeGroup("MagicStorage:AnyCampfire", 3)
				.AddRecipeGroup("MagicStorage:AnyDemonAltar")
				.Register();
		}
	}

	public class BiomePlayer : ModPlayer
	{
		public bool biomeGlobe;

		public override void ResetEffects()
		{
			biomeGlobe = false;
		}
	}

	public class BiomeOverrideItem : GlobalItem
	{
		public override void HoldItem(Item item, Player player)
		{
			Item sItem = player.itemAnimation > 0 ? player.lastVisualizedSelectedItem : player.HeldItem;

			//Near-copy of the Marshmallow on a Stick hold code, since the IL edit forces the original code to not run should this accessory flag be true
			if (player.whoAmI == Main.myPlayer)
				if (item.type == ItemID.MarshmallowonaStick && player.GetModPlayer<BiomePlayer>().biomeGlobe)
				{
					player.miscTimer++;
					if (Main.rand.Next(5) == 0)
						player.miscTimer++;

					if (player.miscTimer > 900)
					{
						player.miscTimer = 0;
						sItem.SetDefaults(ItemID.CookedMarshmallow);

						if (player.selectedItem == 58)
							Main.mouseItem.SetDefaults(ItemID.CookedMarshmallow);

						for (int k = 0; k < 58; k++)
							if (player.inventory[k].type == sItem.type && k != player.selectedItem && player.inventory[k].stack < player.inventory[k].maxStack)
							{
								SoundEngine.PlaySound(7);
								player.inventory[k].stack++;
								sItem.SetDefaults();

								if (player.selectedItem == 58)
									Main.mouseItem.SetDefaults();
							}
					}
				}
		}
	}
}
