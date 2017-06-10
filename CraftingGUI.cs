using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using MagicStorage.Components;
using MagicStorage.Sorting;

namespace MagicStorage
{
	public static class CraftingGUI
	{
		private const int padding = 4;
		private const int numColumns = 10;
		private const float inventoryScale = 0.85f;
		private const float recipeScale = 0.7f;

		public static MouseState curMouse;
		public static MouseState oldMouse;
		public static bool MouseClicked
		{
			get
			{
				return curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
			}
		}

		private static UIPanel basePanel = new UIPanel();
		private static float panelTop;
		private static float panelLeft;
		private static float panelWidth;
		private static float panelHeight;

		private static UIElement topBar = new UIElement();
		internal static UISearchBar searchBar;
		private static UIButtonChoice sortButtons;
		private static UIElement topBar2 = new UIElement();
		private static UIButtonChoice filterButtons;
		internal static UISearchBar searchBar2;

		private static UIText stationText = new UIText(Language.GetText("Mods.MagicStorage.CraftingStations"));
		private static UISlotZone stationZone = new UISlotZone(HoverStation, GetStation);
		private static UIText recipeText = new UIText(Language.GetText("Mods.MagicStorage.Recipes"));
		private static UISlotZone recipeZone = new UISlotZone(HoverRecipe, GetRecipe);

		internal static UIScrollbar scrollBar = new UIScrollbar();
		private static bool scrollBarFocus = false;
		private static int scrollBarFocusMouseStart;
		private static float scrollBarFocusPositionStart;
		private static float scrollBarViewSize = 1f;
		private static float scrollBarMaxViewSize = 2f;

		private static List<Item> items = new List<Item>();
		private static List<Recipe> recipes = new List<Recipe>();
		private static int numRows;
		private static int displayRows;
		private static int hoverSlot = -1;
		private static int slotFocus = -1;
		private static int rightClickTimer = 0;
		private const int startMaxRightClickTimer = 20;
		private static int maxRightClickTimer = startMaxRightClickTimer;

		private static UIElement bottomBar = new UIElement();
		private static UIText capacityText = new UIText("Items");

