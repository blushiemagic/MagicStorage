using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
    public class UpgradeCrimtane : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimtane Storage Upgrade");
            DisplayName.AddTranslation(GameCulture.Russian, "Кримтановое Улучшение Ячейки Хранилища");
            DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Karmazynit)");
            DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Carmitane)");
            DisplayName.AddTranslation(GameCulture.Spanish, "Actualización de Unidad de Almacenamiento (Carmesí)");
            DisplayName.AddTranslation(GameCulture.Chinese, "存储升级珠(血腥))");
            DisplayName.AddTranslation(GameCulture.Portuguese, "Melhoria de Armazenamento (Carmim)");

            Tooltip.SetDefault("Upgrades Storage Unit to 80 capacity"
                + "\n<right> a Storage Unit to use");
            Tooltip.AddTranslation(GameCulture.Russian, "Увеличивает количество слотов в Ячейке Хранилища до 80"
                + "\n<right> на Ячейке Хранилища для улучшения");
            Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 80 miejsc"
                + "\n<right> na Jednostkę magazynującą (Standardową), aby użyć");
            Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 80"
                + "\n<right> l'unité de stockage pour utiliser");
            Tooltip.AddTranslation(GameCulture.Spanish, "Capacidad de unidad de almacenamiento mejorada a 80"
                + "\n<right> en la unidad de almacenamiento para utilizar");
            Tooltip.AddTranslation(GameCulture.Chinese, "将存储单元升级至80容量"
                + "\n<right>一个存储单元(血腥)可镶嵌");
            Tooltip.AddTranslation(GameCulture.Portuguese, "Melhora a unidade de armazenamento para abrigar até 80 itens diferentes"
                + "\n<right> uma Unidade de Armazenamento para usar");
        }

        public override void SetDefaults()
        {
            item.width = 12;
            item.height = 12;
            item.maxStack = 99;
            item.rare = 1;
            item.value = Item.sellPrice(0, 0, 32, 0);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CrimtaneBar, 10);
            if (MagicStorage.legendMod == null)
            {
                recipe.AddIngredient(ItemID.Amethyst);
            }
            else
            {
                recipe.AddRecipeGroup("MagicStorage:AnyAmethyst");
            }
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
