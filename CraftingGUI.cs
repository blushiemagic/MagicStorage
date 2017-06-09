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

		private static UIElement stationZone = new UIElement();
		private static UIText stationText = new UIText("Crafting Stations");
		private static UIElement slotZone = new UIElement();

		internal static UIScrollbar scrollBar = new UIScrollbar();
		private static bool scrollBarFocus = false;
		private static int scrollBarFocusMouseStart;
		private static float scrollBarFocusPositionStart;
		private static float scrollBarViewSize = 1f;
		private static float scrollBarMaxViewSize = 2f;

		private static List<Item> items = new List<Item>();
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

			stationZone.Width.Set(0f, 1f);
			stationZone.Top.Set(76f, 0f);
			stationZone.Height.Set(70f, 0f);
			basePanel.Append(stationZone);
			stationZone.Append(stationText);

			slotZone.Width.Set(0f, 1f);
			slotZone.Top.Set(136f, 0f);
			slotZone.Height.Set(-176f, 1f);
			basePanel.Append(slotZone);

			numRows = (items.Count + numColumns - 1) / numColumns;
			displayRows = (int)slotZone.GetDimensions().Height / ((int)itemSlotHeight + padding);
			int noDisplayRows = numRows - displayRows;
			if (noDisplayRows < 0)
			{
				noDisplayRows = 0;
			}
			scrollBarMaxViewSize = 1 + noDisplayRows;
			scrollBar.Height.Set(displayRows * (itemSlotHeight + padding), 0f);
			scrollBar.Left.Set(-20f, 1f);
			scrollBar.SetView(scrollBarViewSize, scrollBarMaxViewSize);
			slotZone.Append(scrollBar);

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
					MagicStorage.Instance.GetTexture("SortName"),
					MagicStorage.Instance.GetTexture("SortNumber")
				},
				new LocalizedText[]
				{
					Language.GetText("Mods.MagicStorage.SortDefault"),
					Language.GetText("Mods.MagicStorage.SortID"),
					Language.GetText("Mods.MagicStorage.SortName"),
					Language.GetText("Mods.MagicStorage.SortStack")
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
				UpdateItemSlots();
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
			float itemSlotWidth = Main.inventoryBackTexture.Width * inventoryScale;
			float itemSlotHeight = Main.inventoryBackTexture.Height * inventoryScale;
			Vector2 slotZonePos = slotZone.GetDimensions().Position();
			float oldScale = Main.inventoryScale;
			Main.inventoryScale = inventoryScale;
			Item[] temp = new Item[11];
			Item[] craftingStations = GetCraftingStations();
			for (int k = 0; k < numColumns; k++)
			{
				temp[10] = craftingStations[k];
				Vector2 drawPos = GetCraftSlotPos(k);
				ItemSlot.Draw(Main.spriteBatch, temp, 0, 10, drawPos);
			}
			if (hoverSlot >= 0 && hoverSlot < craftingStations.Length)
			{
				Main.HoverItem = craftingStations[hoverSlot].Clone();
				Main.instance.MouseText(string.Empty);
			}
			sortButtons.DrawText();
			filterButtons.DrawText();
			Main.inventoryScale = oldScale;
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
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>(MagicStorage.Instance);
			Point16 pos = modPlayer.ViewingStorage();
			if (pos.X < 0 || pos.Y < 0 || !TileEntity.ByPosition.ContainsKey(pos))
			{
				return null;
			}
			return TileEntity.ByPosition[pos] as TECraftingAccess;
		}

		private static Item[] GetCraftingStations()
		{
			TECraftingAccess ent = GetCraftingEntity();
			return ent == null ? null : ent.stations;
		}

		public static void RefreshItems()
		{
			items.Clear();
			TEStorageHeart heart = GetHeart();
			if (heart == null)
			{
				return;
			}
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
			case 3:
				sortMode = SortMode.Quantity;
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
			items.AddRange(ItemSorter.SortAndFilter(heart.GetStoredItems(), sortMode, filterMode, searchBar2.Text, searchBar.Text));
		}

		private static void UpdateItemSlots()
		{
			hoverSlot = -1;
			TryHoverSlot();
		}

		private static void TryHoverSlot()
		{
			Vector2 slotOrigin = stationZone.GetDimensions().Position();
			if (curMouse.X <= slotOrigin.X || curMouse.Y <= slotOrigin.Y)
			{
				return;
			}
			int itemSlotWidth = (int)(Main.inventoryBackTexture.Width * inventoryScale);
			int itemSlotHeight = (int)(Main.inventoryBackTexture.Height * inventoryScale);
			int slotX = (curMouse.X - (int)slotOrigin.X) / (itemSlotWidth + padding);
			int slotY = (curMouse.Y - (int)slotOrigin.Y) / (itemSlotHeight + padding);
			if (slotX < 0 || slotX >= numColumns || slotY < 0 || slotY >= displayRows)
			{
				return;
			}
			Vector2 slotPos = slotOrigin + new Vector2(slotX * (itemSlotWidth + padding), slotY * (itemSlotHeight + padding));
			if (curMouse.X > slotPos.X && curMouse.X < slotPos.X + itemSlotWidth && curMouse.Y > slotPos.Y && curMouse.Y < slotPos.Y + itemSlotHeight)
			{
				//HoverItemSlot(slotX + numColumns * slotY);
			}
		}

		public static Vector2 GetSlotSize()
		{
			return new Vector2(Main.inventoryBackTexture.Width, Main.inventoryBackTexture.Height) * inventoryScale;
		}

		public static Vector2 GetCraftSlotPos(int slot)
		{
			Vector2 slotSize = GetSlotSize();
			if (slot < numColumns)
			{
				CalculatedStyle dim = stationZone.GetDimensions();
				Vector2 origin = new Vector2(dim.X, dim.Y + dim.Height - slotSize.Y);
				return origin + new Vector2(slot * (slotSize.X + padding), 0f);
			}
			return Vector2.Zero;
		}
	}
}