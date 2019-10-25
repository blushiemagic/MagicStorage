using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
    public class StorageConnector : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.AddTranslation(GameCulture.Russian, "Соединитель Ячеек Хранилища");
            DisplayName.AddTranslation(GameCulture.Polish, "Łącznik");
            DisplayName.AddTranslation(GameCulture.French, "Connecteur de Stockage");
            DisplayName.AddTranslation(GameCulture.Spanish, "Conector de Almacenamiento");
            DisplayName.AddTranslation(GameCulture.Chinese, "存储连接器");
        }    
    
        public override void SetDefaults()
        {
            item.width = 12;
            item.height = 12;
            item.maxStack = 999;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 10;
            item.useStyle = 1;
            item.consumable = true;
            item.rare = 0;
            item.value = Item.sellPrice(0, 0, 0, 10);
            item.createTile = mod.TileType("StorageConnector");
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 16);
            recipe.AddIngredient(ItemID.IronBar);
            recipe.anyWood = true;
            recipe.anyIronBar = true;
            recipe.AddTile(TileID.WorkBenches);
            recipe.SetResult(this, 16);
            recipe.AddRecipe();
        }
    }
}
