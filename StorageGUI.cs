using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using MagicStorage.Components;
using MagicStorage.Sorting;

namespace MagicStorage
{
	public static class StorageGUI
	{
		private const int padding = 4;
		private const int numColumns = 10;
		private const float inventoryScale = 0.85f;

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
		internal static UISearchBar searchBar = new UISearchBar("Search Items");
		internal static UIButtonChoice sortButtons;
		internal static UITextPanel<string> depositButton = new UITextPanel<string>("Deposit All", 1f);
		private static UIElement topBar2 = new UIElement();
		private static UIElement slotZone = new UIElement();

		internal static UIScrollbar scrollBar = new UIScrollbar();
		private static bool scrollBarFocus = false;
		private static int scrollBarFocusMouseStart;
		private static float scrollBarFocusPositionStart;
		private static float scrollBarViewSize = 1f;
		private static float scrollBarMaxViewSize = 2f;

		private static List<Item> items = new List<Item>();
		private static List<bool> didMatCheck = new List<bool>();
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

			depositButton.Left.Set(sortButtons.GetDimensions().Width + 2 * padding, 0f);
			depositButton.Width.Set(128f, 0f);
			depositButton.Height.Set(-2 * padding, 1f);
			depositButton.PaddingTop = 8f;
			depositButton.PaddingBottom = 8f;
			topBar.Append(depositButton);

			float depositButtonRight = sortButtons.GetDimensions().Width + 2 * padding + depositButton.GetDimensions().Width;
			searchBar.Left.Set(depositButtonRight + padding, 0f);
			searchBar.Width.Set(-depositButtonRight - 2 * padding, 1f);
			searchBar.Height.Set(0f, 1f);
			topBar.Append(searchBar);

			topBar2.Width.Set(0f, 1f);
			topBar2.Height.Set(32f, 0f);
			topBar2.Top.Set(36f, 0f);
			basePanel.Append(topBar2);

