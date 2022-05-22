using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
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
			Item.DefaultToThrownWeapon(ModContent.ProjectileType<BiomeGlobeThrown>(), 20, 6f, false);
			Item.width = 50;
			Item.height = 50;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.sellPrice(silver: 15);
			Item.maxStack = 99;
			Item.noUseGraphic = true;
			Item.noMelee = true;
			Item.consumable = false;
			Item.UseSound = SoundID.Item1;
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

		public override bool CanUseItem(Player player)
			=> player.ownedProjectileCounts[Item.shoot] < 1;

		public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI){
			Texture2D texture = TextureAssets.Projectile[ModContent.ProjectileType<BiomeGlobeThrown>()].Value;

			spriteBatch.Draw(texture, Item.Center - Main.screenPosition, null, lightColor, rotation, texture.Size() / 2f, scale, SpriteEffects.None, 0);

			return false;
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
			if (player.whoAmI == Main.myPlayer && item.type == ItemID.MarshmallowonaStick && player.GetModPlayer<BiomePlayer>().biomeGlobe)
			{
				player.miscTimer++;
				if (Main.rand.NextBool(5))
					player.miscTimer++;

				if (player.miscTimer > 900)
				{
					player.miscTimer = 0;
					sItem.SetDefaults(ItemID.CookedMarshmallow);

					if (player.selectedItem == 58)
						Main.mouseItem.SetDefaults(ItemID.CookedMarshmallow);

					for (int k = 0; k < 58; k++)
					{
						if (player.inventory[k].type == sItem.type && k != player.selectedItem && player.inventory[k].stack < player.inventory[k].maxStack)
						{
							SoundEngine.PlaySound(SoundID.Grab);
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
}
