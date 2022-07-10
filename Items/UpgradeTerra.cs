﻿using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
    public class UpgradeTerra : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Terra Storage Upgrade");
            DisplayName.AddTranslation(GameCulture.Russian, "Терра Улучшение Ячейки Хранилища");
            DisplayName.AddTranslation(GameCulture.Polish, "Ulepszenie jednostki magazynującej (Terra)");
            DisplayName.AddTranslation(GameCulture.French, "Amélioration d'Unité de stockage (Terra)");
            DisplayName.AddTranslation(GameCulture.Spanish, "Actualización de Unidad de Almacenamiento (Tierra)");
            DisplayName.AddTranslation(GameCulture.Chinese, "存储升级珠(泰拉)");
            DisplayName.AddTranslation(GameCulture.Portuguese, "Melhoria de Armazenamento (Terra)");

            Tooltip.SetDefault("Upgrades Storage Unit to 640 capacity"
                + "\n<right> a Luminite Storage Unit to use");
            Tooltip.AddTranslation(GameCulture.Russian, "Увеличивает количество слотов в Ячейке Хранилища до 640"
                + "\n<right> на Люминитовой Ячейке Хранилища для улучшения");
            Tooltip.AddTranslation(GameCulture.Polish, "Ulepsza jednostkę magazynującą do 640 miejsc"
                + "\n<right> na Jednostkę magazynującą (Luminowaną), aby użyć");
            Tooltip.AddTranslation(GameCulture.French, "améliore la capacité de unité de stockage à 640"
                + "\n<right> l'unité de stockage (Luminite) pour utiliser");
            Tooltip.AddTranslation(GameCulture.Spanish, "Capacidad de unidad de almacenamiento mejorada a 640"
                + "\n<right> en la unidad de almacenamiento (Luminita) para utilizar");
            Tooltip.AddTranslation(GameCulture.Chinese, "将存储单元升级至640容量"
                + "\n<right>一个存储单元(泰拉)可镶嵌");
            Tooltip.AddTranslation(GameCulture.Portuguese, "Melhora a unidade de armazenamento para abrigar até 640 itens diferentes"
                + "\n<right> uma Unidade de Armazenamento (Luminita) para usar");
        }

        public override void SetDefaults()
        {
            item.width = 12;
            item.height = 12;
            item.maxStack = 99;
            item.rare = 11;
            item.value = Item.sellPrice(0, 10, 0, 0);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(null, "RadiantJewel");
            recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.SetResult(this);
            recipe.AddRecipe();

            Mod otherMod = MagicStorage.bluemagicMod;
            if (otherMod != null)
            {
                recipe = new ModRecipe(mod);
                recipe.AddIngredient(otherMod, "InfinityCrystal");
                recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
                recipe.AddTile(otherMod, "PuriumAnvil");
                recipe.SetResult(this);
                recipe.AddRecipe();
            }

            otherMod = ModLoader.GetMod("CalamityMod");
            if (otherMod != null)
            {
                recipe = new ModRecipe(mod);
                recipe.AddIngredient(otherMod, "CosmiliteBar", 20);
                recipe.AddRecipeGroup("MagicStorage:AnyDiamond");
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.SetResult(this);
                recipe.AddRecipe();
            }
        }
    }
}