			slotZone.Width.Set(0f, 1f);
			slotZone.Top.Set(76f, 0f);
			slotZone.Height.Set(-116f, 1f);
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
				new string[]
				{
					"Default Sorting",
					"Sort By ID",
					"Sort By Name",
					"Sort By Stacks"
				});
			}
		}

		public static void Update(GameTime gameTime)
		{
			oldMouse = curMouse;
			curMouse = Mouse.GetState();
			if (Main.playerInventory && Main.player[Main.myPlayer].GetModPlayer<StoragePlayer>(MagicStorage.Instance).ViewingStorage().X >= 0)
			{
				basePanel.Update(gameTime);
				UpdateScrollBar();
				UpdateDepositButton();
				UpdateItemSlots();
			}
			else
			{
				scrollBarFocus = false;
				scrollBar.ViewPosition = 0f;
				ResetSlotFocus();
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
			for (int k = 0; k < numColumns * displayRows; k++)
			{
				int index = k + numColumns * (int)Math.Round(scrollBar.ViewPosition);
				Item item = index < items.Count ? items[index] : new Item();
				if (!item.IsAir && !didMatCheck[index])
				{
					item.checkMat();
					didMatCheck[index] = true;
				}
				Vector2 drawPos = slotZonePos + new Vector2((itemSlotWidth + padding) * (k % 10), (itemSlotHeight + padding) * (k / 10));
				temp[10] = item;
				ItemSlot.Draw(Main.spriteBatch, temp, 0, 10, drawPos);
			}
			if (hoverSlot >= 0 && hoverSlot < items.Count)
			{
				Main.toolTip = items[hoverSlot].Clone();
				Main.instance.MouseText(string.Empty);
			}
			sortButtons.DrawText();
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
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>(MagicStorage.Instance);
			Point16 pos = modPlayer.ViewingStorage();
			if (pos.X < 0 || pos.Y < 0)
			{
				return null;
			}
			Tile tile = Main.tile[pos.X, pos.Y];
			if (tile == null)
			{
				return null;
			}
			int tileType = tile.type;
			ModTile modTile = TileLoader.GetTile(tileType);
			if (modTile == null || !(modTile is StorageAccess))
			{
				return null;
			}
			return ((StorageAccess)modTile).GetHeart(pos.X, pos.Y);
		}

		public static void RefreshItems()
		{
			items.Clear();
			didMatCheck.Clear();
			TEStorageHeart heart = GetHeart();
			if (heart == null)
			{
				return;
			}
			InitSortButtons();
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
			items.AddRange(ItemSorter.SortAndFilter(heart.GetStoredItems(), sortMode, "", searchBar.Text));
			for (int k = 0; k < items.Count; k++)
			{
				didMatCheck.Add(false);
			}
		}

		private static void UpdateDepositButton()
		{
			CalculatedStyle dim = depositButton.GetDimensions();
			if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
			{
				depositButton.BackgroundColor = new Color(73, 94, 171);
				if (MouseClicked)
				{
					if (TryDepositAll())
					{
						RefreshItems();
						Main.PlaySound(7, -1, -1, 1);
					}
				}
			}
			else
			{
				depositButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
			}
		}

		private static void UpdateItemSlots()
		{
			Player player = Main.player[Main.myPlayer];
			if (!player.trashItem.IsAir)
			{
				if (TryDeposit(player.trashItem))
				{
					RefreshItems();
				}
				if (!player.trashItem.IsAir)
				{
					player.trashItem = player.GetItem(Main.myPlayer, player.trashItem, false, true);
				}
			}

			hoverSlot = -1;
			if (curMouse.RightButton == ButtonState.Released)
			{
				ResetSlotFocus();
			}
			TryHoverSlot();
			if (slotFocus >= 0)
			{
				SlotFocusLogic();
			}
		}

		private static void ResetSlotFocus()
		{
			slotFocus = -1;
			rightClickTimer = 0;
			maxRightClickTimer = startMaxRightClickTimer;
		}

		private static void TryHoverSlot()
		{
			Vector2 slotOrigin = slotZone.GetDimensions().Position();
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
				HoverItemSlot(slotX + numColumns * slotY);
			}
		}

		private static void HoverItemSlot(int slot)
		{
			Player player = Main.player[Main.myPlayer];
			slot += numColumns * (int)Math.Round(scrollBar.ViewPosition);
			if (MouseClicked)
			{
				bool changed = false;
				if (!Main.mouseItem.IsAir)
				{
					if (TryDeposit(Main.mouseItem))
					{
						changed = true;
					}
				}
				else if (Main.mouseItem.IsAir && slot < items.Count && !items[slot].IsAir)
				{
					Item toWithdraw = items[slot].Clone();
					if (toWithdraw.stack > toWithdraw.maxStack)
					{
						toWithdraw.stack = toWithdraw.maxStack;
					}
					Main.mouseItem = DoWithdraw(toWithdraw, ItemSlot.ShiftInUse);
					if (ItemSlot.ShiftInUse)
					{
						Main.mouseItem = player.GetItem(Main.myPlayer, Main.mouseItem, false, true);
					}
					changed = true;
				}
				if (changed)
				{
					RefreshItems();
					Main.PlaySound(7, -1, -1, 1);
				}
			}

			if (curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released && slot < items.Count && (Main.mouseItem.IsAir || ItemData.Matches(Main.mouseItem, items[slot]) && Main.mouseItem.stack < Main.mouseItem.maxStack))
			{
				slotFocus = slot;
			}
			
			if (slot < items.Count && !items[slot].IsAir)
			{
				hoverSlot = slot;
			}
		}

		private static void SlotFocusLogic()
		{
			if (slotFocus >= items.Count || (!Main.mouseItem.IsAir && (!ItemData.Matches(Main.mouseItem, items[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack)))
			{
				ResetSlotFocus();
			}
			else
			{
				if (rightClickTimer <= 0)
				{
					rightClickTimer = maxRightClickTimer;
					maxRightClickTimer = maxRightClickTimer * 3 / 4;
					if (maxRightClickTimer <= 0)
					{
						maxRightClickTimer = 1;
					}
					Item toWithdraw = items[slotFocus].Clone();
					toWithdraw.stack = 1;
					Item result = DoWithdraw(toWithdraw);
					if (Main.mouseItem.IsAir)
					{
						Main.mouseItem = result;
					}
					else
					{
						Main.mouseItem.stack += result.stack;
					}
					Main.soundInstanceMenuTick.Stop();
					Main.soundInstanceMenuTick = Main.soundMenuTick.CreateInstance();
					Main.PlaySound(12, -1, -1, 1);
					RefreshItems();
				}
				rightClickTimer--;
			}
		}

		private static bool TryDeposit(Item item)
		{
			int oldStack = item.stack;
			DoDeposit(item);
			return oldStack != item.stack;
		}

		private static void DoDeposit(Item item)
		{
			TEStorageHeart heart = GetHeart();
			if (Main.netMode == 0)
			{
				heart.DepositItem(item);
			}
			else
			{
				NetHelper.SendDeposit(heart.ID, item);
				item.SetDefaults(0, true);
			}
		}

		private static bool TryDepositAll()
		{
			Player player = Main.player[Main.myPlayer];
			TEStorageHeart heart = GetHeart();
			bool changed = false;
			if (Main.netMode == 0)
			{
				for (int k = 10; k < 50; k++)
				{
					if (!player.inventory[k].IsAir && !player.inventory[k].favorited)
					{
						int oldStack = player.inventory[k].stack;
						heart.DepositItem(player.inventory[k]);
						if (oldStack != player.inventory[k].stack)
						{
							changed = true;
						}
					}
				}
			}
			else
			{
				List<Item> items = new List<Item>();
				for (int k = 10; k < 50; k++)
				{
					if (!player.inventory[k].IsAir && !player.inventory[k].favorited)
					{
						items.Add(player.inventory[k]);
					}
				}
				NetHelper.SendDepositAll(heart.ID, items);
				foreach (Item item in items)
				{
					item.SetDefaults(0, true);
				}
				changed = true;
			}
			return changed;
		}

		private static Item DoWithdraw(Item item, bool toInventory = false)
		{
			TEStorageHeart heart = GetHeart();
			if (Main.netMode == 0)
			{
				return heart.TryWithdraw(item);
			}
			else
			{
				NetHelper.SendWithdraw(heart.ID, item, toInventory);
				return new Item();
			}
		}
	}
}