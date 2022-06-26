using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.UI.Chat;

namespace MagicStorage.TagHandlers {
	internal class RecipeGroupTagHandler : ILoadable, ITagHandler {
		//1:1 copy of ItemTagHandler.ItemSnippet
		private class ItemSnippet : TextSnippet {
			private Item _item;

			public ItemSnippet(Item item) {
				_item = item;
				Color = ItemRarity.GetColor(item.rare);
			}

			public override void OnHover() {
				Main.HoverItem = _item.Clone();
				Main.instance.MouseText(_item.Name, _item.rare, 0);
			}

			//TODO possibly allow modders to custom draw here
			public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1f) {
				float num = 1f;
				float num2 = 1f;
				if (Main.netMode != NetmodeID.Server && !Main.dedServ) {
					Main.instance.LoadItem(_item.type);
					Texture2D value = TextureAssets.Item[_item.type].Value;
					if (Main.itemAnimations[_item.type] != null)
						Main.itemAnimations[_item.type].GetFrame(value);
					else
						value.Frame();
				}

				num2 *= scale;
				num *= num2;
				if (num > 0.75f)
					num = 0.75f;

				if (!justCheckingString && color != Color.Black) {
					float inventoryScale = Main.inventoryScale;
					Main.inventoryScale = scale * num;
					ItemSlot.Draw(spriteBatch, ref _item, ItemSlot.Context.ChatItem, position - new Vector2(10f) * scale * num, Color.White);
					Main.inventoryScale = inventoryScale;
				}

				size = new Vector2(32f) * scale * num;
				return true;
			}

			public override float GetStringLength(DynamicSpriteFont font) => 32f * Scale * 0.65f;
		}

		public Mod Mod { get; private set; } = null;

		public void Load(Mod mod) {
			Mod = mod;

			ChatManager.Register<RecipeGroupTagHandler>("rg", "recgroup");
		}

		public void Unload() {
			ConcurrentDictionary<string, ITagHandler> _handlers = typeof(ChatManager)
				.GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static)
				.GetValue(null)
				as ConcurrentDictionary<string, ITagHandler>;

			_handlers.TryRemove("rg", out _);
			_handlers.TryRemove("recgroup", out _);
		}

		TextSnippet ITagHandler.Parse(string text, Color baseColor, string options) {
			//Copy of ItemTagHandler, but for recipe groups
			Item item = new();

			RecipeGroup group = null;
			if (RecipeGroup.recipeGroupIDs.TryGetValue(text, out int id) && RecipeGroup.recipeGroups.TryGetValue(id, out group))
				item.netDefaults(group.IconicItemId);

			if (item.type <= ItemID.None)
				return new TextSnippet(text);

			item.stack = 1;

			//Check options
			if (options is not null) {
				string[] array = options.Split(',');
				for (int i = 0; i < array.Length; i++) {
					if (array[i].Length == 0)
						continue;

					switch (array[i][0]) {
						case 'd': // MID (ModItemData) is present, we will override
							item = ItemIO.FromBase64(array[i][1..]);
							break;
						case 's':
						case 'x':
							if (int.TryParse(array[i].AsSpan(1), out int stack))
								item.stack = Utils.Clamp(stack, 1, item.maxStack);

							break;
					}
				}
			}

			item.SetNameOverride(group.GetText());

			string str = "";
			if (item.stack > 1)
				str = " (" + item.stack + ")";

			return new ItemSnippet(item) {
				Text = "[" + item.AffixName() + str + "]",
				CheckForHover = true,
				DeleteWhole = true
			};
		}
	}
}
