﻿using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class Locator : ModItem
	{
		public Point16 location = new Point16(-1, -1);

		public override bool CloneNewInstances
		{
			get
			{
				return true;
			}
		}

		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.Russian, "Локатор");
			DisplayName.AddTranslation(GameCulture.Polish, "Lokalizator");
			DisplayName.AddTranslation(GameCulture.French, "Localisateur");

			Tooltip.SetDefault("<right> Storage Heart to store location"
				+ "\n<right> Remote Storage Access to set it");
			Tooltip.AddTranslation(GameCulture.Russian, "<right> по сердцу хранилища чтобы сохранить локацию"
				+ "\n<right> по дистанционному доступу к хранилищу чтобы установить его");
			Tooltip.AddTranslation(GameCulture.Polish, "<right> na serce jednostki magazynującej, aby zapisać jej lokalizację"
				+ "\n<right> na bezprzewodowe okno dostępu aby je ustawić");
			Tooltip.AddTranslation(GameCulture.French, "<right> le Cœur de Stockage pour enregistrer son emplacement"
				+ "\n<right> Stockage Éloigné pour le mettre en place");
		}

		public override void SetDefaults()
		{
			item.width = 28;
			item.height = 28;
			item.maxStack = 1;
			item.rare = 1;
			item.value = Item.sellPrice(0, 1, 0, 0);
		}

		public override void ModifyTooltips(List<TooltipLine> lines)
		{
			bool isSet = location.X >= 0 && location.Y >= 0;
			for (int k = 0; k < lines.Count; k++)
			{
				if (isSet && lines[k].mod == "Terraria" && lines[k].Name == "Tooltip0")
				{
					lines[k].text = Language.GetTextValue("Mods.MagicStorage.SetTo", location.X, location.Y);
				}
				else if (!isSet && lines[k].mod == "Terraria" && lines[k].Name == "Tooltip1")
				{
					lines.RemoveAt(k);
					k--;
				}
			}
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.MeteoriteBar, 10);
			recipe.AddIngredient(ItemID.Amber, 5);
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}

		public override TagCompound Save()
		{
			TagCompound tag = new TagCompound();
			tag.Set("X", location.X);
			tag.Set("Y", location.Y);
			return tag;
		}

		public override void Load(TagCompound tag)
		{
			location = new Point16(tag.GetShort("X"), tag.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(location.X);
			writer.Write(location.Y);
		}

		public override void NetRecieve(BinaryReader reader)
		{
			location = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