		public static void Initialize()
		{
			InitLangStuff();
			float itemSlotWidth = Main.inventoryBackTexture.Width * inventoryScale;
			float itemSlotHeight = Main.inventoryBackTexture.Height * inventoryScale;

			panelTop = Main.instance.invBottom + 60;
			panelLeft = 20f;
			float innerPanelLeft = panelLeft + basePanel.PaddingLeft;
			float innerPanelWidth = numColumns * (itemSlotWidth + padding) + 20f + padding;
			panelWidth = basePanel.PaddingLeft + innerPanelWidth + basePanel.PaddingRight;
			panelHeight = Main.screenHeight - panelTop - 40f;
			basePanel.Left.Set(panelLeft, 0f);
			basePanel.Top.Set(panelTop, 0f);
			basePanel.Width.Set(panelWidth, 0f);
			basePanel.Height.Set(panelHeight, 0f);
			basePanel.Recalculate();

			topBar.Width.Set(0f, 1f);
			topBar.Height.Set(32f, 0f);
			basePanel.Append(topBar);

			InitSortButtons();
			topBar.Append(sortButtons);
			float sortButtonsRight = sortButtons.GetDimensions().Width + padding;

			searchBar.Left.Set(sortButtonsRight + padding, 0f);
			searchBar.Width.Set(-sortButtonsRight - 2 * padding, 1f);
			searchBar.Height.Set(0f, 1f);
			topBar.Append(searchBar);

			topBar2.Width.Set(0f, 1f);
			topBar2.Height.Set(32f, 0f);
			topBar2.Top.Set(36f, 0f);
			basePanel.Append(topBar2);

			InitFilterButtons();
			float filterButtonsRight = filterButtons.GetDimensions().Width + padding;
			topBar2.Append(filterButtons);
			searchBar2.Left.Set(filterButtonsRight + padding, 0f);
			searchBar2.Width.Set(-filterButtonsRight - 2 * padding, 1f);
			searchBar2.Height.Set(0f, 1f);
			topBar2.Append(searchBar2);

			stationText.Top.Set(76f, 0f);
			basePanel.Append(stationText);

			stationZone.Width.Set(0f, 1f);
			stationZone.Top.Set(100f, 0f);
			stationZone.Height.Set(70f, 0f);
			stationZone.SetDimensions(numColumns, 1);
			basePanel.Append(stationZone);

			recipeText.Top.Set(152f, 0f);
			basePanel.Append(recipeText);

			recipeZone.Width.Set(0f, 1f);
			recipeZone.Top.Set(176f, 0f);
			recipeZone.Height.Set(-216f, 1f);
			basePanel.Append(recipeZone);

			numRows = (recipes.Count + numColumns - 1) / numColumns;
			displayRows = (int)recipeZone.GetDimensions().Height / ((int)itemSlotHeight + padding);
			recipeZone.SetDimensions(numColumns, displayRows);
			int noDisplayRows = numRows - displayRows;
			if (noDisplayRows < 0)
			{
				noDisplayRows = 0;
			}
			scrollBarMaxViewSize = 1 + noDisplayRows;
			scrollBar.Height.Set(displayRows * (itemSlotHeight + padding), 0f);
			scrollBar.Left.Set(-20f, 1f);
			scrollBar.SetView(scrollBarViewSize, scrollBarMaxViewSize);
			recipeZone.Append(scrollBar);

			bottomBar.Width.Set(0f, 1f);
			bottomBar.Height.Set(32f, 0f);
			bottomBar.Top.Set(-32f, 1f);
			basePanel.Append(bottomBar);

			capacityText.Left.Set(6f, 0f);
			capacityText.Top.Set(6f, 0f);
			TEStorageHeart heart = GetHeart();
			int numItems = 0;
			int capacity = 0;
			if (heart != null)
			{
				foreach (TEAbstractStorageUnit abstractStorageUnit in heart.GetStorageUnits())
				{
					if (abstractStorageUnit is TEStorageUnit)
					{
						TEStorageUnit storageUnit = (TEStorageUnit)abstractStorageUnit;
						numItems += storageUnit.NumItems;
						capacity += storageUnit.Capacity;
					}
				}
			}
			capacityText.SetText(numItems + "/" + capacity + " Items");
			bottomBar.Append(capacityText);
		}

		private static void InitLangStuff()
		{
			if (searchBar == null)
			{
				searchBar = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchName"));
			}
			if (searchBar2 == null)
			{
				searchBar2 = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchMod"));
			}
		}

		private static void InitSortButtons()
		{
			if (sortButtons == null)
			{
				sortButtons = new UIButtonChoice(new Texture2D[]
				{
					Main.inventorySortTexture[0],
					MagicStorage.Instance.GetTexture("SortID"),
					MagicStorage.Instance.GetTexture("SortName")
				},
				new LocalizedText[]
				{
					Language.GetText("Mods.MagicStorage.SortDefault"),
					Language.GetText("Mods.MagicStorage.SortID"),
					Language.GetText("Mods.MagicStorage.SortName")
				});
			}
		}

		private static void InitFilterButtons()
		{
			if (filterButtons == null)
			{
				filterButtons = new UIButtonChoice(new Texture2D[]
				{
					MagicStorage.Instance.GetTexture("FilterAll"),
					MagicStorage.Instance.GetTexture("FilterMelee"),
					MagicStorage.Instance.GetTexture("FilterPickaxe"),
					MagicStorage.Instance.GetTexture("FilterArmor"),
					MagicStorage.Instance.GetTexture("FilterPotion"),
					MagicStorage.Instance.GetTexture("FilterTile"),
					MagicStorage.Instance.GetTexture("FilterMisc"),
				},
				new LocalizedText[]
				{
					Language.GetText("Mods.MagicStorage.FilterAll"),
					Language.GetText("Mods.MagicStorage.FilterWeapons"),
					Language.GetText("Mods.MagicStorage.FilterTools"),
					Language.GetText("Mods.MagicStorage.FilterEquips"),
					Language.GetText("Mods.MagicStorage.FilterPotions"),
					Language.GetText("Mods.MagicStorage.FilterTiles"),
					Language.GetText("Mods.MagicStorage.FilterMisc")
				});
			}
		}

