using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class CraftingAccess : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 10;

			// Crafting Interface -> Decrafting Interface
			ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<DecraftingAccess>();
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 99;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(0, 1, 16, 25);
			Item.createTile = ModContent.TileType<Components.CraftingAccess>();
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<StorageComponent>();
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
			recipe.AddIngredient(ItemID.Sapphire, 3);
			recipe.AddTile(TileID.WorkBenches);
			// Mechs must be defeated in order to shimmer this item
			// While the item can't be decrafted, this condition is still checked for item transformations
			recipe.AddDecraftCondition(Condition.DownedMechBossAll);
			recipe.Register();
		}
	}
}
