using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Gets the tooltip text lines Terraria would render for <paramref name="item"/>.
		/// </summary>
		public static List<string> GetItemTooltipLines(Item item) {
			Item hoverItem = item;
			int yoyoLogo = -1;
			int researchLine = -1;
			int rare = hoverItem.rare;
			float oldKB = hoverItem.knockBack;
			float knockbackModifier = 1f;
			if (hoverItem.CountsAsClass(DamageClass.Melee) && Main.LocalPlayer.kbGlove)
				knockbackModifier += 1f;

			if (Main.LocalPlayer.kbBuff)
				knockbackModifier += 0.5f;

			if (knockbackModifier != 1f)
				hoverItem.knockBack *= knockbackModifier;

			if (hoverItem.CountsAsClass(DamageClass.Ranged) && Main.LocalPlayer.shroomiteStealth)
				hoverItem.knockBack *= 1f + (1f - Main.LocalPlayer.stealth) * 0.5f;

			long maxLines = 30;
			int numLines = 1;
			string[] toolTipLine = new string[maxLines];
			bool[] preFixLine = new bool[maxLines];
			bool[] badPreFixLine = new bool[maxLines];
			for (int i = 0; i < maxLines; i++) {
				preFixLine[i] = false;
				badPreFixLine[i] = false;
			}
			string[] toolTipNames = new string[maxLines];

			Main.MouseText_DrawItemTooltip_GetLinesInfo(item, ref yoyoLogo, ref researchLine, oldKB, ref numLines, toolTipLine, preFixLine, badPreFixLine, toolTipNames, out int prefixlineIndex);

			// "Main.HoverItem" is set every render tick, but this method can be given "any item"
			// Hence, to ensure that the knockback of the item doesn't grow to infinity, reset it to what it used to be
			hoverItem.knockBack = oldKB;

			if (Main.npcShop > 0 && hoverItem.value >= 0 && (hoverItem.type < ItemID.CopperCoin || hoverItem.type > ItemID.PlatinumCoin)) {
				Main.LocalPlayer.GetItemExpectedPrice(hoverItem, out long calcForSelling, out long calcForBuying);

				long num5 = (hoverItem.isAShopItem || hoverItem.buyOnce) ? calcForBuying : calcForSelling;
				if (hoverItem.shopSpecialCurrency != -1) {
					toolTipNames[numLines] = "SpecialPrice";
					CustomCurrencyManager.GetPriceText(hoverItem.shopSpecialCurrency, toolTipLine, ref numLines, num5);
				} else if (num5 > 0) {
					string text = "";
					long num6 = 0;
					long num7 = 0;
					long num8 = 0;
					long num9 = 0;
					long num10 = num5 * hoverItem.stack;
					if (!hoverItem.buy) {
						num10 = num5 / 5;
						if (num10 < 1)
							num10 = 1;

						long num11 = num10;
						num10 *= hoverItem.stack;
						int amount = Main.shopSellbackHelper.GetAmount(hoverItem);
						if (amount > 0)
							num10 += (-num11 + calcForBuying) * Math.Min(amount, hoverItem.stack);
					}

					if (num10 < 1)
						num10 = 1;

					if (num10 >= 1000000) {
						num6 = num10 / 1000000;
						num10 -= num6 * 1000000;
					}

					if (num10 >= 10000) {
						num7 = num10 / 10000;
						num10 -= num7 * 10000;
					}

					if (num10 >= 100) {
						num8 = num10 / 100;
						num10 -= num8 * 100;
					}

					if (num10 >= 1)
						num9 = num10;

					if (num6 > 0)
						text = text + num6 + " " + Lang.inter[15].Value + " ";

					if (num7 > 0)
						text = text + num7 + " " + Lang.inter[16].Value + " ";

					if (num8 > 0)
						text = text + num8 + " " + Lang.inter[17].Value + " ";

					if (num9 > 0)
						text = text + num9 + " " + Lang.inter[18].Value + " ";

					if (!hoverItem.buy)
						toolTipLine[numLines] = Lang.tip[49].Value + " " + text;
					else
						toolTipLine[numLines] = Lang.tip[50].Value + " " + text;

					toolTipNames[numLines] = "Price";
					numLines++;
				} else if (hoverItem.type != ItemID.DefenderMedal) {
					toolTipLine[numLines] = Lang.tip[51].Value;
					toolTipNames[numLines] = "Price";
					numLines++;
				}
			}

			List<TooltipLine> lines = ItemLoader.ModifyTooltips(item, ref numLines, toolTipNames, ref toolTipLine, ref preFixLine, ref badPreFixLine, ref yoyoLogo, out _, prefixlineIndex);

			return [.. lines.Select(line => line.Text)];
		}
	}
}