		public static void Update(GameTime gameTime)
		{
			oldMouse = StorageGUI.oldMouse;
			curMouse = StorageGUI.curMouse;
			if (Main.playerInventory && Main.player[Main.myPlayer].GetModPlayer<StoragePlayer>(MagicStorage.Instance).ViewingStorage().X >= 0 && StoragePlayer.IsStorageCrafting())
			{
				basePanel.Update(gameTime);
				UpdateScrollBar();
			}
			else
			{
				scrollBarFocus = false;
			}
		}

		public static void Draw(TEStorageHeart heart)
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>(MagicStorage.Instance);
			Initialize();
			if (Main.mouseX > panelLeft && Main.mouseX < panelLeft + panelWidth && Main.mouseY > panelTop && Main.mouseY < panelTop + panelHeight)
			{
				player.mouseInterface = true;
				player.showItemIcon = false;
				InterfaceHelper.HideItemIconCache();
			}
			basePanel.Draw(Main.spriteBatch);
			stationZone.DrawText();
			recipeZone.DrawText();
			sortButtons.DrawText();
			filterButtons.DrawText();
		}

		private static Item GetStation(int slot, ref int context)
		{
			Item[] stations = GetCraftingStations();
			if (stations == null || slot >= stations.Length)
			{
				return new Item();
			}
			return stations[slot];
		}

		private static Item GetRecipe(int slot, ref int context)
		{
			int index = slot + numColumns * (int)Math.Round(scrollBar.ViewPosition);
			return index < recipes.Count ? recipes[index].createItem : new Item();
		}

		private static void UpdateScrollBar()
		{
			if (slotFocus >= 0)
			{
				scrollBarFocus = false;
				return;
			}
			CalculatedStyle dim = scrollBar.GetInnerDimensions();
			Vector2 boxPos = new Vector2(dim.X, dim.Y + dim.Height * (scrollBar.ViewPosition / scrollBarMaxViewSize));
			float boxWidth = 20f;
			float boxHeight = dim.Height * (scrollBarViewSize / scrollBarMaxViewSize);
			if (scrollBarFocus)
			{
				if (curMouse.LeftButton == ButtonState.Released)
				{
					scrollBarFocus = false;
				}
				else
				{
					int difference = curMouse.Y - scrollBarFocusMouseStart;
					scrollBar.ViewPosition = scrollBarFocusPositionStart + (float)difference / boxHeight;
				}
			}
			else if (MouseClicked)
			{
				if (curMouse.X > boxPos.X && curMouse.X < boxPos.X + boxWidth && curMouse.Y > boxPos.Y - 3f && curMouse.Y < boxPos.Y + boxHeight + 4f)
				{
					scrollBarFocus = true;
					scrollBarFocusMouseStart = curMouse.Y;
					scrollBarFocusPositionStart = scrollBar.ViewPosition;
				}
			}
			if (!scrollBarFocus)
			{
				int difference = oldMouse.ScrollWheelValue / 250 - curMouse.ScrollWheelValue / 250;
				scrollBar.ViewPosition += difference;
			}
		}

