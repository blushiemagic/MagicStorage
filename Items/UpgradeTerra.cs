using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.GameContent.Creative;

namespace MagicStorage.Items
{
    public class UpgradeTerra : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Terra Storage Upgrade");
            DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Терра Улучшение Ячейки Хранилища");
            DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Ulepszenie jednostki magazynującej (Terra)");
            DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Amélioration d'Unité de stockage (Terra)");
            DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Actualización de Unidad de Almacenamiento (Tierra)");
            DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储升级珠(泰拉)");

            Tooltip.SetDefault("Upgrades Storage Unit to 640 capacity"
                + "\n<right> a Luminite Storage Unit to use");
            Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Увеличивает количество слотов в Ячейке Хранилища до 640"
                + "\n<right> на Люминитовой Ячейке Хранилища для улучшения");
            Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Ulepsza jednostkę magazynującą do 640 miejsc"
                + "\n<right> na Jednostkę magazynującą (Luminowaną), aby użyć");
            Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "améliore la capacité de unité de stockage à 640"
                + "\n<right> l'unité de stockage (Luminite) pour utiliser");
            Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Capacidad de unidad de almacenamiento mejorada a 640"
                + "\n<right> en la unidad de almacenamiento (Luminita) para utilizar");
            Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "将存储单元升级至640容量"
                + "\n<right>一个存储单元(泰拉)可镶嵌");

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.maxStack = 99;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(0, 10, 0, 0);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(null, "RadiantJewel");
            recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();

            if (ModLoader.TryGetMod("CalamityMod", out Mod otherMod))
            {
                recipe = CreateRecipe();
                recipe.AddIngredient(otherMod, "CosmiliteBar", 20);
                recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
        }
    }
}