		private static TEStorageHeart GetHeart()
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			return modPlayer.GetStorageHeart();
		}

		private static TECraftingAccess GetCraftingEntity()
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			return modPlayer.GetCraftingAccess();
		}

		private static Item[] GetCraftingStations()
		{
			TECraftingAccess ent = GetCraftingEntity();
			return ent == null ? null : ent.stations;
		}

		public static void RefreshItems()
		{
			items.Clear();
			recipes.Clear();
			TEStorageHeart heart = GetHeart();
			if (heart == null)
			{
				return;
			}
			items.AddRange(ItemSorter.SortAndFilter(heart.GetStoredItems(), SortMode.Id, FilterMode.All, "", ""));
			InitLangStuff();
			InitSortButtons();
			InitFilterButtons();
			SortMode sortMode;
			switch (sortButtons.Choice)
			{
			case 0:
				sortMode = SortMode.Default;
				break;
			case 1:
				sortMode = SortMode.Id;
				break;
			case 2:
				sortMode = SortMode.Name;
				break;
			default:
				sortMode = SortMode.Default;
				break;
			}
			FilterMode filterMode;
			switch (filterButtons.Choice)
			{
			case 0:
				filterMode = FilterMode.All;
				break;
			case 1:
				filterMode = FilterMode.Weapons;
				break;
			case 2:
				filterMode = FilterMode.Tools;
				break;
			case 3:
				filterMode = FilterMode.Equipment;
				break;
			case 4:
				filterMode = FilterMode.Potions;
				break;
			case 5:
				filterMode = FilterMode.Placeables;
				break;
			case 6:
				filterMode = FilterMode.Misc;
				break;
			default:
				filterMode = FilterMode.All;
				break;
			}
			recipes.AddRange(ItemSorter.GetRecipes(sortMode, filterMode, searchBar2.Text, searchBar.Text));
		}

		private static void HoverStation(int slot, ref int hoverSlot)
		{
			TECraftingAccess ent = GetCraftingEntity();
			if (ent == null || slot >= ent.stations.Length)
			{
				return;
			}

			Player player = Main.player[Main.myPlayer];
			if (MouseClicked)
			{
				bool changed = false;
				if (!ent.stations[slot].IsAir && ItemSlot.ShiftInUse)
				{
					Item result = player.GetItem(Main.myPlayer, DoWithdraw(slot), false, true);
					if (!result.IsAir && Main.mouseItem.IsAir)
					{
						Main.mouseItem = result;
						result = new Item();
					}
					if (!result.IsAir && Main.mouseItem.type == result.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
					{
						Main.mouseItem.stack += result.stack;
						result = new Item();
					}
					if (!result.IsAir)
					{
						player.QuickSpawnClonedItem(result);
					}
					changed = true;
				}
				else
				{
					int oldType = Main.mouseItem.type;
					int oldStack = Main.mouseItem.stack;
					Main.mouseItem = DoStationSwap(Main.mouseItem, slot);
					if (oldType != Main.mouseItem.type || oldStack != Main.mouseItem.stack)
					{
						changed = true;
					}
				}
				if (changed)
				{
					RefreshItems();
					Main.PlaySound(7, -1, -1, 1);
				}
			}
			
			hoverSlot = slot;
		}

		private static void HoverRecipe(int slot, ref int hoverSlot)
		{
			int visualSlot = slot;
			slot += numColumns * (int)Math.Round(scrollBar.ViewPosition);
			if (slot < recipes.Count)
			{
				hoverSlot = visualSlot;
			}
		}

		private static Item DoWithdraw(int slot)
		{
			TECraftingAccess access = GetCraftingEntity();
			if (Main.netMode == 0)
			{
				return access.TryWithdrawStation(slot);
			}
			else
			{
				NetHelper.SendWithdrawStation(access.ID, slot);
				return new Item();
			}
		}

		private static Item DoStationSwap(Item item, int slot)
		{
			TECraftingAccess access = GetCraftingEntity();
			if (Main.netMode == 0)
			{
				return access.DoStationSwap(item, slot);
			}
			else
			{
				NetHelper.SendStationSlotClick(access.ID, item, slot);
				return new Item();
			}
		}
	}
}